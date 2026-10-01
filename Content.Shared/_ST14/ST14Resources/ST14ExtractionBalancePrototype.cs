using Robust.Shared.Prototypes;

namespace Content.Shared._ST14.ST14Resources;

[Prototype("ST14ExtractionBalance")]
public sealed partial class ST14ExtractionBalancePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public List<ST14CampaignModifierThreshold> CampaignThresholds = new();

    [DataField]
    public float MinModifier = -0.5f;

    [DataField]
    public float MaxModifier = 0.5f;
}

[DataDefinition]
public sealed partial class ST14CampaignModifierThreshold
{
    [DataField(required: true)]
    public float UpTo;

    [DataField(required: true)]
    public float Modifier;
}
