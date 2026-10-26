using System.Linq;
using Content.Client._ST14.Lobby;
using Content.Client._ST14.MainHub;
using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Fonts;
using Content.Client.Stylesheets.Palette;
using Content.Client.Stylesheets.Stylesheets;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Utility;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client._ST14.Stylesheets;

/// <summary>
/// Main game stylesheet in the look of the Terran Command menu: brass plates with the cut corner, dark glass windows,
/// gold headings, Exo 2 text. Textures with the stock names in <see cref="TextureRoot"/> replace the Nano ones,
/// everything missing there still comes from Nano
/// </summary>
public sealed class ST14Stylesheet : NanotrasenStylesheet
{
    public static readonly ResPath ST14TextureRoot = new("/Textures/_ST14/Interface/Style");

    // keeps the name, windows and code look the main sheet up by it
    public override string StylesheetName => "Nanotrasen";

    public override Dictionary<Type, ResPath[]> Roots => new()
    {
        { typeof(TextureResource), [ST14TextureRoot, TextureRoot] },
    };

    private NotoFontFamilyStack? _font;

    // Exo 2 covers Latin and Cyrillic, Noto symbols stay as fallback. There is no plain Exo 2 italic, so italic falls back to regular
    public override NotoFontFamilyStack BaseFont => _font ??= new NotoFontFamilyStack(ResCache,
        "/Fonts/_ST14/Exo2/Exo2-{0}.ttf",
        [FontKind.Regular, FontKind.Bold, FontKind.BoldItalic]);

    // brass plates, warm dark glass, gold highlights like the hub
    public override ColorPalette PrimaryPalette => ST14Palettes.Brass;
    public override ColorPalette SecondaryPalette => ST14Palettes.Glass;
    public override ColorPalette HighlightPalette => ST14Palettes.Gold;

    public ST14Stylesheet(object config, StylesheetManager man) : base(config, man)
    {
        var titleFont = ST14HubStyle.Font(ResCache, ST14HubFont.SemiBold, 13);

        StyleRule[] rules =
        [
            // window titles in the hub heading font instead of the hardcoded Boxfont
            E<Label>()
                .Class("FancyWindowTitle")
                .Font(titleFont)
                .FontColor(ST14HubStyle.GoldLight),
            E<Label>()
                .Class(DefaultWindow.StyleClassWindowTitle)
                .Font(titleFont)
                .FontColor(ST14HubStyle.GoldLight),
        ];

        // controls and windows of the whole game in the menu look, after the stock rules so they win at equal specificity
        Stylesheet = new Stylesheet(Stylesheet.Rules
            .Concat(rules)
            .Concat(ST14LobbyStyle.Rules(ResCache))
            .Concat(ST14LobbyStyle.WindowRules(ResCache))
            .ToArray());
    }
}

public static class ST14Palettes
{
    public static readonly ColorPalette Brass = ColorPalette.FromHexBase("#7a6c45", lightnessShift: 0.05f, chromaShift: 0.004f);
    public static readonly ColorPalette Glass = ColorPalette.FromHexBase("#4a463c", lightnessShift: 0.045f);
    public static readonly ColorPalette Gold = ColorPalette.FromHexBase("#d4b36a");
}
