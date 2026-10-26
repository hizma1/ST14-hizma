using Content.Client._ST14.MainHub;
using Content.Shared._ST14.CCVar;
using Content.Shared._ST14.Supporter;
using Robust.Shared.Configuration;

namespace Content.Client._ST14.Supporter;

/// <summary>
/// Passes the supporter tier sent by the server to <see cref="ST14SupporterStatus"/> and sends back the perk choices
/// saved in the client cvars. The server sends the tier on every connect, so the choices always reach it
/// </summary>
public sealed partial class ST14SupporterClientSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<ST14SupporterTierEvent>(OnTier);
    }

    private void OnTier(ST14SupporterTierEvent ev)
    {
        ST14SupporterStatus.Set(ev.Tier);
        SendChoices();
    }

    /// <summary>
    /// Sends the current choices to the server, called again whenever the player changes one on the support screen
    /// </summary>
    public void SendChoices()
    {
        RaiseNetworkEvent(new ST14PerkChoicesEvent(
            _cfg.GetCVar(ST14CVars.PerkOocColor),
            _cfg.GetCVar(ST14CVars.PerkGhostColor),
            _cfg.GetCVar(ST14CVars.PerkWeapon),
            _cfg.GetCVar(ST14CVars.PerkNickname)));
    }
}
