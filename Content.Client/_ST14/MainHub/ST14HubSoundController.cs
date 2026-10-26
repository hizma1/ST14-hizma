using Content.Shared.CCVar;
using Robust.Client.Audio;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Audio.Sources;
using Robust.Shared.Configuration;

namespace Content.Client._ST14.MainHub;

public enum ST14HubSound : byte
{
    Hover,
    Click,
    Tab,
    Back,
}

/// <summary>
/// Interface sounds of the hub. Loaded outside of the game sim like the regular UI click sounds
/// </summary>
public sealed partial class ST14HubSoundController : UIController
{
    [Dependency] private IAudioManager _audioManager = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IResourceCache _cache = default!;

    private const string SoundDir = "/Audio/_ST14/Interface/Hub/";

    private static readonly Dictionary<ST14HubSound, (string File, float Gain)> Sounds = new()
    {
        [ST14HubSound.Hover] = ("hover.ogg", 0.35f),
        [ST14HubSound.Click] = ("click.ogg", 0.5f),
        [ST14HubSound.Tab] = ("tab.ogg", 0.5f),
        [ST14HubSound.Back] = ("back.ogg", 0.5f),
    };

    private readonly Dictionary<ST14HubSound, IAudioSource> _sources = new();
    private float _interfaceGain;

    public override void Initialize()
    {
        base.Initialize();
        _cfg.OnValueChanged(CCVars.InterfaceVolume, OnInterfaceVolume, true);
    }

    private void OnInterfaceVolume(float value)
    {
        _interfaceGain = value;
        foreach (var (sound, source) in _sources)
        {
            source.Gain = Sounds[sound].Gain * _interfaceGain;
        }
    }

    public void Play(ST14HubSound sound)
    {
        if (!_sources.TryGetValue(sound, out var source))
        {
            var (file, gain) = Sounds[sound];
            if (!_cache.TryGetResource(SoundDir + file, out AudioResource? resource))
                return;

            var created = _audioManager.CreateAudioSource(resource);
            if (created == null)
                return;

            created.Global = true;
            created.Gain = gain * _interfaceGain;
            _sources[sound] = created;
            source = created;
        }

        source.Restart();
    }
}
