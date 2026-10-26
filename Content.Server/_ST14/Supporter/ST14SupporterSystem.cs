using System.Linq;
using Content.Server.Administration.Logs;
using Content.Shared._ST14.Supporter;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.Ghost.Components;
using Content.Shared.Ghost.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Hands;
using Content.Shared.Inventory.Events;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._ST14.Supporter;

/// <summary>
/// Supporter tiers of players and the perks they unlock:
/// tier 1 colours the name in OOC chat, tier 2 colours the ghost and names a weapon type.
/// There is no Boosty backend yet: tiers only come from the st14_sponsor test command, live in memory and are gone
/// after a server restart. A real backend would call <see cref="SetTier"/> when the player connects
/// </summary>
public sealed partial class ST14SupporterSystem : EntitySystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IPrototypeManager _protoMan = default!;
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private MetaDataSystem _meta = default!;
    [Dependency] private SharedGhostSystem _ghost = default!;

    public const int OocColorTier = 1;
    public const int GhostColorTier = 2;
    public const int NicknameTier = 2;

    private readonly Dictionary<NetUserId, int> _tiers = new();
    private readonly Dictionary<NetUserId, Choices> _choices = new();

    public override void Initialize()
    {
        base.Initialize();
        _player.PlayerStatusChanged += OnPlayerStatusChanged;
        SubscribeNetworkEvent<ST14PerkChoicesEvent>(OnChoices);
        SubscribeLocalEvent<GhostComponent, PlayerAttachedEvent>(OnGhostAttached);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnSpawnComplete);
        SubscribeLocalEvent<GunComponent, GotEquippedHandEvent>(OnGunPickedUp);
        SubscribeLocalEvent<GunComponent, GotEquippedEvent>(OnGunEquipped);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _player.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    public int GetTier(NetUserId user)
    {
        return _tiers.GetValueOrDefault(user);
    }

    public void SetTier(ICommonSession session, int tier)
    {
        tier = Math.Clamp(tier, 0, ST14SupporterTierEvent.MaxTier);
        if (tier == 0)
            _tiers.Remove(session.UserId);
        else
            _tiers[session.UserId] = tier;

        RaiseNetworkEvent(new ST14SupporterTierEvent(tier), session);
        ApplyGhostColor(session);
    }

    /// <summary>
    /// OOC name colour of a supporter, null when they have none or their tier is too low
    /// </summary>
    public Color? GetOocColor(NetUserId user)
    {
        return GetTier(user) >= OocColorTier && _choices.TryGetValue(user, out var choices) ? choices.OocColor : null;
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        // the tier goes out on every connect, the client answers with its saved perk choices
        if (args.NewStatus == SessionStatus.Connected)
            RaiseNetworkEvent(new ST14SupporterTierEvent(GetTier(args.Session.UserId)), args.Session);
    }

    private void OnChoices(ST14PerkChoicesEvent ev, EntitySessionEventArgs args)
    {
        var choices = new Choices(ParseColor(ev.OocColor), ParseColor(ev.GhostColor), null, CleanNickname(ev.Nickname));
        if (_protoMan.TryIndex<EntityPrototype>(ev.Weapon, out var weapon) && weapon.Components.ContainsKey("Gun"))
            choices = choices with { Weapon = weapon.ID };

        _choices[args.SenderSession.UserId] = choices;

        if (choices.Weapon != null && choices.Nickname != null)
        {
            _adminLog.Add(LogType.Action, LogImpact.Low,
                $"{args.SenderSession.Name} picked the supporter nickname \"{choices.Nickname}\" for {choices.Weapon}");
        }

        ApplyGhostColor(args.SenderSession);
    }

    private static Color? ParseColor(string hex)
    {
        return Color.TryFromHex(hex, out var color) ? color.WithAlpha(1f) : null;
    }

    /// <summary>
    /// No markup, no line breaks, at most 24 characters
    /// </summary>
    private static string? CleanNickname(string text)
    {
        var clean = new string(text.Where(c => c is not ('[' or ']' or '\n' or '\r' or '«' or '»' or '"')).ToArray()).Trim();
        if (clean.Length > ST14PerkChoicesEvent.MaxNickname)
            clean = clean[..ST14PerkChoicesEvent.MaxNickname].Trim();

        return clean.Length == 0 ? null : clean;
    }

    #region Ghost colour

    private void OnGhostAttached(EntityUid uid, GhostComponent ghost, PlayerAttachedEvent args)
    {
        ApplyGhostColor(args.Player);
    }

    private void ApplyGhostColor(ICommonSession session)
    {
        if (session.AttachedEntity is not { } entity || !TryComp<GhostComponent>(entity, out var ghost))
            return;

        var color = GetTier(session.UserId) >= GhostColorTier && _choices.TryGetValue(session.UserId, out var choices)
            ? choices.GhostColor ?? Color.White
            : Color.White;

        _ghost.SetColor((entity, ghost), color);
    }

    #endregion

    #region Weapon nickname

    // Rule: a weapon of the picked type gets the nickname when the supporter spawns with it or takes it into a hand
    // or slot, unless it already carries a nickname. The name stays with the weapon when someone else picks it up

    private void OnSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (!TryGetNickname(args.Player, out var weapon, out var nickname))
            return;

        foreach (var item in Contained(args.Mob))
        {
            TryNickname(item, weapon, nickname);
        }
    }

    private void OnGunPickedUp(EntityUid gun, GunComponent comp, GotEquippedHandEvent args)
    {
        if (_player.TryGetSessionByEntity(args.User, out var session) && TryGetNickname(session, out var weapon, out var nickname))
            TryNickname(gun, weapon, nickname);
    }

    private void OnGunEquipped(EntityUid gun, GunComponent comp, GotEquippedEvent args)
    {
        if (_player.TryGetSessionByEntity(args.EquipTarget, out var session) && TryGetNickname(session, out var weapon, out var nickname))
            TryNickname(gun, weapon, nickname);
    }

    private bool TryGetNickname(ICommonSession session, out string weapon, out string nickname)
    {
        weapon = string.Empty;
        nickname = string.Empty;
        if (GetTier(session.UserId) < NicknameTier || !_choices.TryGetValue(session.UserId, out var choices)
            || choices.Weapon == null || choices.Nickname == null)
            return false;

        weapon = choices.Weapon;
        nickname = choices.Nickname;
        return true;
    }

    private void TryNickname(EntityUid item, string weapon, string nickname)
    {
        if (MetaData(item).EntityPrototype?.ID != weapon || HasComp<ST14NicknamedComponent>(item))
            return;

        AddComp<ST14NicknamedComponent>(item);
        var name = Loc.GetString("st14-weapon-nickname", ("weapon", MetaData(item).EntityName), ("nickname", nickname));
        _meta.SetEntityName(item, name);
    }

    private IEnumerable<EntityUid> Contained(EntityUid uid)
    {
        if (!TryComp<ContainerManagerComponent>(uid, out var manager))
            yield break;

        foreach (var container in manager.Containers.Values)
        {
            foreach (var child in container.ContainedEntities)
            {
                yield return child;
                foreach (var inner in Contained(child))
                {
                    yield return inner;
                }
            }
        }
    }

    #endregion

    private sealed record Choices(Color? OocColor, Color? GhostColor, string? Weapon, string? Nickname);
}

/// <summary>
/// Marks a weapon that already got a supporter nickname
/// </summary>
[RegisterComponent]
public sealed partial class ST14NicknamedComponent : Component;
