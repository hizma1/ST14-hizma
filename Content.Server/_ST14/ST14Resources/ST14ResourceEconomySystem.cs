using Content.Shared._ST14.CCVar;
using Content.Shared._ST14.ST14Resources;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Server._ST14.ST14Resources;

public sealed partial class ST14ResourceEconomySystem : EntitySystem
{
    private const float MinDifficulty = 0.01f;

    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPrototypeManager _prototype = default!;

    private static readonly ProtoId<ST14ExtractionBalancePrototype> BalanceId = "Default";

    private float? _winRatio;
    private float _campaignModifier;
    private float _difficultyMultiplier = 1f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        Subs.CVar(_cfg, ST14CVars.DifficultyMultiplier, value => _difficultyMultiplier = MathF.Max(MinDifficulty, value), true);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<ST14ExtractionBalancePrototype>())
            RecalculateCampaignModifier();
    }

    public void SetCampaignWinRatio(float? ratio)
    {
        _winRatio = ratio;
        RecalculateCampaignModifier();
    }

    public float GetExtractionModifier(EntityUid extractor)
    {
        var ev = new ST14GetExtractionModifierEvent(0f);
        RaiseLocalEvent(extractor, ref ev);

        var balance = _prototype.Index(BalanceId);
        return Math.Clamp(_campaignModifier + ev.Modifier, balance.MinModifier, balance.MaxModifier);
    }

    public float GetExtractionMultiplier(EntityUid extractor)
    {
        return (1f + GetExtractionModifier(extractor)) / _difficultyMultiplier;
    }

    private void RecalculateCampaignModifier()
    {
        _campaignModifier = 0f;
        if (_winRatio is not { } ratio)
            return;

        foreach (var threshold in _prototype.Index(BalanceId).CampaignThresholds)
        {
            if (ratio < threshold.UpTo)
            {
                _campaignModifier = threshold.Modifier;
                return;
            }
        }
    }
}
