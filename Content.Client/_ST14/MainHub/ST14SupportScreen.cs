using System.Linq;
using System.Numerics;
using Content.Client._ST14.Lobby;
using Content.Client._ST14.Supporter;
using Content.Client.Lobby;
using Content.Client.Lobby.UI.ProfileEditorControls;
using Content.Shared._ST14.CCVar;
using Content.Shared.Preferences;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._ST14.MainHub;

/// <summary>
/// Supporter tier of the local player, 0 is none and 1 to 4 are the Boosty sponsorships. The server sends it
/// (see ST14SupporterClientSystem); for now only the st14_sponsor test command sets it there
/// </summary>
public static class ST14SupporterStatus
{
    public static int Tier { get; private set; }

    public static bool IsSupporter => Tier > 0;

    public static event Action? Changed;

    public static void Set(int tier)
    {
        Tier = Math.Clamp(tier, 0, 4);
        Changed?.Invoke();
    }

    /// <summary>
    /// Accent of every tier: bronze, silver, gold and the red of the elite tier. Index 0 is "no subscription"
    /// </summary>
    public static readonly Color[] TierColors =
    {
        Color.FromHex("#8A8478"), Color.FromHex("#D08C4E"), Color.FromHex("#C9D4E0"), Color.FromHex("#F2C14E"), Color.FromHex("#E8604C"),
    };

    public static readonly string[] Numerals = { "—", "I", "II", "III", "IV" };
}

/// <summary>
/// "Support" screen of the main menu and the lobby: the player's tier with a medal, how to get a subscription,
/// a plate leading to Boosty, the four sponsorship tiers and the perks. Perks that already work in the game
/// (OOC and ghost colour, weapon nickname) save the choice and send it to the server, the rest is shown only
/// </summary>
public sealed partial class ST14SupportScreen : ST14WarScreenBase
{
    [Dependency] private IUriOpener _uriOpener = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private IPrototypeManager _protoMan = default!;
    [Dependency] private IClientPreferencesManager _prefs = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IEntityManager _entMan = default!;

    public const string BoostyUrl = "https://boosty.to/corvaxcolonialmarines";

    // price and the number of perk lines of every tier, the texts are in the locale
    private static readonly (string Price, int Perks)[] Tiers = { ("300", 3), ("600", 2), ("1 300", 3), ("4 200", 2) };

    // the preview tab each tier card leads to
    private static readonly int[] TierPages = { 1, 2, 3, 4 };

    private static readonly Color[] Swatches =
    {
        Color.FromHex("#F7D68C"), Color.FromHex("#FF6B5A"), Color.FromHex("#FF9A3C"), Color.FromHex("#7CE07C"),
        Color.FromHex("#5CBFE6"), Color.FromHex("#8C7CFF"), Color.FromHex("#E67CE0"), Color.FromHex("#FFFFFF"),
    };

    private readonly ST14TierMedal _medal;
    private readonly Label _status;
    private readonly RichTextLabel _statusText;
    private readonly List<PanelContainer> _steps = new();
    private readonly List<(Button Tab, Control Page)> _pages = new();
    private readonly List<TierCard> _tierCards = new();
    private readonly List<(Label Label, int Tier, string Perk)> _locks = new();
    private readonly List<(Button Swatch, Color Color)> _oocSwatches = new();
    private readonly List<(Button Swatch, Color Color)> _ghostSwatches = new();

    private Label _oocPreview = default!;
    private EntityPrototypeView _ghost = default!;
    private readonly ProfilePreviewSpriteView _figure;
    private readonly ST14WoodenStand _stand;
    private readonly EntityPrototypeView _weaponView;
    private readonly Label _weaponPreview;
    private readonly List<EntityPrototype> _weapons;
    private int _weapon;
    private string _nickname = string.Empty;

    public ST14SupportScreen() : base("ST14SupportBackground")
    {
        BuildHeader();

        // left: the player's status, how to subscribe and the way to Boosty
        var statusFrame = new ST14LobbyFrame(ResCache, Loc.GetString("st14-support-status-header"));
        Canvas.AddAt(statusFrame, new Vector2(0.5f, 0.5f), new Vector2(-640, 40), new Vector2(460, 700));

        _medal = new ST14TierMedal(ResCache) { SetSize = new Vector2(150, 150), HorizontalAlignment = HAlignment.Center };
        statusFrame.Body.AddChild(_medal);

        _status = ST14HubStyle.Label(ResCache, string.Empty, ST14HubFont.SemiBold, 24, ST14HubStyle.GoldLight);
        _status.Align = Label.AlignMode.Center;
        statusFrame.Body.AddChild(_status);

        var steps = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 6,
            HorizontalAlignment = HAlignment.Center,
        };
        for (var tier = 1; tier <= 4; tier++)
        {
            var step = new PanelContainer { SetSize = new Vector2(64, 30) };
            var numeral = ST14HubStyle.Label(ResCache, ST14SupporterStatus.Numerals[tier], ST14HubFont.SemiBold, 15, Color.White);
            numeral.Align = Label.AlignMode.Center;
            numeral.VAlign = Label.VAlignMode.Center;
            step.AddChild(numeral);
            steps.AddChild(step);
            _steps.Add(step);
        }

        statusFrame.Body.AddChild(steps);

        _statusText = Text(string.Empty, 380);
        statusFrame.Body.AddChild(_statusText);

        statusFrame.Body.AddChild(SectionTitle("st14-support-how-header"));
        statusFrame.Body.AddChild(Text("st14-support-how-text", 380));
        statusFrame.Body.AddChild(new Control { VerticalExpand = true });

        var boosty = new ST14HubButton(ResCache, Loc.GetString("st14-support-boosty"), 21, ST14HubStyle.Orange, Color.White)
        {
            MinHeight = 70,
            ToolTip = BoostyUrl,
        };
        boosty.OnPressed += _ => _uriOpener.OpenUri(BoostyUrl);
        statusFrame.Body.AddChild(boosty);
        statusFrame.Body.AddChild(Text("st14-support-boosty-note", 380));

        // right: tiers and the perk previews
        var showcase = new ST14LobbyFrame(ResCache, Loc.GetString("st14-support-perks-header"));
        Canvas.AddAt(showcase, new Vector2(0.5f, 0.5f), new Vector2(250, 40), new Vector2(1200, 700));
        var tabs = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, SeparationOverride = 6 };
        showcase.Body.AddChild(tabs);

        AddPage(tabs, showcase, "st14-support-tab-tiers", BuildTiers());

        // OOC name colour (tier 1) and ghost colour (tier 2), two cards side by side
        var colours = Page();
        var colourCards = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, SeparationOverride = 14, VerticalExpand = true };
        colours.AddChild(colourCards);

        var oocCard = Card();
        colourCards.AddChild(oocCard.Root);
        oocCard.Body.AddChild(Lock(1, "st14-support-perk-ooc"));
        var chat = new PanelContainer
        {
            PanelOverride = ST14HubStyle.Flat(Color.FromHex("#0A0A08F2"), ST14HubStyle.Gold.WithAlpha(0.35f), 1),
            VerticalExpand = true,
        };
        var chatLines = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, SeparationOverride = 6, Margin = new Thickness(12, 10) };
        chat.AddChild(chatLines);
        chatLines.AddChild(ChatLine(Loc.GetString("st14-support-ooc-other-1"), Color.FromHex("#C9C2B0")));
        _oocPreview = ChatLine(Loc.GetString("st14-support-ooc-preview", ("name", _player.LocalSession?.Name ?? "?")), Swatches[0]);
        chatLines.AddChild(_oocPreview);
        chatLines.AddChild(ChatLine(Loc.GetString("st14-support-ooc-other-2"), Color.FromHex("#C9C2B0")));
        oocCard.Body.AddChild(chat);
        oocCard.Body.AddChild(SectionTitle("st14-support-pick-colour"));
        oocCard.Body.AddChild(SwatchGrid(_oocSwatches, color =>
        {
            _oocPreview.FontColorOverride = color;
            SaveChoice(ST14CVars.PerkOocColor, color.ToHex());
        }));

        var ghostCard = Card();
        colourCards.AddChild(ghostCard.Root);
        ghostCard.Body.AddChild(Lock(2, "st14-support-perk-ghost"));
        var ghostStand = new ST14Pedestal(ResCache) { SetSize = new Vector2(220, 230), HorizontalAlignment = HAlignment.Center, VerticalExpand = true };
        _ghost = new EntityPrototypeView
        {
            Scale = new Vector2(5, 5),
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, 24),
        };
        _ghost.SetPrototype("MobObserver");
        ghostStand.AddChild(_ghost);
        ghostCard.Body.AddChild(ghostStand);
        ghostCard.Body.AddChild(SectionTitle("st14-support-pick-colour"));
        ghostCard.Body.AddChild(SwatchGrid(_ghostSwatches, color =>
        {
            _ghost.Modulate = color;
            SaveChoice(ST14CVars.PerkGhostColor, color.ToHex());
        }));

        colours.AddChild(Text("st14-support-colours-text", 1100));

        // what was picked before
        if (Color.TryFromHex(_cfg.GetCVar(ST14CVars.PerkOocColor), out var ooc))
        {
            _oocPreview.FontColorOverride = ooc;
            MarkSwatch(_oocSwatches, ooc);
        }
        else
        {
            MarkSwatch(_oocSwatches, Swatches[0]);
        }

        if (Color.TryFromHex(_cfg.GetCVar(ST14CVars.PerkGhostColor), out var ghostColor))
        {
            _ghost.Modulate = ghostColor;
            MarkSwatch(_ghostSwatches, ghostColor);
        }

        AddPage(tabs, showcase, "st14-support-tab-colours", colours);

        // naming an item: the trooper guns, the engineer tool pistol is not a weapon to name
        var naming = Page();
        naming.AddChild(Lock(2, "st14-support-perk-naming"));
        _weapons = _protoMan.EnumeratePrototypes<EntityPrototype>()
            .Where(p => !p.Abstract && p.ID.StartsWith("ST14Weapon") && !p.ID.Contains("Engineer") && p.Components.ContainsKey("Gun"))
            .OrderBy(p => p.Name)
            .ToList();
        var weaponRow = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, SeparationOverride = 14, VerticalExpand = true };

        var listCard = Card();
        listCard.Root.HorizontalExpand = false;
        listCard.Root.SetWidth = 300;
        listCard.Body.AddChild(SectionTitle("st14-support-weapons-header"));
        var list = new ItemList { VerticalExpand = true, SelectMode = ItemList.ItemListSelectMode.Single };
        foreach (var weapon in _weapons)
        {
            list.AddItem(weapon.Name);
        }

        list.OnItemSelected += args =>
        {
            SelectWeapon(args.ItemIndex);
            SaveChoice(ST14CVars.PerkWeapon, _weapons[_weapon].ID);
        };
        listCard.Body.AddChild(list);
        weaponRow.AddChild(listCard.Root);

        // display case: the gun on a lit stand and a brass plate with its new name
        var caseCard = Card();
        weaponRow.AddChild(caseCard.Root);
        var display = new ST14Pedestal(ResCache) { SetHeight = 250, HorizontalExpand = true };
        _weaponView = new EntityPrototypeView
        {
            Scale = new Vector2(8, 8),
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            Margin = new Thickness(0, 0, 0, 30),
        };
        display.AddChild(_weaponView);
        caseCard.Body.AddChild(display);

        var plate = new PanelContainer
        {
            PanelOverride = ST14HubStyle.Flat(Color.FromHex("#C9A253"), Color.FromHex("#6B5226"), 2),
            HorizontalAlignment = HAlignment.Center,
            MinWidth = 420,
        };
        _weaponPreview = ST14HubStyle.Label(ResCache, string.Empty, ST14HubFont.SemiBold, 20, Color.FromHex("#2A1A08"));
        _weaponPreview.Align = Label.AlignMode.Center;
        _weaponPreview.ClipText = true;
        _weaponPreview.Margin = new Thickness(16, 6);
        plate.AddChild(_weaponPreview);
        caseCard.Body.AddChild(plate);

        var inputRow = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, SeparationOverride = 8, HorizontalAlignment = HAlignment.Center };
        var input = new LineEdit { PlaceHolder = Loc.GetString("st14-support-nickname-placeholder"), MinHeight = 38, MinWidth = 360 };
        input.OnTextChanged += args =>
        {
            _nickname = args.Text;
            UpdateWeapon();
        };
        input.OnTextEntered += _ => SaveNickname();
        inputRow.AddChild(input);
        var save = new Button { Text = Loc.GetString("st14-support-nickname-save"), StyleClasses = { "ButtonBig" }, MinWidth = 220, MinHeight = 38 };
        save.OnPressed += _ => SaveNickname();
        inputRow.AddChild(save);
        caseCard.Body.AddChild(inputRow);
        caseCard.Body.AddChild(Text("st14-support-nickname-text", 760));
        naming.AddChild(weaponRow);

        // the weapon and nickname picked before
        var savedWeapon = _weapons.FindIndex(w => w.ID == _cfg.GetCVar(ST14CVars.PerkWeapon));
        _weapon = Math.Max(0, savedWeapon);
        _nickname = _cfg.GetCVar(ST14CVars.PerkNickname);
        input.Text = _nickname;
        if (_weapons.Count > 0)
            list[_weapon].Selected = true;
        AddPage(tabs, showcase, "st14-support-tab-naming", naming);

        // figurine of the selected character on a wooden stand
        var figurine = Page();
        figurine.AddChild(Lock(3, "st14-support-perk-figurine"));
        var figureRow = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, SeparationOverride = 14, VerticalExpand = true };
        var standCard = Card();
        standCard.Root.HorizontalExpand = false;
        standCard.Root.SetWidth = 380;
        _stand = new ST14WoodenStand(ResCache) { VerticalExpand = true, HorizontalExpand = true, MinHeight = 400 };
        _figure = new ProfilePreviewSpriteView
        {
            Scale = new Vector2(5, 5),
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, 92),
            OverrideDirection = Direction.South,
        };
        _stand.AddChild(_figure);
        standCard.Body.AddChild(_stand);
        figureRow.AddChild(standCard.Root);

        var aboutCard = Card();
        aboutCard.Body.AddChild(new Control { VerticalExpand = true });
        aboutCard.Body.AddChild(SectionTitle("st14-support-figurine-title"));
        aboutCard.Body.AddChild(Text("st14-support-figurine-text", 660));
        aboutCard.Body.AddChild(Text("st14-support-figurine-note", 660));
        aboutCard.Body.AddChild(new Control { VerticalExpand = true });
        figureRow.AddChild(aboutCard.Root);
        figurine.AddChild(figureRow);
        AddPage(tabs, showcase, "st14-support-tab-figurine", figurine);

        // cosmetics are not made yet: a stamp says so
        var cosmetics = Page();
        cosmetics.AddChild(Lock(3, "st14-support-perk-cosmetics"));
        cosmetics.AddChild(new Control { VerticalExpand = true });
        cosmetics.AddChild(new ST14StampControl(ResCache, Loc.GetString("st14-support-soon"))
        {
            SetSize = new Vector2(520, 120),
            HorizontalAlignment = HAlignment.Center,
        });
        var soon = Text("st14-support-cosmetics-text", 760);
        soon.HorizontalAlignment = HAlignment.Center;
        cosmetics.AddChild(soon);
        cosmetics.AddChild(new Control { VerticalExpand = true });
        AddPage(tabs, showcase, "st14-support-tab-cosmetics", cosmetics);

        AddBottomBar();

        SelectWeapon(_weapon);
        SelectPage(0);
        UpdateStatus();
        ST14SupporterStatus.Changed += UpdateStatus;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        ST14SupporterStatus.Changed -= UpdateStatus;
    }

    protected override void VisibilityChanged(bool newVisible)
    {
        base.VisibilityChanged(newVisible);

        // the figurine is the character the player has selected right now, in the clothes they gave it
        if (newVisible)
            LoadFigure();
    }

    #region Building

    private void BuildHeader()
    {
        // dark strip with a gold line under it, the title and a short line of what this screen is about
        Canvas.Add(new TextureRect
        {
            Texture = ST14HubStyle.Tex(ResCache, "mask_bottom.png"),
            Stretch = TextureRect.StretchMode.Scale,
            ModulateSelfOverride = Color.Black.WithAlpha(0.85f),
        }, Vector2.Zero, new Vector2(1, 0), Vector2.Zero, new Vector2(0, 170), new Vector2(0.5f, 0));

        Canvas.Add(new TextureRect
        {
            Texture = ST14HubStyle.Tex(ResCache, "objectivebanner.png"),
            Stretch = TextureRect.StretchMode.Scale,
            ModulateSelfOverride = ST14HubStyle.Gold.WithAlpha(0.8f),
        }, Vector2.Zero, new Vector2(1, 0), new Vector2(0, 128), new Vector2(0, 4), new Vector2(0.5f, 0));

        var title = ST14HubStyle.Label(ResCache, Loc.GetString("st14-support-title").ToUpperInvariant(), ST14HubFont.Black, 36,
            ST14HubStyle.GoldLight, shadow: true);
        title.Align = Label.AlignMode.Center;
        Canvas.AddAt(title, new Vector2(0.5f, 0), new Vector2(0, 52), new Vector2(1400, 60));

        var subtitle = ST14HubStyle.Label(ResCache, Loc.GetString("st14-support-subtitle"), ST14HubFont.Regular, 17, ST14LobbyStyle.Text);
        subtitle.Align = Label.AlignMode.Center;
        Canvas.AddAt(subtitle, new Vector2(0.5f, 0), new Vector2(0, 100), new Vector2(1400, 30));
    }

    private BoxContainer BuildTiers()
    {
        var grid = new GridContainer { Columns = 2, HSeparationOverride = 12, VSeparationOverride = 12 };
        for (var i = 0; i < Tiers.Length; i++)
        {
            var card = new TierCard(this, i + 1);
            grid.AddChild(card.Root);
            _tierCards.Add(card);
        }

        var page = Page();
        page.AddChild(grid);
        page.AddChild(Text("st14-support-tiers-hint", 1100));
        return page;
    }

    /// <summary>
    /// One sponsorship: an accent strip, medal, name and price, a chip for the tiers it includes and the perks
    /// </summary>
    private sealed class TierCard
    {
        public readonly ContainerButton Root;
        public readonly int Tier;
        private readonly Label _badge;
        private readonly RichTextLabel _perks;
        private readonly ST14SupportScreen _screen;

        public TierCard(ST14SupportScreen screen, int tier)
        {
            _screen = screen;
            Tier = tier;
            var cache = screen.ResCache;
            var accent = ST14SupporterStatus.TierColors[tier];
            var (price, _) = Tiers[tier - 1];

            Root = new ContainerButton { HorizontalExpand = true, MinHeight = 218 };
            Root.OnPressed += _ => screen.SelectPage(TierPages[tier - 1]);

            var column = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, SeparationOverride = 6 };
            Root.AddChild(column);
            column.AddChild(new PanelContainer { PanelOverride = ST14HubStyle.Flat(accent), MinHeight = 4 });

            var head = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, SeparationOverride = 10, Margin = new Thickness(10, 4, 10, 0) };
            head.AddChild(new ST14TierMedal(cache) { Tier = tier, SetSize = new Vector2(54, 54) });
            var names = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, HorizontalExpand = true };
            names.AddChild(ST14HubStyle.Label(cache, $"SPONSORSHIP {ST14SupporterStatus.Numerals[tier]}", ST14HubFont.SemiBold, 19, accent));
            names.AddChild(ST14HubStyle.Label(cache, Loc.GetString("st14-support-tier-price", ("price", price)), ST14HubFont.Medium, 17, Color.White));
            head.AddChild(names);
            _badge = ST14HubStyle.Label(cache, string.Empty, ST14HubFont.Medium, 13, ST14HubStyle.Gold);
            _badge.VerticalAlignment = Control.VAlignment.Top;
            head.AddChild(_badge);
            column.AddChild(head);

            if (tier > 1)
            {
                var chip = new PanelContainer
                {
                    PanelOverride = ST14HubStyle.Flat(accent.WithAlpha(0.12f), accent.WithAlpha(0.5f), 1),
                    HorizontalAlignment = Control.HAlignment.Left,
                    Margin = new Thickness(10, 0),
                };
                var chipText = ST14HubStyle.Label(cache, Loc.GetString("st14-support-tier-includes", ("tier", ST14SupporterStatus.Numerals[tier - 1])),
                    ST14HubFont.Regular, 13, accent);
                chipText.Margin = new Thickness(8, 1);
                chip.AddChild(chipText);
                column.AddChild(chip);
            }

            _perks = new RichTextLabel { MaxWidth = 540, Margin = new Thickness(10, 0, 10, 8) };
            column.AddChild(_perks);
        }

        public void Update(int current)
        {
            var accent = ST14SupporterStatus.TierColors[Tier];
            var open = Tier <= current;
            var here = Tier == current;

            var box = ST14HubStyle.Flat(Color.FromHex("#100F0C").WithAlpha(open ? 0.92f : 0.75f),
                here ? ST14HubStyle.GoldLight : accent.WithAlpha(open ? 0.7f : 0.3f), here ? 3 : 1);
            Root.StyleBoxOverride = box;
            Root.ModulateSelfOverride = Color.White;

            _badge.Text = Loc.GetString(here ? "st14-support-tier-current" : Tier == 4 ? "st14-support-tier-max" : open
                ? "st14-support-tier-included"
                : "st14-support-tier-locked").ToUpperInvariant();
            _badge.FontColorOverride = here ? ST14HubStyle.GoldLight : open ? Color.FromHex("#9CE07C") : Color.FromHex("#8A8478");

            var (_, count) = Tiers[Tier - 1];
            var mark = open ? "[color=#9CE07C]✓[/color]" : "[color=#6E6A60]•[/color]";
            var textColor = open ? "#E6DCC0" : "#A8A090";
            var lines = Enumerable.Range(1, count)
                .Select(p => $"{mark} [color={textColor}]{Loc.GetString($"st14-support-tier-{Tier}-perk-{p}")}[/color]");
            _perks.SetMessage(FormattedMessage.FromMarkupOrThrow(string.Join("\n", lines)));
        }
    }

    private static BoxContainer Page()
    {
        return new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, VerticalExpand = true, SeparationOverride = 10 };
    }

    private Label SectionTitle(string loc)
    {
        var label = ST14HubStyle.Label(ResCache, Loc.GetString(loc).ToUpperInvariant(), ST14HubFont.SemiBold, 17, ST14HubStyle.GoldLight);
        label.Margin = new Thickness(6, 6, 6, 0);
        return label;
    }

    /// <summary>
    /// Line above a perk preview: the perk name and whether the current tier has it
    /// </summary>
    private Label Lock(int tier, string perk)
    {
        var label = ST14HubStyle.Label(ResCache, string.Empty, ST14HubFont.Medium, 16, ST14HubStyle.Gold);
        _locks.Add((label, tier, perk));
        return label;
    }

    /// <summary>
    /// Framed card of a tab: dark glass with a thin gold edge, content in <see cref="CardParts.Body"/>
    /// </summary>
    private static CardParts Card()
    {
        var box = ST14LobbyStyle.Glass(0.65f);
        box.SetContentMarginOverride(StyleBox.Margin.All, 12);
        var root = new PanelContainer { PanelOverride = box, HorizontalExpand = true, VerticalExpand = true };
        var body = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, SeparationOverride = 10 };
        root.AddChild(body);
        return new CardParts(root, body);
    }

    private readonly record struct CardParts(PanelContainer Root, BoxContainer Body);

    private Label ChatLine(string text, Color color)
    {
        var line = ST14HubStyle.Label(ResCache, text, ST14HubFont.Regular, 16, color);
        line.ClipText = true;
        return line;
    }

    /// <summary>
    /// Two rows of colour chips, the picked one gets a thick light gold ring
    /// </summary>
    private static GridContainer SwatchGrid(List<(Button Swatch, Color Color)> swatches, Action<Color> onPick)
    {
        var grid = new GridContainer { Columns = 4, HSeparationOverride = 8, VSeparationOverride = 8, HorizontalAlignment = HAlignment.Center };
        foreach (var color in Swatches)
        {
            var swatch = new Button { MinSize = new Vector2(64, 40), ModulateSelfOverride = Color.White };
            swatch.OnPressed += _ =>
            {
                MarkSwatch(swatches, color);
                onPick(color);
            };
            grid.AddChild(swatch);
            swatches.Add((swatch, color));
        }

        MarkSwatch(swatches, Color.Transparent);
        return grid;
    }

    private static void MarkSwatch(List<(Button Swatch, Color Color)> swatches, Color picked)
    {
        foreach (var (swatch, color) in swatches)
        {
            var selected = color.ToHex() == picked.WithAlpha(1f).ToHex();
            var box = ST14HubStyle.Flat(color, selected ? ST14HubStyle.GoldLight : Color.Black.WithAlpha(0.6f), selected ? 3 : 1);
            swatch.StyleBoxOverride = box;
            swatch.Text = selected ? "✓" : string.Empty;
            swatch.Label.FontColorOverride = Color.Black;
        }
    }

    private static RichTextLabel Text(string loc, float maxWidth)
    {
        // rich text measures itself on one line inside the canvas, the width cap makes it wrap inside the frame
        var text = new RichTextLabel { Margin = new Thickness(6, 4, 6, 4), MaxWidth = maxWidth };
        if (loc.Length > 0)
            text.SetMessage(FormattedMessage.FromMarkupOrThrow(Loc.GetString(loc)));
        return text;
    }

    private void AddPage(BoxContainer tabs, ST14LobbyFrame frame, string loc, Control page)
    {
        var index = _pages.Count;
        var tab = new Button { Text = Loc.GetString(loc), HorizontalExpand = true, MinHeight = 38 };
        tab.OnPressed += _ => SelectPage(index);
        tabs.AddChild(tab);

        page.Margin = new Thickness(4, 10, 4, 4);
        frame.Body.AddChild(page);
        _pages.Add((tab, page));
    }

    #endregion

    private void SelectPage(int index)
    {
        for (var i = 0; i < _pages.Count; i++)
        {
            var (tab, page) = _pages[i];
            var selected = i == index;
            page.Visible = selected;
            tab.ModulateSelfOverride = Color.White;
            tab.StyleBoxOverride = ST14LobbyStyle.Tab(ResCache, selected);
            tab.Label.FontColorOverride = selected ? Color.Black : ST14HubStyle.Gold;
        }
    }

    private void SelectWeapon(int index)
    {
        if (_weapons.Count == 0)
            return;

        _weapon = Math.Clamp(index, 0, _weapons.Count - 1);
        _weaponView.SetPrototype(_weapons[_weapon].ID);
        UpdateWeapon();
    }

    /// <summary>
    /// Keeps a perk choice in the client cvars and sends all choices to the server, which uses them if the tier allows
    /// </summary>
    private void SaveChoice(CVarDef<string> cvar, string value)
    {
        _cfg.SetCVar(cvar, value);
        _cfg.SaveToFile();
        _entMan.System<ST14SupporterClientSystem>().SendChoices();
    }

    private void SaveNickname()
    {
        if (_weapons.Count > 0)
            _cfg.SetCVar(ST14CVars.PerkWeapon, _weapons[_weapon].ID);
        SaveChoice(ST14CVars.PerkNickname, _nickname.Trim());
    }

    private void UpdateWeapon()
    {
        var nick = string.IsNullOrWhiteSpace(_nickname) ? Loc.GetString("st14-support-nickname-default") : _nickname.Trim();
        if (nick.Length > 24)
            nick = nick[..24];

        var weapon = _weapons.Count > 0 ? _weapons[_weapon].Name : "?";
        _weaponPreview.Text = Loc.GetString("st14-support-nickname-preview", ("weapon", weapon), ("nickname", nick));
    }

    private void LoadFigure()
    {
        if (_prefs.ServerDataLoaded && _prefs.Preferences?.SelectedCharacter is HumanoidCharacterProfile profile)
        {
            _figure.LoadPreview(profile);
            _stand.Plate = string.IsNullOrWhiteSpace(profile.Name) ? _player.LocalSession?.Name ?? string.Empty : profile.Name;
        }
        else
        {
            _stand.Plate = _player.LocalSession?.Name ?? string.Empty;
        }
    }

    private void UpdateStatus()
    {
        var tier = ST14SupporterStatus.Tier;
        _medal.Tier = tier;
        _status.Text = (tier > 0 ? $"SPONSORSHIP {ST14SupporterStatus.Numerals[tier]}" : Loc.GetString("st14-support-status-none")).ToUpperInvariant();
        _status.FontColorOverride = tier > 0 ? ST14SupporterStatus.TierColors[tier] : Color.FromHex("#B8B0A0");
        _statusText.SetMessage(FormattedMessage.FromMarkupOrThrow(Loc.GetString(tier > 0
            ? "st14-support-status-active-text"
            : "st14-support-status-none-text")));

        for (var i = 0; i < _steps.Count; i++)
        {
            var stepTier = i + 1;
            var reached = stepTier <= tier;
            var accent = ST14SupporterStatus.TierColors[stepTier];
            _steps[i].PanelOverride = ST14HubStyle.Flat(reached ? accent.WithAlpha(0.85f) : Color.FromHex("#1A1814"), accent.WithAlpha(reached ? 1f : 0.45f), 1);
            _steps[i].Children.OfType<Label>().First().FontColorOverride = reached ? Color.Black : accent.WithAlpha(0.8f);
        }

        foreach (var card in _tierCards)
        {
            card.Update(tier);
        }

        foreach (var (label, lockTier, perk) in _locks)
        {
            var open = tier >= lockTier;
            label.Text = Loc.GetString(open ? "st14-support-perk-open" : "st14-support-perk-locked",
                ("perk", Loc.GetString(perk)), ("tier", ST14SupporterStatus.Numerals[lockTier])).ToUpperInvariant();
            label.FontColorOverride = open ? Color.FromHex("#9CE07C") : ST14SupporterStatus.TierColors[lockTier];
        }
    }
}

/// <summary>
/// Round medal of a supporter tier: a ring in the tier colour, a slowly pulsing glow and the tier numeral,
/// grey with a dash when there is no subscription
/// </summary>
public sealed class ST14TierMedal : Control
{
    private readonly IResourceCache _cache;
    private readonly Texture _glow;
    private float _time;

    public int Tier;

    public ST14TierMedal(IResourceCache cache)
    {
        _cache = cache;
        _glow = cache.GetResource<TextureResource>("/Textures/_ST14/Interface/Lobby/soft_glow.png").Texture;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        _time += args.DeltaSeconds;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var size = PixelSize;
        var center = size / 2;
        var radius = MathF.Min(size.X, size.Y) * 0.42f;
        var color = ST14SupporterStatus.TierColors[Math.Clamp(Tier, 0, 4)];
        var pulse = 0.8f + 0.2f * MathF.Sin(_time * MathF.Tau / 3f);

        if (Tier > 0)
        {
            var glow = radius * 2.4f;
            handle.DrawTextureRect(_glow, UIBox2.FromDimensions(center - new Vector2(glow, glow) / 2, new Vector2(glow, glow)),
                color.WithAlpha(0.35f * pulse));
        }

        handle.DrawCircle(center, radius, Color.FromHex("#14120E"));
        handle.DrawCircle(center, radius, color, false);
        handle.DrawCircle(center, radius * 0.86f, color.WithAlpha(0.6f), false);
        handle.DrawCircle(center, radius * 0.84f, color.WithAlpha(Tier > 0 ? 0.18f : 0.06f));

        var font = ST14HubStyle.Font(_cache, ST14HubFont.Black, Math.Max(8, (int) (radius * 0.75f)));
        var text = ST14SupporterStatus.Numerals[Math.Clamp(Tier, 0, 4)];
        var dims = handle.GetDimensions(font, text, 1f);
        handle.DrawString(font, center - dims / 2, text, 1f, Tier > 0 ? color : color.WithAlpha(0.7f));
    }
}

/// <summary>
/// Slanted rubber stamp with a caption, like the classified stamps of the galaxy map
/// </summary>
public sealed class ST14StampControl : Control
{
    private readonly IResourceCache _cache;
    private readonly string _text;

    public ST14StampControl(IResourceCache cache, string text)
    {
        _cache = cache;
        _text = text;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var size = PixelSize;
        var stamp = new Vector2(size.X * 0.9f, size.Y * 0.62f);
        ST14ClassifiedStamp.DrawStamp(handle, _cache, _text, size / 2, stamp, -6f, stamp.Y / 56f);
    }
}

/// <summary>
/// Wooden plinth for the character figurine: a soft spotlight from above, a shadow under the figure, a stepped wooden
/// base with bevels and grain and a brass plate carrying the character name
/// </summary>
public sealed class ST14WoodenStand : Control
{
    private readonly IResourceCache _cache;
    private readonly Texture _glow;

    public string Plate = string.Empty;

    public ST14WoodenStand(IResourceCache cache)
    {
        _cache = cache;
        _glow = cache.GetResource<TextureResource>("/Textures/_ST14/Interface/Lobby/soft_glow.png").Texture;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var size = PixelSize;
        var s = UIScale;
        var top = size.Y - 100 * s;
        var cx = size.X / 2;

        // spotlight falling on the figure and its shadow on the top board
        handle.DrawTextureRect(_glow, new UIBox2(cx - size.X * 0.45f, 0, cx + size.X * 0.45f, top + 10 * s),
            Color.FromHex("#F7D68C").WithAlpha(0.16f));
        handle.DrawTextureRect(_glow, new UIBox2(cx - 70 * s, top - 14 * s, cx + 70 * s, top + 10 * s), Color.Black.WithAlpha(0.55f));

        void Board(float left, float right, float y, float height, Color face)
        {
            var box = new UIBox2(cx - left * s, y, cx + right * s, y + height * s);
            handle.DrawRect(box, face);
            // bevel: light top edge, dark bottom edge
            handle.DrawRect(new UIBox2(box.Left, box.Top, box.Right, box.Top + 2 * s), Color.White.WithAlpha(0.18f));
            handle.DrawRect(new UIBox2(box.Left, box.Bottom - 3 * s, box.Right, box.Bottom), Color.Black.WithAlpha(0.35f));
            // grain
            for (var i = 1; i < height / 7; i++)
            {
                var gy = y + i * 7 * s;
                handle.DrawLine(new Vector2(box.Left + 6 * s, gy), new Vector2(box.Right - 6 * s, gy + (i % 2 == 0 ? 1 : -1) * s),
                    Color.FromHex("#3A2210").WithAlpha(0.35f));
            }
        }

        Board(118, 118, top, 16, Color.FromHex("#8C5A32"));
        Board(100, 100, top + 16 * s, 56, Color.FromHex("#6E4526"));
        Board(132, 132, top + 72 * s, 18, Color.FromHex("#7C4E2B"));

        // brass name plate with screws
        var plate = new UIBox2(cx - 82 * s, top + 30 * s, cx + 82 * s, top + 58 * s);
        handle.DrawRect(plate, Color.FromHex("#C9A253"));
        handle.DrawRect(new UIBox2(plate.Left, plate.Top, plate.Right, plate.Top + 2 * s), Color.FromHex("#F2D58A"));
        handle.DrawRect(plate, Color.FromHex("#6B5226"), false);
        handle.DrawCircle(new Vector2(plate.Left + 6 * s, plate.Center.Y), 2 * s, Color.FromHex("#6B5226"));
        handle.DrawCircle(new Vector2(plate.Right - 6 * s, plate.Center.Y), 2 * s, Color.FromHex("#6B5226"));

        if (string.IsNullOrEmpty(Plate))
            return;

        var font = ST14HubStyle.Font(_cache, ST14HubFont.Medium, Math.Max(8, (int) (13 * s)));
        var text = Plate.Length > 22 ? Plate[..22] : Plate;
        var dims = handle.GetDimensions(font, text, 1f);
        handle.DrawString(font, plate.Center - dims / 2, text, 1f, Color.FromHex("#2A1A08"));
    }
}
