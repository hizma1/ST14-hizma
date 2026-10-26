using System.Linq;
using System.Numerics;
using Content.Shared._ST14.MainHub;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._ST14.MainHub;

/// <summary>
/// Common parts of the screens opened by "To battle!": animated background, the dark bottom bar and the exit icon in its corner
/// </summary>
public abstract partial class ST14WarScreenBase : Control
{
    [Dependency] protected IResourceCache ResCache = default!;

    protected readonly ST14AnimatedScene Scene;
    protected readonly ST14CanvasLayout Canvas;

    public event Action? OnBack;

    protected ST14WarScreenBase(ProtoId<ST14HubScenePrototype> scene)
    {
        IoCManager.InjectDependencies(this);

        MouseFilter = MouseFilterMode.Stop;
        HorizontalExpand = true;
        VerticalExpand = true;
        Visible = false;

        AddChild(new PanelContainer { PanelOverride = ST14HubStyle.Flat(Color.Black) });

        Scene = new ST14AnimatedScene { ScanlineAlpha = 0.3f };
        Scene.SetScene(scene);
        AddChild(Scene);

        Canvas = new ST14CanvasLayout { ScaleLabels = true };
        AddChild(Canvas);
    }

    /// <summary>
    /// Bottom bar of the original, added after the screen content so it covers it
    /// </summary>
    protected void AddBottomBar()
    {
        Canvas.Add(new TextureRect
        {
            Texture = ST14HubStyle.Tex(ResCache, "mask_top.png"),
            Stretch = TextureRect.StretchMode.Scale,
            ModulateSelfOverride = Color.Black,
        }, new Vector2(0, 1), new Vector2(1, 1), Vector2.Zero, new Vector2(0, 110.6f), new Vector2(0.5f, 1));

        Canvas.Add(new PanelContainer { PanelOverride = ST14HubStyle.MainBar(ResCache) },
            new Vector2(0, 1), new Vector2(1, 1), Vector2.Zero, new Vector2(0, 93), new Vector2(0.5f, 1));

        var back = new ST14HubIconButton(ST14HubStyle.Tex(ResCache, "exit.png"), new Color(0.15f, 0.35f, 0.47f))
        {
            PressSound = ST14HubSound.Back,
            ToolTip = Loc.GetString("st14-hub-war-back"),
        };
        back.OnPressed += _ => OnBack?.Invoke();
        Canvas.AddAt(back, new Vector2(0, 1), new Vector2(62.5f, -18.2f), new Vector2(70.2f, 70.2f), new Vector2(0, 1));
    }
}

/// <summary>
/// Mode selection of the original: three tall cards on a starfield. Only the galactic war is open, the other two are shown as in development
/// </summary>
public sealed class ST14ModeSelectScreen : ST14WarScreenBase
{
    // Card centres and size of GameModeSelectionMenu, relative to the screen centre
    private const float CardStep = 650f;
    private const float CardY = -69.35f;
    private static readonly Vector2 CardSize = new(599.9f, 838.2f);

    public event Action? OnGalacticWar;

    public ST14ModeSelectScreen() : base("ST14HubModes")
    {
        AddCard(0, "card_campaign.png", "st14-hub-war-mode-galactic", false,
            new ST14CardCaption("st14-hub-war-mode-galactic-banner", new Vector2(0, 302.5f), 6.56f, ST14HubFont.Black, 28));

        AddCard(1, "card_territory.png", "st14-hub-war-mode-territory", true,
            new ST14CardCaption("st14-hub-war-mode-territory-banner", new Vector2(-1, -336.5f), 6.56f, ST14HubFont.Black, 28),
            new ST14CardCaption("st14-hub-war-in-development", new Vector2(5.8f, 300.2f), -7.4f, ST14HubFont.SemiBold, 24));

        AddCard(2, "card_challenges.png", "st14-hub-war-mode-challenges", true);

        AddBottomBar();
    }

    private void AddCard(int index, string art, string name, bool locked, params ST14CardCaption[] captions)
    {
        var x = (index - 1) * CardStep;

        var card = new ST14ModeCard(ResCache, art, captions)
        {
            Locked = locked,
            Stamp = locked && captions.Length == 0 ? Loc.GetString("st14-hub-war-in-development") : null,
        };
        Canvas.AddAt(card, new Vector2(0.5f, 0.5f), new Vector2(x, CardY), CardSize);

        // the plate hangs over the bottom edge of the card
        var plate = new ST14HubButton(ResCache, Loc.GetString(name), 20, locked ? Color.FromHex("#8A8A8A") : null)
        {
            Disabled = locked,
        };
        Canvas.AddAt(plate, new Vector2(0.5f, 0.5f), new Vector2(x, CardY + CardSize.Y / 2 - 10), new Vector2(400, 70));

        if (locked)
            return;

        card.OnPressed += _ => OnGalacticWar?.Invoke();
        plate.OnPressed += _ => OnGalacticWar?.Invoke();
    }
}

/// <summary>
/// Slanted caption drawn over a banner baked into the card art. Position is from the card centre in canvas units,
/// the angle is the counter clockwise angle of the original
/// </summary>
public readonly record struct ST14CardCaption(string Loc, Vector2 Position, float Angle, ST14HubFont Font, int Size);

/// <summary>
/// Tall mode card: thin frame, the art inside and captions on its banners. Locked cards are dimmed and do not react
/// </summary>
public sealed class ST14ModeCard : ContainerButton
{
    private readonly IResourceCache _cache;
    private readonly (Texture Full, Texture Half) _art;
    private readonly StyleBoxTexture _frame;
    private readonly (string Text, ST14CardCaption Caption)[] _captions;
    private bool _locked;

    public string? Stamp;

    public ST14ModeCard(IResourceCache cache, string art, ST14CardCaption[] captions)
    {
        _cache = cache;
        MuteSounds = true;
        StyleBoxOverride = new StyleBoxEmpty();

        _art = ST14HubStyle.WarTexPair(cache, art);
        _frame = new StyleBoxTexture
        {
            Texture = ST14HubStyle.WarTex(cache, "card_frame.png"),
            TextureScale = new Vector2(0.5f, 0.5f),
        };
        _frame.SetPatchMargin(StyleBox.Margin.All, 8);

        _captions = captions.Select(c => (Loc.GetString(c.Loc).ToUpperInvariant(), c)).ToArray();
        OnPressed += _ => Sounds.Play(ST14HubSound.Click);
    }

    public bool Locked
    {
        get => _locked;
        set
        {
            _locked = value;
            Disabled = value;
            DefaultCursorShape = value ? CursorShape.Arrow : CursorShape.Hand;
        }
    }

    private ST14HubSoundController Sounds => UserInterfaceManager.GetUIController<ST14HubSoundController>();

    protected override void MouseEntered()
    {
        base.MouseEntered();

        if (!Disabled)
            Sounds.Play(ST14HubSound.Hover);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var size = PixelSize;
        var scale = size.Y / 838.2f;
        var hover = DrawMode is DrawModeEnum.Hover or DrawModeEnum.Pressed;

        _frame.Modulate = hover ? ST14HubStyle.GoldLight : Color.White;
        _frame.Draw(handle, UIBox2.FromDimensions(Vector2.Zero, size), scale);

        var tint = _locked ? new Color(0.36f, 0.36f, 0.38f) : hover ? Color.White : new Color(0.88f, 0.88f, 0.88f);
        var inner = new UIBox2(5 * scale, 5 * scale, size.X - 5 * scale, size.Y - 5 * scale);
        handle.DrawTextureRect(ST14HubStyle.Pick(_art, inner.Width), inner, tint);

        var center = size / 2;
        foreach (var (text, caption) in _captions)
        {
            var font = ST14HubStyle.Font(_cache, caption.Font, Math.Max(8, (int) MathF.Round(caption.Size * scale)));
            ST14HubStyle.DrawRotatedText(handle, font, text, center + caption.Position * scale, -caption.Angle,
                _locked ? new Color(0.85f, 0.85f, 0.85f) : Color.White, ST14HubStyle.TextShadow);
        }

        if (Stamp != null)
            ST14ClassifiedStamp.DrawStamp(handle, _cache, Stamp, center, new Vector2(500, 70) * scale, -6.6f, scale);
    }
}

/// <summary>
/// Galaxy map of the original territory mode. Klendathu leads to the lobby, the other planets are classified for now
/// </summary>
public sealed class ST14GalaxyScreen : ST14WarScreenBase
{
    public event Action? OnKlendathu;

    public ST14GalaxyScreen() : base("ST14HubGalaxy")
    {
        // Planets of TerritoryMissionScreen, sizes and positions from the screen centre
        AddPlanet("ghutana", "planet_ghutana.png", new Vector2(-694, -190), 300, 20, -8, new Vector2(0, 0));
        AddPlanet("varuna", "planet_varuna.png", new Vector2(-613.5f, 172.5f), 425, 20, 0, new Vector2(-7, -19.5f));
        AddPlanet("gehenna", "planet_gehenna.png", new Vector2(367.5f, -303), 400, 20, 0, new Vector2(-8, -10));

        var klendathu = AddPlanet("klendathu", "planet_kwalasha.png", new Vector2(71, 33), 500, 25, 0, null);
        klendathu.Glow = ST14HubStyle.WarTexPair(ResCache, "planet_kwalasha_glow.png");
        klendathu.OnPressed += _ => OnKlendathu?.Invoke();

        // The mission line starts on the planet and ends with the operation plate
        var mission = new ST14MissionPlate(ResCache, Loc.GetString("st14-hub-war-klendathu-mission"));
        mission.OnPressed += _ => OnKlendathu?.Invoke();
        mission.OnHoverChanged += hover => klendathu.ForceHighlight = hover;
        klendathu.OnHoverChanged += hover => mission.ForceHighlight = hover;
        Canvas.AddAt(mission, new Vector2(0, 0.5f), new Vector2(1105, -84.5f), new Vector2(425, 90), new Vector2(0, 0.5f));

        AddBottomBar();

        var hint = ST14HubStyle.Label(ResCache, Loc.GetString("st14-hub-war-galaxy-hint").ToUpperInvariant(), ST14HubFont.Regular, 22,
            ST14HubStyle.Gold, shadow: true);
        hint.Align = Label.AlignMode.Center;
        hint.VAlign = Label.VAlignMode.Center;
        Canvas.AddAt(hint, new Vector2(0.5f, 1), new Vector2(0, -46.5f), new Vector2(1000, 50));
    }

    private ST14GalaxyPlanet AddPlanet(string id, string texture, Vector2 position, float size, int fontSize, float labelY,
        Vector2? stamp)
    {
        var planet = new ST14GalaxyPlanet(ResCache, texture, size, stamp != null)
        {
            StampOffset = stamp ?? Vector2.Zero,
        };
        Canvas.AddAt(planet, new Vector2(0.5f, 0.5f), position, new Vector2(size, size));

        // name sits right above the planet
        var label = ST14HubStyle.Label(ResCache, Loc.GetString($"st14-hub-war-planet-{id}").ToUpperInvariant(), ST14HubFont.Medium,
            fontSize, stamp != null ? ST14GalaxyPlanet.LockedLabel : ST14GalaxyPlanet.OpenLabel, shadow: true);
        label.Align = Label.AlignMode.Center;
        planet.NameLabel = label;
        Canvas.AddAt(label, new Vector2(0.5f, 0.5f), position + new Vector2(0, -size / 2 + labelY - 16), new Vector2(250, 40));
        return planet;
    }
}

/// <summary>
/// Round planet on the galaxy map. Locked planets are darkened and carry the classified stamp
/// </summary>
public sealed class ST14GalaxyPlanet : ContainerButton
{
    public static readonly Color LockedLabel = new(0.32f, 0.4f, 0.54f);
    public static readonly Color OpenLabel = new(0.78f, 0.84f, 0.95f);

    private readonly IResourceCache _cache;
    private readonly (Texture Full, Texture Half) _texture;
    private readonly float _canvasSize;
    private readonly bool _locked;
    private bool _hover;
    private bool _forced;

    /// <summary>
    /// Cyan rim shown around the planet on hover, the texture is 1184 / 1024 of the planet size
    /// </summary>
    public (Texture Full, Texture Half)? Glow;
    public Vector2 StampOffset;
    public Label? NameLabel;

    public event Action<bool>? OnHoverChanged;

    public ST14GalaxyPlanet(IResourceCache cache, string texture, float canvasSize, bool locked)
    {
        _cache = cache;
        _canvasSize = canvasSize;
        _texture = ST14HubStyle.WarTexPair(cache, texture);
        _locked = locked;
        MuteSounds = true;
        Disabled = locked;
        StyleBoxOverride = new StyleBoxEmpty();
        DefaultCursorShape = locked ? CursorShape.Arrow : CursorShape.Hand;
        OnPressed += _ => Sounds.Play(ST14HubSound.Click);
    }

    /// <summary>
    /// Lit from outside, when the mouse is over the mission plate of this planet
    /// </summary>
    public bool ForceHighlight
    {
        set
        {
            _forced = value;
            UpdateLabel();
        }
    }

    private ST14HubSoundController Sounds => UserInterfaceManager.GetUIController<ST14HubSoundController>();

    private bool Lit => !_locked && (_hover || _forced);

    protected override bool HasPoint(Vector2 point)
    {
        // only the disc reacts, not the corners of the square
        var radius = Size.X * 0.42f;
        return (point - Size / 2).LengthSquared() <= radius * radius;
    }

    protected override void MouseEntered()
    {
        base.MouseEntered();
        if (_locked)
            return;

        _hover = true;
        Sounds.Play(ST14HubSound.Hover);
        UpdateLabel();
        OnHoverChanged?.Invoke(true);
    }

    protected override void MouseExited()
    {
        base.MouseExited();
        if (_locked)
            return;

        _hover = false;
        UpdateLabel();
        OnHoverChanged?.Invoke(false);
    }

    private void UpdateLabel()
    {
        if (NameLabel != null)
            NameLabel.FontColorOverride = Lit ? ST14HubStyle.GoldLight : _locked ? LockedLabel : OpenLabel;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var size = PixelSize;
        var box = UIBox2.FromDimensions(Vector2.Zero, size);
        // the planet keeps its size, hover only lights a rim around it
        if (Lit && Glow is { } glow)
        {
            var pad = size * (80f / 1024f);
            handle.DrawTextureRect(ST14HubStyle.Pick(glow, size.X + pad.X * 2), new UIBox2(-pad, size + pad));
        }

        // the original covers locked planets with a black disc at 79% opacity
        handle.DrawTextureRect(ST14HubStyle.Pick(_texture, size.X), box, _locked ? new Color(0.21f, 0.21f, 0.21f) : Color.White);

        if (!_locked)
            return;

        var scale = size.X / _canvasSize;
        ST14ClassifiedStamp.DrawStamp(handle, _cache, Loc.GetString("st14-hub-war-classified"), size / 2 + StampOffset * scale,
            new Vector2(341.78f, 55.89f) * scale, -11.5f, scale);
    }
}

/// <summary>
/// Dark red stamp with a frame and white caption, the "CLASSIFIED" mark of the original
/// </summary>
public static class ST14ClassifiedStamp
{
    public static void DrawStamp(DrawingHandleScreen handle, IResourceCache cache, string text, Vector2 center, Vector2 size,
        float degrees, float scale)
    {
        var baseTransform = handle.GetTransform();
        handle.SetTransform(Matrix3x2.CreateRotation(MathF.PI * degrees / 180f) * Matrix3x2.CreateTranslation(center) * baseTransform);

        var half = size / 2;
        handle.DrawRect(new UIBox2(-half + new Vector2(2.5f * scale), half - new Vector2(2.5f * scale)), new Color(0.12f, 0f, 0f, 0.85f));

        var frame = new Color(0.49f, 0.09f, 0.09f);
        var t = MathF.Max(1f, 3f * scale);
        handle.DrawRect(new UIBox2(-half.X, -half.Y, half.X, -half.Y + t), frame);
        handle.DrawRect(new UIBox2(-half.X, half.Y - t, half.X, half.Y), frame);
        handle.DrawRect(new UIBox2(-half.X, -half.Y, -half.X + t, half.Y), frame);
        handle.DrawRect(new UIBox2(half.X - t, -half.Y, half.X, half.Y), frame);

        var font = ST14HubStyle.Font(cache, ST14HubFont.SemiBold, Math.Max(8, (int) MathF.Round(size.Y * 0.45f)));
        handle.SetTransform(baseTransform);
        ST14HubStyle.DrawRotatedText(handle, font, text.ToUpperInvariant(), center, degrees, Color.White);
    }
}

/// <summary>
/// Operation plate next to a planet: a gold line from a dot on the planet to a dark plate with the operation name
/// </summary>
public sealed class ST14MissionPlate : ContainerButton
{
    private readonly IResourceCache _cache;
    private readonly string _text;
    private readonly Texture _line;
    private readonly Texture _dot;
    private readonly StyleBoxTexture _plate;
    private readonly StyleBoxTexture _plateLit;
    private bool _hover;
    private bool _forced;

    public event Action<bool>? OnHoverChanged;

    public ST14MissionPlate(IResourceCache cache, string text)
    {
        _cache = cache;
        _text = text.ToUpperInvariant();
        MuteSounds = true;
        StyleBoxOverride = new StyleBoxEmpty();
        DefaultCursorShape = CursorShape.Hand;

        _line = ST14HubStyle.Tex(cache, "objectivebanner.png");
        _dot = ST14HubStyle.WarTex(cache, "dot.png");
        _plate = Plate(cache, "mission_plate.png", 22, 23);
        _plateLit = Plate(cache, "mission_plate_selected.png", 22, 24);
        OnPressed += _ => Sounds.Play(ST14HubSound.Click);
    }

    private static StyleBoxTexture Plate(IResourceCache cache, string file, float left, float right)
    {
        var box = new StyleBoxTexture
        {
            Texture = ST14HubStyle.WarTex(cache, file),
            TextureScale = new Vector2(0.5f, 0.5f),
        };
        box.SetPatchMargin(StyleBox.Margin.Left, left);
        box.SetPatchMargin(StyleBox.Margin.Right, right);
        return box;
    }

    public bool ForceHighlight
    {
        set => _forced = value;
    }

    private ST14HubSoundController Sounds => UserInterfaceManager.GetUIController<ST14HubSoundController>();

    protected override bool HasPoint(Vector2 point)
    {
        // the line itself does not react, only the plate on its right end
        return point.X >= Size.X - 330f / 425f * Size.X && base.HasPoint(point);
    }

    protected override void MouseEntered()
    {
        base.MouseEntered();
        _hover = true;
        Sounds.Play(ST14HubSound.Hover);
        OnHoverChanged?.Invoke(true);
    }

    protected override void MouseExited()
    {
        base.MouseExited();
        _hover = false;
        OnHoverChanged?.Invoke(false);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var size = PixelSize;
        var scale = size.X / 425f;
        var mid = size.Y / 2;
        var lit = _hover || _forced;
        var gold = new Color(0.83f, 0.67f, 0.43f);

        handle.DrawTextureRect(_line, new UIBox2(0, mid - 1.5f * scale, size.X - 10 * scale, mid + 1.5f * scale), gold);
        var dot = 7f * scale;
        handle.DrawTextureRect(_dot, new UIBox2(-dot, mid - dot, dot, mid + dot), lit ? ST14HubStyle.GoldLight : Color.White);

        var plate = new UIBox2(size.X - 5.25f * scale - 318.4f * scale, mid - 21.25f * scale, size.X - 5.25f * scale, mid + 21.25f * scale);
        (lit ? _plateLit : _plate).Draw(handle, plate, scale);

        var font = ST14HubStyle.Font(_cache, ST14HubFont.Regular, Math.Max(8, (int) MathF.Round(15 * scale)));
        var color = lit ? ST14HubStyle.GoldLight : new Color(0.88f, 0.83f, 0.66f);
        ST14HubStyle.DrawRotatedText(handle, font, _text, plate.Center, 0, color, ST14HubStyle.TextShadow);
    }
}
