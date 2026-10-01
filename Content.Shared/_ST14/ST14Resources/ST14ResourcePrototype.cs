using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._ST14.ST14Resources;

[Prototype("ST14Resource")]
public sealed partial class ST14ResourcePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name { get; private set; }

    [DataField]
    public Color Color { get; private set; } = Color.White;

    [DataField]
    public SpriteSpecifier? Icon { get; private set; }
}
