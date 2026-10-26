using System.Numerics;
using Content.Shared._ST14.CCVar;
using Content.Shared._ST14.MainHub;
using Robust.Shared.Configuration;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Graphics;
using Robust.Shared.Timing;

namespace Content.Client._ST14.MainHub;

/// <summary>
/// Draws a layered <see cref="ST14HubScenePrototype"/> scaled to cover the control.
/// With several scenes it plays each one and switches to the next through the glitch of the original menu, the old one keeps moving until the switch
/// </summary>
public sealed partial class ST14AnimatedScene : Control
{
    [Dependency] private IPrototypeManager _protoMan = default!;
    [Dependency] private IResourceCache _cache = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IClyde _clyde = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private ILogManager _log = default!;

    private const string ScanlinesPath = "/Textures/_ST14/Interface/Hub/Settings/scanlines.png";
    // GlitchTransition of the original camera: 0.2 s up to full strength, the wallpaper changes at the peak, 0.2 s back down
    private const float GlitchIn = 0.2f;
    private const float GlitchOut = 0.2f;
    private static readonly ProtoId<ShaderPrototype> GlitchShader = "ST14HubGlitch";

    private static readonly Color SloganColor = Color.FromHex("#FFCC7C");
    private static readonly Color SloganOutline = Color.FromHex("#3A1E00");

    private readonly List<SceneData> _playlist = new();
    private int _current;
    private float _time;
    private float _glitch = -1;
    private float _clock;
    private Texture? _scanlines;
    private ShaderInstance? _glitchShader;
    private IRenderTexture? _target;
    private bool _glitchWarm;

    // animation clock
    private readonly System.Diagnostics.Stopwatch _stopwatch = System.Diagnostics.Stopwatch.StartNew();
    private double _lastFrame;
    private double _smoothElapsed;
    private float _avgDelta;
    private bool _smooth = true;
    private float _supersample = 2f;

    // st14.hub_debug: frame pacing and the on-screen position of one moving layer, logged every few seconds
    private const int StatFrames = 2048;
    private const float StatPeriod = 3f;
    private readonly float[] _statRaw = new float[StatFrames];
    private readonly float[] _statStep = new float[StatFrames];
    private readonly float[] _statPos = new float[StatFrames];
    private int _statCount;
    private float _statTime;
    private bool _debug;
    private ISawmill? _sawmill;

    /// <summary>
    /// Extra seconds a scene stays after its animation ends before switching
    /// </summary>
    public float Hold;

    /// <summary>
    /// Alpha of the scanline overlay drawn over the whole scene, zero disables it
    /// </summary>
    public float ScanlineAlpha;

    public ST14AnimatedScene()
    {
        IoCManager.InjectDependencies(this);
        HorizontalExpand = true;
        VerticalExpand = true;
        RectClipContent = true;
    }

    /// <summary>
    /// Loads every scene and its textures up front, so switching never stalls on the first draw
    /// </summary>
    public void SetScenes(IEnumerable<ProtoId<ST14HubScenePrototype>> scenes, bool shuffle = true)
    {
        _playlist.Clear();
        foreach (var id in scenes)
        {
            if (_protoMan.TryIndex(id, out var proto))
                _playlist.Add(new SceneData(proto, _cache));
        }

        if (shuffle)
            _random.Shuffle(_playlist);

        _current = 0;
        _time = 0;
        _glitch = -1;
        _warmScene = 0;
        _warmLayer = 0;
    }

    public void SetScene(ProtoId<ST14HubScenePrototype> scene)
    {
        SetScenes(new[] { scene }, false);
    }

    /// <summary>
    /// Switches to another scene through the same glitch as between the wallpapers
    /// </summary>
    public void GlitchTo(ProtoId<ST14HubScenePrototype> scene)
    {
        if (_playlist.Count == 0 || !_protoMan.TryIndex(scene, out var proto))
        {
            SetScene(scene);
            return;
        }

        var current = _playlist[_current];
        if (current.Proto.ID == scene.Id)
            return;

        _playlist.Clear();
        _playlist.Add(current);
        _playlist.Add(new SceneData(proto, _cache));
        _current = 0;
        _glitch = 0;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!VisibleInTree || _playlist.Count == 0)
            return;

        _smooth = _cfg.GetCVar(ST14CVars.HubSmooth);
        _supersample = _cfg.GetCVar(ST14CVars.HubSupersample);
        _debug = _cfg.GetCVar(ST14CVars.HubDebug);

        var step = NextStep();
        _clock += step;
        _time += step;

        if (_glitch >= 0)
        {
            var before = _glitch;
            _glitch += step;

            // the old wallpaper keeps moving until the peak, then the next one starts from its first frame
            if (before < GlitchIn && _glitch >= GlitchIn)
            {
                _current = (_current + 1) % _playlist.Count;
                _time = 0;
            }

            if (_glitch >= GlitchIn + GlitchOut)
                _glitch = -1;
        }

        var scene = _playlist[_current].Proto;
        if (_playlist.Count > 1 && _glitch < 0 && scene.Duration > 0 && _time >= scene.Duration + Hold)
            _glitch = 0;
    }

    /// <summary>
    /// Time step of this frame. The raw frame delta jitters by a few milliseconds even with vsync, with motion of a couple of
    /// pixels per second that jitter is visible. The smoothed clock advances by the running average of the deltas and is
    /// pulled softly towards the real clock, so it never drifts and still follows real hitches after a moment
    /// </summary>
    private float NextStep()
    {
        var now = _stopwatch.Elapsed.TotalSeconds;
        var raw = (float) (now - _lastFrame);
        _lastFrame = now;

        if (!_smooth || raw <= 0f || raw > 0.25f || _avgDelta <= 0f)
        {
            // first frame, a pause or a long hitch: jump straight to real time
            _avgDelta = raw is > 0f and <= 0.25f ? raw : 1f / 60f;
            _smoothElapsed = now;
            RecordFrame(raw, raw);
            return Math.Clamp(raw, 0f, 0.25f);
        }

        _avgDelta += (raw - _avgDelta) * 0.08f;
        var drift = (float) (now - _smoothElapsed - _avgDelta);
        var step = Math.Clamp(_avgDelta + drift * 0.1f, 0f, 0.1f);
        _smoothElapsed += step;
        RecordFrame(raw, step);
        return step;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (_playlist.Count == 0)
            return;

        var baseTransform = handle.GetTransform();
        Vector2 size = PixelSize;
        var scene = _playlist[_current];
        var amount = GlitchAmount();

        if (_smooth && PixelSize.X > 0 && PixelSize.Y > 0)
        {
            DrawSupersampled(handle, scene, size, amount, baseTransform);
            DrawScanlines(handle, size, baseTransform);
            return;
        }

        if (_playlist.Count > 1 && !_glitchWarm && PixelSize.X > 0 && PixelSize.Y > 0)
        {
            // compile the glitch shader and allocate the offscreen texture before the first switch, otherwise that frame stalls
            EnsureTarget();
            _glitchShader ??= _protoMan.Index(GlitchShader).InstanceUnique();
            handle.RenderInRenderTarget(_target!, () => { }, Color.Black);
            handle.UseShader(_glitchShader);
            // a pixel outside the control, clipped away, just to get the shader compiled
            handle.DrawRect(new UIBox2(-2, -2, -1, -1), Color.Transparent);
            handle.UseShader(null);
            _glitchWarm = true;
        }

        if (amount > 0 && PixelSize.X > 0 && PixelSize.Y > 0)
        {
            // the glitch works on the finished picture, so the scene goes to an offscreen texture first
            EnsureTarget();

            handle.RenderInRenderTarget(_target!, () =>
            {
                DrawScene(handle, scene, _time, 1f, size, Matrix3x2.Identity);
                handle.SetTransform(Matrix3x2.Identity);
            }, Color.Black);

            _glitchShader ??= _protoMan.Index(GlitchShader).InstanceUnique();
            _glitchShader.SetParameter("amount", amount);
            _glitchShader.SetParameter("screenSize", size);

            handle.SetTransform(baseTransform);
            handle.UseShader(_glitchShader);
            handle.DrawTextureRect(_target!.Texture, new UIBox2(Vector2.Zero, size));
            handle.UseShader(null);
        }
        else
        {
            DrawScene(handle, scene, _time, 1f, size, baseTransform);
        }

        DrawScanlines(handle, size, baseTransform);
    }

    /// <summary>
    /// The scene is drawn into an offscreen texture a few times larger than the control with the full resolution art,
    /// then that texture is scaled down onto the screen. A layer moving by a hundredth of a pixel per frame then changes
    /// the result smoothly instead of the picture flickering between sharp and soft every time it crosses a pixel.
    /// The glitch is applied to the same texture on the way to the screen
    /// </summary>
    private void DrawSupersampled(DrawingHandleScreen handle, SceneData scene, Vector2 size, float amount, Matrix3x2 baseTransform)
    {
        var factor = Supersample(PixelSize);
        var targetSize = new Vector2i((int) MathF.Round(PixelSize.X * factor), (int) MathF.Round(PixelSize.Y * factor));
        EnsureTarget(targetSize);

        _renderScene = scene;
        _renderSize = targetSize;
        _renderFactor = factor;
        _renderHandle = handle;
        handle.RenderInRenderTarget(_target!, _renderAction ??= RenderToTarget, Color.Black);
        _renderHandle = null;

        handle.SetTransform(baseTransform);

        if (amount > 0)
        {
            _glitchShader ??= _protoMan.Index(GlitchShader).InstanceUnique();
            _glitchShader.SetParameter("amount", amount);
            _glitchShader.SetParameter("screenSize", size);
            handle.UseShader(_glitchShader);
            handle.DrawTextureRect(_target!.Texture, new UIBox2(Vector2.Zero, size));
            handle.UseShader(null);
        }
        else
        {
            handle.DrawTextureRect(_target!.Texture, new UIBox2(Vector2.Zero, size));

            if (_playlist.Count > 1 && !_glitchWarm)
            {
                // compile the glitch shader before the first switch, otherwise that frame stalls
                _glitchShader ??= _protoMan.Index(GlitchShader).InstanceUnique();
                handle.UseShader(_glitchShader);
                handle.DrawRect(new UIBox2(-2, -2, -1, -1), Color.Transparent);
                handle.UseShader(null);
                _glitchWarm = true;
            }
        }
    }

    // cached so drawing into the offscreen texture allocates nothing per frame
    private Action? _renderAction;
    private DrawingHandleScreen? _renderHandle;
    private SceneData? _renderScene;
    private Vector2i _renderSize;
    private float _renderFactor = 1f;

    private void RenderToTarget()
    {
        if (_renderHandle == null || _renderScene == null)
            return;

        WarmTextures(_renderHandle);

        DrawScene(_renderHandle, _renderScene, _time, 1f, _renderSize, Matrix3x2.Identity, _renderFactor);
        _renderHandle.SetTransform(Matrix3x2.Identity);
    }

    private int _warmScene;
    private int _warmLayer;

    /// <summary>
    /// Video drivers upload a texture on its first real use, a whole wallpaper of 4K layers at once costs a visible frame.
    /// Every frame two textures of the playlist are touched with a one pixel quad (painted over by the scene right after),
    /// so all of them are on the card before their wallpaper first shows up
    /// </summary>
    private void WarmTextures(DrawingHandleScreen handle)
    {
        var budget = 2;
        while (budget > 0 && _warmScene < _playlist.Count)
        {
            var scene = _playlist[_warmScene];
            if (_warmLayer >= scene.Textures.Length)
            {
                _warmScene++;
                _warmLayer = 0;
                continue;
            }

            var i = _warmLayer++;
            if (scene.Textures[i] is { } full)
            {
                handle.DrawTextureRect(full, new UIBox2(0, 0, 1, 1));
                budget--;
            }

            if (scene.HalfTextures[i] is { } half)
                handle.DrawTextureRect(half, new UIBox2(0, 0, 1, 1));
        }
    }

    /// <summary>
    /// How many times larger than the screen the scene is rendered. The art has two texels per canvas unit, so going past
    /// that brings nothing, and the texture size stays within what every video card takes
    /// </summary>
    private float Supersample(Vector2i pixelSize)
    {
        var canvas = ST14CanvasLayout.CanvasSize;
        var screenScale = MathF.Max(pixelSize.X / canvas.X, pixelSize.Y / canvas.Y);
        var factor = Math.Clamp(_supersample, 1f, 4f);
        factor = MathF.Min(factor, MathF.Max(1f, 2f / screenScale));
        factor = MathF.Min(factor, 4096f / MathF.Max(pixelSize.X, pixelSize.Y));
        return MathF.Max(1f, factor);
    }

    private void DrawScanlines(DrawingHandleScreen handle, Vector2 size, Matrix3x2 baseTransform)
    {
        handle.SetTransform(baseTransform);

        if (ScanlineAlpha <= 0)
            return;

        _scanlines ??= _cache.GetResource<TextureResource>(ScanlinesPath).Texture;
        var scale = MathF.Max(size.X / ST14CanvasLayout.CanvasSize.X, size.Y / ST14CanvasLayout.CanvasSize.Y);
        // the scanline texture is full resolution: two texels per canvas unit
        DrawTiled(handle, _scanlines, new UIBox2(Vector2.Zero, size), Color.Black.WithAlpha(ScanlineAlpha), 2f / scale);
    }

    private void EnsureTarget() => EnsureTarget(PixelSize);

    private void EnsureTarget(Vector2i size)
    {
        if (_target != null && _target.Size == size)
            return;

        _target?.Dispose();
        _target = _clyde.CreateRenderTarget(size,
            new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb),
            new TextureSampleParameters { Filter = true },
            nameof(ST14AnimatedScene));
    }

    private void RecordFrame(float raw, float step)
    {
        if (!_debug)
            return;

        if (_statCount < StatFrames)
        {
            _statRaw[_statCount] = raw;
            _statStep[_statCount] = step;
            _statPos[_statCount] = float.NaN;
            _statCount++;
        }

        _statTime += raw;
        if (_statTime >= StatPeriod)
            FlushStats();
    }

    private void RecordPosition(float x)
    {
        if (_statCount > 0)
            _statPos[_statCount - 1] = x;
    }

    /// <summary>
    /// Frame pacing and how even the per-frame movement of the probe layer was, in screen pixels
    /// </summary>
    private void FlushStats()
    {
        _sawmill ??= _log.GetSawmill("st14.hub");

        double rawSum = 0, rawSq = 0, stepSum = 0, stepSq = 0;
        float rawMax = 0;
        for (var i = 0; i < _statCount; i++)
        {
            rawSum += _statRaw[i];
            rawSq += _statRaw[i] * _statRaw[i];
            stepSum += _statStep[i];
            stepSq += _statStep[i] * _statStep[i];
            rawMax = MathF.Max(rawMax, _statRaw[i]);
        }

        var n = Math.Max(1, _statCount);
        var rawMean = rawSum / n;
        var stepMean = stepSum / n;

        // per-frame movement of the probe, divided by its own frame step so uneven frames show up as uneven speed
        double speedSum = 0, speedSq = 0;
        var speeds = 0;
        double dxSum = 0, dxSq = 0;
        var steps1px = 0;
        for (var i = 1; i < _statCount; i++)
        {
            var a = _statPos[i - 1];
            var b = _statPos[i];
            if (float.IsNaN(a) || float.IsNaN(b) || _statStep[i] <= 0)
                continue;

            var dx = b - a;
            if (MathF.Abs(dx) > 5f) // a scene switch
                continue;

            if (MathF.Abs(dx - MathF.Round(dx)) < 0.001f && MathF.Abs(dx) >= 1f)
                steps1px++;

            dxSum += dx;
            dxSq += dx * dx;
            var speed = dx / _statStep[i];
            speedSum += speed;
            speedSq += speed * speed;
            speeds++;
        }

        var m = Math.Max(1, speeds);
        var speedMean = speedSum / m;
        var speedStd = Math.Sqrt(Math.Max(0, speedSq / m - speedMean * speedMean));
        var dxMean = dxSum / m;
        var dxStd = Math.Sqrt(Math.Max(0, dxSq / m - dxMean * dxMean));

        _sawmill.Info(
            $"smooth={_smooth} ss={Supersample(PixelSize):0.##} frames={_statCount} " +
            $"dt={rawMean * 1000:0.00}ms sd={Math.Sqrt(Math.Max(0, rawSq / n - rawMean * rawMean)) * 1000:0.00} max={rawMax * 1000:0.0} | " +
            $"step sd={Math.Sqrt(Math.Max(0, stepSq / n - stepMean * stepMean)) * 1000:0.000}ms | " +
            $"probe dx/frame={dxMean:0.0000}px sd={dxStd:0.0000} speed={speedMean:0.00}px/s sd={speedStd:0.00} whole-px-steps={steps1px}");

        _statCount = 0;
        _statTime = 0;
    }

    private float GlitchAmount()
    {
        if (_glitch < 0)
            return 0f;

        return _glitch < GlitchIn
            ? _glitch / GlitchIn
            : MathF.Max(0f, 1f - (_glitch - GlitchIn) / GlitchOut);
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();
        _target?.Dispose();
        _target = null;
        _glitchWarm = false;
    }

    private void DrawScene(DrawingHandleScreen handle, SceneData scene, float time, float sceneAlpha, Vector2 size, Matrix3x2 baseTransform,
        float pixelsPerScreenPixel = 1f)
    {
        if (sceneAlpha <= 0)
            return;

        var proto = scene.Proto;
        var layers = proto.Layers;
        var canvas = proto.CanvasSize;
        var scale = MathF.Max(size.X / canvas.X, size.Y / canvas.Y);
        var offset = (size - canvas * scale) / 2;
        var canvasMatrix = Matrix3x2.CreateScale(scale) * Matrix3x2.CreateTranslation(offset);
        var rootRect = new UIBox2(-proto.Pivot * canvas, (Vector2.One - proto.Pivot) * canvas);
        var rootMatrix = Matrix3x2.CreateTranslation(proto.Pivot * canvas) * canvasMatrix;

        // Textures have two texels per canvas unit, the half copies one. Take the smallest one that is still not stretched,
        // so the picture is always minified (sharp) and never minified more than 2x (no shimmer without mipmaps)
        var useHalf = scale <= 1f;

        for (var i = 0; i < layers.Count; i++)
        {
            var layer = layers[i];
            var parent = scene.Parents[i];
            var parentRect = parent >= 0 ? scene.Rects[parent] : rootRect;
            var parentMatrix = parent >= 0 ? scene.Matrices[parent] : rootMatrix;

            var position = layer.Position;
            var sizeDelta = layer.Size;
            var pivot = layer.Pivot;
            var rotation = layer.Rotation;
            var layerScale = layer.Scale;
            var alpha = 1f;

            foreach (var curve in layer.Curves)
            {
                // past the end the motion keeps its last speed, so a scene fading out never freezes
                var value = curve.Evaluate(time, extrapolate: true);
                switch (curve.Property)
                {
                    case ST14HubSceneProperty.PosX: position.X = value; break;
                    case ST14HubSceneProperty.PosY: position.Y = value; break;
                    case ST14HubSceneProperty.SizeX: sizeDelta.X = value; break;
                    case ST14HubSceneProperty.SizeY: sizeDelta.Y = value; break;
                    case ST14HubSceneProperty.Rotation: rotation = value; break;
                    case ST14HubSceneProperty.PivotX: pivot.X = value; break;
                    case ST14HubSceneProperty.PivotY: pivot.Y = value; break;
                    case ST14HubSceneProperty.ScaleX: layerScale.X = value; break;
                    case ST14HubSceneProperty.ScaleY: layerScale.Y = value; break;
                    case ST14HubSceneProperty.Alpha: alpha = Math.Clamp(value, 0f, 1f); break;
                }
            }

            if (layer.Wiggle is { } wiggle)
            {
                position += new Vector2(
                    Noise(_clock * wiggle.Frequency.X, i * 3.1f) * wiggle.Amplitude.X,
                    Noise(_clock * wiggle.Frequency.Y, i * 3.1f + 1.7f) * wiggle.Amplitude.Y);

                if (wiggle.RotationFrequency > 0)
                    rotation += Noise(_clock * wiggle.RotationFrequency, i * 3.1f + 5.3f) * wiggle.Rotation;
            }

            if (layer.Drift is { } drift)
            {
                var breathe = MathF.Sin((_clock / drift.BreathePeriod + drift.Phase) * MathF.Tau);
                layerScale *= 1f + breathe * drift.Breathe;
                var slide = MathF.Sin((_clock / drift.OffsetPeriod + drift.Phase) * MathF.Tau);
                position += drift.Offset * slide;

                if (drift.Run != Vector2.Zero && drift.RunPeriod > 0)
                {
                    var cycle = _clock / drift.RunPeriod + drift.Phase;
                    position += drift.Run * drift.RunPeriod * (cycle - MathF.Floor(cycle) - 0.5f);
                }
            }

            Vector2 pivotPos;
            UIBox2 rect;
            if (layer.Rect)
            {
                var anchorMin = parentRect.TopLeft + parentRect.Size * layer.AnchorMin;
                var anchorMax = parentRect.TopLeft + parentRect.Size * layer.AnchorMax;
                var rectSize = anchorMax - anchorMin + sizeDelta;
                pivotPos = Vector2.Lerp(anchorMin, anchorMax, pivot) + position;
                rect = new UIBox2(-rectSize * pivot, rectSize * (Vector2.One - pivot));
            }
            else
            {
                pivotPos = position;
                rect = default;
            }

            // all in floats, nothing is snapped to whole pixels
            var matrix = Matrix3x2.CreateScale(layerScale)
                         * Matrix3x2.CreateRotation(MathHelper.DegreesToRadians(rotation))
                         * Matrix3x2.CreateTranslation(pivotPos)
                         * parentMatrix;

            scene.Rects[i] = rect;
            scene.Matrices[i] = matrix;

            if (_debug && i == scene.Probe)
                RecordPosition(Vector2.Transform(Vector2.Zero, matrix).X / pixelsPerScreenPixel);

            if (!layer.Rect)
                continue;

            var color = layer.Color.WithAlpha(layer.Color.A * alpha * sceneAlpha);
            if (color.A <= 0)
                continue;

            if (layer.Text is { } text)
            {
                DrawText(handle, scene, i, text, rect, matrix, scale, baseTransform, color.A);
                continue;
            }

            var half = useHalf ? scene.HalfTextures[i] : null;
            var texture = half ?? scene.Textures[i];
            if (texture == null && !layer.Solid)
                continue;

            handle.SetTransform(matrix * baseTransform);

            if (texture == null)
                handle.DrawRect(rect, color);
            else if (layer.Tile)
                DrawTiled(handle, texture, rect, color, half != null ? 1f : 2f);
            else
                handle.DrawTextureRect(texture, rect, color);
        }
    }

    private void DrawText(DrawingHandleScreen handle, SceneData scene, int index, ST14HubSceneText text, UIBox2 rect,
        Matrix3x2 matrix, float scale, Matrix3x2 baseTransform, float alpha)
    {
        // Text is rendered at screen resolution, so only the rotation and the center come from the layer matrix
        var center = Vector2.Transform(rect.Center, matrix);
        var right = Vector2.Transform(rect.Center + Vector2.UnitX, matrix) - center;
        var angle = MathF.Atan2(right.Y, right.X);

        var fontSize = Math.Max(8, (int) MathF.Round(text.Size * scale));
        var font = ST14HubStyle.Font(_cache, ST14HubFont.Slogan, fontSize);
        var lines = scene.TextLines[index] ??= Loc.GetString(text.Loc).Split('\n');
        var widths = scene.TextWidths[index];
        if (widths == null || scene.TextFontSizes[index] != fontSize)
        {
            widths = new float[lines.Length];
            for (var l = 0; l < lines.Length; l++)
            {
                widths[l] = handle.GetDimensions(font, lines[l], 1f).X;
            }

            scene.TextWidths[index] = widths;
            scene.TextFontSizes[index] = fontSize;
        }

        var lineHeight = font.GetLineHeight(1f);
        var thickness = MathF.Max(2f, fontSize * 0.07f);
        var shadow = Color.Black.WithAlpha(0.6f * alpha);

        handle.SetTransform(Matrix3x2.CreateRotation(angle) * Matrix3x2.CreateTranslation(center) * baseTransform);

        var top = -lines.Length * lineHeight / 2f;
        for (var l = 0; l < lines.Length; l++)
        {
            var pos = new Vector2(-widths[l] / 2f, top);

            // drop shadow, then the outlined golden face
            handle.DrawString(font, pos + new Vector2(fontSize * 0.06f), lines[l], 1f, shadow, new TextOutline(thickness, shadow));
            handle.DrawString(font, pos, lines[l], 1f, SloganColor.WithAlpha(alpha), new TextOutline(thickness, SloganOutline.WithAlpha(alpha)));
            top += lineHeight;
        }
    }

    /// <summary>
    /// One quad with repeating texture coordinates, the tiled textures are loaded with wrap: repeat
    /// </summary>
    private static void DrawTiled(DrawingHandleScreen handle, Texture texture, UIBox2 rect, Color color, float texelsPerUnit)
    {
        var region = new UIBox2(0, 0, rect.Width * texelsPerUnit, rect.Height * texelsPerUnit);
        handle.DrawTextureRectRegion(texture, rect, region, color);
    }

    /// <summary>
    /// Smooth noise in [-1, 1] from continuous time, close enough to the Perlin wiggle of the original
    /// </summary>
    private static float Noise(float x, float seed)
    {
        return (MathF.Sin(x * MathF.Tau + seed)
                + 0.5f * MathF.Sin(x * MathF.Tau * 2.17f + seed * 1.9f)
                + 0.25f * MathF.Sin(x * MathF.Tau * 4.31f + seed * 3.7f)) / 1.75f;
    }

    private sealed class SceneData
    {
        public readonly ST14HubScenePrototype Proto;
        public readonly Texture?[] Textures;
        public readonly Texture?[] HalfTextures;
        public readonly int[] Parents;
        public readonly UIBox2[] Rects;
        public readonly Matrix3x2[] Matrices;
        public readonly string[]?[] TextLines;
        public readonly float[]?[] TextWidths;
        public readonly int[] TextFontSizes;

        /// <summary>
        /// A moving layer whose screen position st14.hub_debug tracks
        /// </summary>
        public readonly int Probe = -1;

        public SceneData(ST14HubScenePrototype proto, IResourceCache cache)
        {
            Proto = proto;
            var count = proto.Layers.Count;
            Textures = new Texture?[count];
            HalfTextures = new Texture?[count];
            Parents = new int[count];
            Rects = new UIBox2[count];
            Matrices = new Matrix3x2[count];
            TextLines = new string[count][];
            TextWidths = new float[count][];
            TextFontSizes = new int[count];

            var index = new Dictionary<string, int>();
            for (var i = 0; i < count; i++)
            {
                var layer = proto.Layers[i];
                index[layer.Id] = i;
                Parents[i] = layer.Parent != null && index.TryGetValue(layer.Parent, out var parent) ? parent : -1;

                if (Probe < 0 && layer.Rect && layer.Texture != null
                    && layer.Curves.Exists(c => c.Property == ST14HubSceneProperty.PosX))
                    Probe = i;

                if (layer.Texture is not { } path)
                    continue;

                if (cache.TryGetResource<TextureResource>(path, out var texture))
                    Textures[i] = texture.Texture;

                var halfPath = path.WithName(path.FilenameWithoutExtension + ".half.png");
                if (cache.ContentFileExists(halfPath) && cache.TryGetResource<TextureResource>(halfPath, out var halfTexture))
                    HalfTextures[i] = halfTexture.Texture;
            }
        }
    }
}
