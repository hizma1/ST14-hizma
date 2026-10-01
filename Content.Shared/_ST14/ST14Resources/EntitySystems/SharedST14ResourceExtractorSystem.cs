using Content.Shared._ST14.ST14Resources.Components;
using Content.Shared.Containers.ItemSlots;

namespace Content.Shared._ST14.ST14Resources.EntitySystems;

public abstract partial class SharedST14ResourceExtractorSystem : EntitySystem
{
    [Dependency] protected ItemSlotsSystem ItemSlots = default!;
    [Dependency] protected ST14ResourceCapsuleSystem Capsules = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ST14ResourceExtractorComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ST14ResourceExtractorComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<ST14ResourceExtractorComponent, ItemSlotInsertAttemptEvent>(OnInsertAttempt);
    }

    private void OnInit(Entity<ST14ResourceExtractorComponent> ent, ref ComponentInit args)
    {
        for (var i = 0; i < ST14ResourceExtractorComponent.SlotCount; i++)
        {
            ItemSlots.AddItemSlot(ent.Owner, ST14ResourceExtractorComponent.CapsuleSlotIds[i], GetSlot(ent.Comp, i));
        }
    }

    private void OnRemove(Entity<ST14ResourceExtractorComponent> ent, ref ComponentRemove args)
    {
        for (var i = 0; i < ST14ResourceExtractorComponent.SlotCount; i++)
        {
            ItemSlots.RemoveItemSlot(ent.Owner, GetSlot(ent.Comp, i));
        }
    }

    private void OnInsertAttempt(Entity<ST14ResourceExtractorComponent> ent, ref ItemSlotInsertAttemptEvent args)
    {
        if (args.Slot != ent.Comp.CapsuleSlot1 && args.Slot != ent.Comp.CapsuleSlot2)
            return;

        if (!TryComp<ST14ResourceCapsuleComponent>(args.Item, out var capsule) ||
            !Capsules.IsCompatible((args.Item, capsule), ent.Comp.ResourceType))
        {
            args.Cancelled = true;
        }
    }

    public static ItemSlot GetSlot(ST14ResourceExtractorComponent comp, int index)
    {
        return index == 0 ? comp.CapsuleSlot1 : comp.CapsuleSlot2;
    }

    public static int GetSlotIndex(string containerId)
    {
        return Array.IndexOf(ST14ResourceExtractorComponent.CapsuleSlotIds, containerId);
    }

    public static int OtherSlot(int index)
    {
        return (index + 1) % ST14ResourceExtractorComponent.SlotCount;
    }

    public bool TryGetCapsule(Entity<ST14ResourceExtractorComponent> ent, int index, out Entity<ST14ResourceCapsuleComponent> capsule)
    {
        capsule = default;
        if (GetSlot(ent.Comp, index).Item is not { } item ||
            !TryComp<ST14ResourceCapsuleComponent>(item, out var comp))
            return false;

        capsule = (item, comp);
        return true;
    }

    public bool CanFill(Entity<ST14ResourceExtractorComponent> ent, int index)
    {
        return TryGetCapsule(ent, index, out var capsule) && Capsules.CanAccept(capsule, ent.Comp.ResourceType);
    }
}
