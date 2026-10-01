using Robust.Shared.Prototypes;

namespace Content.Shared._ST14.ST14Resources.Components;

[RegisterComponent]
public sealed partial class ST14ResourceFieldComponent : Component
{
    [DataField(required: true)]
    public ProtoId<ST14ResourcePrototype> ResourceType;
}
