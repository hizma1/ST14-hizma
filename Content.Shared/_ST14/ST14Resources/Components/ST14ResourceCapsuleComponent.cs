using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ST14.ST14Resources.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ST14ResourceCapsuleComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<ST14ResourcePrototype>? ResourceType;

    [DataField, AutoNetworkedField]
    public FixedPoint2 Amount = FixedPoint2.Zero;

    [DataField(required: true)]
    public FixedPoint2 Capacity;
}
