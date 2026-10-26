using Robust.Shared.Serialization;

namespace Content.Shared._ST14.Supporter;

/// <summary>
/// Server tells a player their supporter tier, 0 is none and 1 to 4 are the Boosty sponsorships
/// </summary>
[Serializable, NetSerializable]
public sealed class ST14SupporterTierEvent(int tier) : EntityEventArgs
{
    public const int MaxTier = 4;

    public readonly int Tier = tier;
}

/// <summary>
/// Client tells the server what it picked for its supporter perks. The server checks the tier before using any of it
/// </summary>
[Serializable, NetSerializable]
public sealed class ST14PerkChoicesEvent(string oocColor, string ghostColor, string weapon, string nickname) : EntityEventArgs
{
    public const int MaxNickname = 24;

    public readonly string OocColor = oocColor;
    public readonly string GhostColor = ghostColor;
    public readonly string Weapon = weapon;
    public readonly string Nickname = nickname;
}
