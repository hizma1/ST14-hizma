using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._ST14.MainHub;

/// <summary>
/// Metal plate button with the cut corner from the original menu
/// </summary>
public sealed class ST14HubButton : ContainerButton
{
    private readonly IResourceCache _cache;
    private readonly Label _label;
    private readonly Color _tint;

    public ST14HubSound PressSound = ST14HubSound.Click;

    public ST14HubButton(IResourceCache cache, string text, int fontSize = 25, Color? tint = null, Color? textColor = null)
    {
        _cache = cache;
        _tint = tint ?? Color.White;
        MuteSounds = true;

        _label = ST14HubStyle.Label(cache, text.ToUpperInvariant(), ST14HubFont.Regular, fontSize, textColor ?? Color.White, shadow: true);
        _label.Align = Label.AlignMode.Center;
        _label.VAlign = Label.VAlignMode.Center;
        _label.ClipText = true;
        _label.Margin = new Thickness(6, 0, 14, 3);
        AddChild(_label);

        OnPressed += _ => Sounds.Play(PressSound);
        UpdateLook();
    }

    public string Text
    {
        get => _label.Text ?? string.Empty;
        set => _label.Text = value.ToUpperInvariant();
    }

    public Label Label => _label;

    private ST14HubSoundController Sounds => UserInterfaceManager.GetUIController<ST14HubSoundController>();

    protected override void MouseEntered()
    {
        base.MouseEntered();

        if (!Disabled)
            Sounds.Play(ST14HubSound.Hover);
    }

    protected override void DrawModeChanged()
    {
        base.DrawModeChanged();
        UpdateLook();
    }

    private void UpdateLook()
    {
        // BaseButton can report a draw mode change before the label exists
        if (_label == null)
            return;

        var tint = DrawMode switch
        {
            DrawModeEnum.Hover => Brighten(_tint, 1.18f),
            DrawModeEnum.Pressed => Brighten(_tint, 0.8f),
            DrawModeEnum.Disabled => _tint.WithAlpha(0.45f),
            _ => _tint,
        };

        StyleBoxOverride = ST14HubStyle.MainButton(_cache, tint);
        _label.Modulate = Disabled ? Color.White.WithAlpha(0.5f) : Color.White;
    }

    private static Color Brighten(Color color, float factor)
    {
        return new Color(
            MathF.Min(1f, color.R * factor),
            MathF.Min(1f, color.G * factor),
            MathF.Min(1f, color.B * factor),
            color.A);
    }
}

/// <summary>
/// Flat icon button, brightens on hover like the gear and power icons of the original menu
/// </summary>
public sealed class ST14HubIconButton : TextureButton
{
    private readonly Color _tint;

    public ST14HubSound PressSound = ST14HubSound.Click;

    public ST14HubIconButton(Texture texture, Color tint)
    {
        _tint = tint;
        TextureNormal = texture;
        MuteSounds = true;
        Modulate = tint;

        OnPressed += _ => Sounds.Play(PressSound);
    }

    private ST14HubSoundController Sounds => UserInterfaceManager.GetUIController<ST14HubSoundController>();

    protected override void MouseEntered()
    {
        base.MouseEntered();
        Sounds.Play(ST14HubSound.Hover);
    }

    protected override void DrawModeChanged()
    {
        base.DrawModeChanged();

        Modulate = DrawMode switch
        {
            DrawModeEnum.Hover => Color.White,
            DrawModeEnum.Pressed => _tint.WithAlpha(0.7f),
            _ => _tint,
        };
    }
}
