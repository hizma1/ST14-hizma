using Content.Client.Audio;
using Content.Shared.CCVar;
using Robust.Client.Audio;
using Robust.Client.ResourceManagement;
using Robust.Client.State;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client._ST14.MainHub;

/// <summary>
/// Plays the main theme while the hub is open: the intro once, then the loop again and again on the bar grid
/// </summary>
public sealed partial class ST14HubMusicSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IStateManager _state = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private ContentAudioSystem _contentAudio = default!;
    [Dependency] private IResourceCache _resCache = default!;

    private const string IntroPath = "/Audio/_ST14/Music/Hub/main_intro.ogg";
    private const string LoopPath = "/Audio/_ST14/Music/Hub/main_loop.ogg";

    // The parts carry a quarter bar of pickup and a reverb tail, so the next part starts on the bar grid, not when the file ends.
    // One bar is worked out from the two files: 8.25 bars + tail = 16.35 s and 62.75 bars + tail = 107.55 s
    private static readonly TimeSpan Bar = TimeSpan.FromSeconds(1.6734);
    private static readonly TimeSpan IntroLength = Bar * 8;
    private static readonly TimeSpan LoopLength = Bar * 62.5;
    private const float FadeTime = 1.5f;

    private readonly AudioParams _params = new(-5f, 1, 0, 0, 0, false, 0f);
    private readonly List<EntityUid> _streams = new();
    private bool _playing;
    private TimeSpan _nextLoop;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesOutsidePrediction = true;
        _state.OnStateChanged += OnStateChanged;

        // Decode both parts now. Loading the 2 MB loop when it is first played froze the hub for ~200-360 ms
        // right in the middle of the animated background
        _resCache.TryGetResource<AudioResource>(IntroPath, out _);
        _resCache.TryGetResource<AudioResource>(LoopPath, out _);
        Subs.CVar(_cfg, CCVars.LobbyMusicEnabled, OnMusicEnabled);
        Subs.CVar(_cfg, CCVars.LobbyMusicVolume, OnVolume);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _state.OnStateChanged -= OnStateChanged;
        Stop(0);
    }

    private void OnStateChanged(StateChangedEventArgs args)
    {
        if (args.NewState is ST14HubState)
            Start();
        else
            Stop(FadeTime);
    }

    private void OnMusicEnabled(bool enabled)
    {
        if (enabled && _state.CurrentState is ST14HubState)
            Start();
        else if (!enabled)
            Stop(FadeTime);
    }

    private void OnVolume(float _)
    {
        foreach (var stream in _streams)
        {
            _audio.SetVolume(stream, Volume);
        }
    }

    private float Volume => _params.Volume + SharedAudioSystem.GainToVolume(_cfg.GetCVar(CCVars.LobbyMusicVolume));

    private void Start()
    {
        if (_playing || !_cfg.GetCVar(CCVars.LobbyMusicEnabled))
            return;

        _playing = true;
        Play(IntroPath);
        _nextLoop = _timing.RealTime + IntroLength;
    }

    private void Stop(float fade)
    {
        _playing = false;

        foreach (var stream in _streams)
        {
            // the intro is gone once it has played through
            if (!HasComp<AudioComponent>(stream))
                continue;

            if (fade > 0)
                _contentAudio.FadeOut(stream, duration: fade);
            else
                _audio.Stop(stream);
        }

        _streams.Clear();
    }

    private void Play(string path)
    {
        var result = _audio.PlayGlobal(path, Filter.Local(), false, _params.WithVolume(Volume));
        if (result == null)
            return;

        _streams.RemoveAll(stream => !Exists(stream));
        _streams.Add(result.Value.Entity);
    }

    /// <summary>
    /// The server keeps sending lobby audio while the player sits in the hub: round start and restart sounds, announcements,
    /// votes, admin music and so on. Only the hub theme plays here, everything else is stopped as soon as it appears.
    /// Interface sounds of the hub are audio sources, not entities, so they are not touched
    /// </summary>
    private void SilenceOthers()
    {
        var query = EntityQueryEnumerator<AudioComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (!_streams.Contains(uid))
                _audio.Stop(uid);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_state.CurrentState is ST14HubState)
            SilenceOthers();

        if (!_playing || _timing.RealTime < _nextLoop)
            return;

        Play(LoopPath);
        _nextLoop += LoopLength;

        // Got far behind, e.g. the game was paused in the debugger
        if (_nextLoop < _timing.RealTime)
            _nextLoop = _timing.RealTime + LoopLength;
    }
}
