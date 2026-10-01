using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._ST14.ST14Resources.UI;

public sealed class ST14ResourceGauge : Control
{
    private const float TubeWidth = 36f;
    private const float LabelGap = 6f;

    private static readonly Color TubeColor = Color.FromHex("#2A2E34");
    private static readonly Color BorderColor = Color.FromHex("#A0A6AE");
    private static readonly Color ScaleColor = Color.FromHex("#C8CDD3");

    private float _amount;
    private float _capacity;

    public Color FillColor { get; set; } = Color.White;

    public bool LabelsOnLeft { get; set; }

    public int MajorDivisions { get; set; } = 2;

    public int MinorPerMajor { get; set; } = 2;

    public void SetValues(float amount, float capacity)
    {
        _amount = amount;
        _capacity = capacity;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var scale = UIScale;
        var font = TryGetStyleProperty<Font>(Label.StylePropertyFont, out var styleFont)
            ? styleFont
            : UserInterfaceManager.ThemeDefaults.DefaultFont;
        var lineHeight = font.GetLineHeight(scale);

        var tubeWidth = TubeWidth * scale;
        var left = LabelsOnLeft ? PixelSize.X - tubeWidth : 0f;
        var right = left + tubeWidth;
        var top = lineHeight / 2f;
        var bottom = PixelSize.Y - lineHeight / 2f;
        var height = bottom - top;
        var tube = new UIBox2(left, top, right, bottom);

        handle.DrawRect(tube, TubeColor);

        var fraction = _capacity > 0f ? Math.Clamp(_amount / _capacity, 0f, 1f) : 0f;
        if (fraction > 0f)
        {
            var fillTop = bottom - height * fraction;
            handle.DrawRect(new UIBox2(left, fillTop, right, bottom), FillColor);

            handle.DrawLine(new Vector2(left, fillTop), new Vector2(right, fillTop),
                Color.InterpolateBetween(FillColor, Color.White, 0.6f));
        }

        var minor = Math.Max(1, MinorPerMajor);
        var steps = Math.Max(1, MajorDivisions) * minor;
        var gap = LabelGap * scale;

        for (var i = 0; i <= steps; i++)
        {
            var y = bottom - height * i / steps;
            var major = i % minor == 0;
            var length = (major ? 0.5f : 0.25f) * tubeWidth;

            if (LabelsOnLeft)
                handle.DrawLine(new Vector2(left, y), new Vector2(left + length, y), ScaleColor);
            else
                handle.DrawLine(new Vector2(right - length, y), new Vector2(right, y), ScaleColor);

            if (!major || _capacity <= 0f)
                continue;

            var text = MathF.Round(_capacity * i / steps).ToString();
            var size = handle.GetDimensions(font, text, scale);
            var x = LabelsOnLeft ? left - gap - size.X : right + gap;
            handle.DrawString(font, new Vector2(x, y - size.Y / 2f), text, scale, ScaleColor);
        }

        handle.DrawRect(tube, BorderColor, filled: false);
    }
}
