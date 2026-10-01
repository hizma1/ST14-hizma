using Content.Shared._ST14.ST14Resources;
using Content.Shared._ST14.ST14Resources.Components;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._ST14.ST14Resources.UI;

[UsedImplicitly]
public sealed class ST14ResourceExtractorBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private ST14ResourceExtractorWindow? _window;

    public ST14ResourceExtractorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ST14ResourceExtractorWindow>();
        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;

        _window.OnToggle += () => SendMessage(new ST14ResourceExtractorToggleMessage());
        _window.OnSelectSlot += slot => SendMessage(new ST14ResourceExtractorSelectSlotMessage(slot));
        _window.OnAutoSwitchChanged += enabled => SendMessage(new ST14ResourceExtractorSetAutoSwitchMessage(enabled));
        _window.OnSlotButton += slot => SendPredictedMessage(
            new ItemSlotButtonPressedEvent(ST14ResourceExtractorComponent.CapsuleSlotIds[slot]));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is ST14ResourceExtractorBoundUserInterfaceState cast)
            _window?.UpdateState(cast);
    }
}
