using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._ST14.ST14Resources;

[Serializable, NetSerializable]
public enum ST14ResourceExtractorUIKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public enum ST14ResourceExtractorStatus : byte
{
    Off,
    Working,
    NoSpace,
}

[Serializable, NetSerializable]
public sealed class ST14ResourceExtractorSlotState(bool hasCapsule, FixedPoint2 amount, FixedPoint2 capacity)
{
    public readonly bool HasCapsule = hasCapsule;
    public readonly FixedPoint2 Amount = amount;
    public readonly FixedPoint2 Capacity = capacity;
}

[Serializable, NetSerializable]
public sealed class ST14ResourceExtractorBoundUserInterfaceState(
    ST14ResourceExtractorStatus status,
    float rate,
    int activeSlot,
    bool autoSwitch,
    ST14ResourceExtractorSlotState[] slots,
    ProtoId<ST14ResourcePrototype> resource) : BoundUserInterfaceState
{
    public readonly ST14ResourceExtractorStatus Status = status;
    public readonly float Rate = rate;

    public readonly int ActiveSlot = activeSlot;
    public readonly bool AutoSwitch = autoSwitch;
    public readonly ST14ResourceExtractorSlotState[] Slots = slots;
    public readonly ProtoId<ST14ResourcePrototype> Resource = resource;
}

[Serializable, NetSerializable]
public sealed class ST14ResourceExtractorToggleMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class ST14ResourceExtractorSelectSlotMessage(int slot) : BoundUserInterfaceMessage
{
    public readonly int Slot = slot;
}

[Serializable, NetSerializable]
public sealed class ST14ResourceExtractorSetAutoSwitchMessage(bool enabled) : BoundUserInterfaceMessage
{
    public readonly bool Enabled = enabled;
}
