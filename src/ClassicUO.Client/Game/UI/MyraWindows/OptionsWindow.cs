#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using ClassicUO.Common.Enums;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Input;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Myra.Events;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;
using Label = Myra.Graphics2D.UI.Label;

namespace ClassicUO.Game.UI.MyraWindows;

public class OptionsWindow : MyraControl
{
    /// <summary>
    /// Category, (sub category?, widget)
    /// </summary>
    private readonly Dictionary<string, List<OptionItem>> _options = new();

    private readonly MyraGrid _mainArea = new();
    private readonly VerticalStackPanel _optionsPanel = new() { Spacing =  MyraStyle.STANDARD_SPACING, Padding = new Thickness(3, 0, 0, 0) };
    private readonly VerticalStackPanel _searchPanel = new();
    private readonly VerticalStackPanel _optionsStack = new();
    private readonly MyraInputBox _searchField = new();
    private string _lastCategory = string.Empty;
    private List<Panel> _visualContainers = new();

    public OptionsWindow() : base("Options")
    {
        UIManager.ForEach<OptionsWindow>(w =>
        {
            if (w != this) w.Dispose();
        });

        SetupOptions();
        Build();

        CenterInViewPort();

        _rootWindow.Props.MaxHeight = 800;
        _rootWindow.Props.Resize.MaxHeight = 800;
    }

    private void SetupOptions()
    {
        SetupGeneralOptions();
        SetupMobileOptions();
        SetupInterfaceOptions();
        SetupMiscOptions();
        SetupTerrainStatics();
        SetupSound();
        SetupVideo();
        SetupInfoBarOptions();
        SetupTooltipOptions();
        SetupSpeechOptions();
        SetupCombatOptions();
    }

    private void Build()
    {
        _mainArea.MinWidth = 400;
        _mainArea.MinHeight = 400;

        _mainArea.AddColumn(Proportion.Auto);
        _mainArea.AddColumn(Proportion.Fill);
        _mainArea.AddRow(Proportion.Auto);
        _mainArea.AddRow(Proportion.Fill);

        _searchField.HintText = "Search...";
        _searchField.TextChangedByUser += SearchFieldOnTextChangedByUser;
        _mainArea.AddWidget(_searchField, 0, 0, null, 2);

        VerticalStackPanel categoryPanel = new() { Spacing = MyraStyle.STANDARD_SPACING};
        _mainArea.AddWidget(categoryPanel.WrapInScroll(800), 1, 0);

        _optionsStack.Widgets.Add(_optionsPanel);
        _mainArea.AddWidget(_optionsStack, 1, 1);

        foreach (string category in _options.Keys) categoryPanel.Widgets.Add(GetCategoryButton(category));

        MyraButton GetCategoryButton(string category)
        {
            return ApplyTabStyleToButton(new MyraButton(category, () => { ShowPage(category); }));
        }

        SetRootContent(_mainArea);
    }

    private void SearchFieldOnTextChangedByUser(object? sender, ValueChangedEventArgs<string> e)
    {
        foreach (Panel visualContainer in _visualContainers) visualContainer.RemoveFromParent();
        _visualContainers.Clear();

        string search = e.NewValue?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(search))
        {
            _optionsStack.Widgets.Clear();
            _optionsStack.Widgets.Add(_optionsPanel);

            if (_lastCategory.NotNullNotEmpty())
                ShowPage(_lastCategory);
        }
        else
        {
            _searchPanel.Widgets.Clear();
            foreach (var (_, items) in _options)
            {
                foreach (OptionItem item in items)
                {
                    if (item.MatchesSearch(search))
                        _searchPanel.Widgets.Add(item.GetWidget);
                }
            }

            _optionsStack.Widgets.Clear();
            _optionsStack.Widgets.Add(_searchPanel);
        }
    }

    private static ButtonStyle _tabButtonStyle = null!;

    private MyraButton ApplyTabStyleToButton(MyraButton tabButton)
    {
        if (_tabButtonStyle == null!)
        {
            ButtonStyle tabControlStyle = Stylesheet.Current.ButtonStyle;
            _tabButtonStyle = new ButtonStyle(tabControlStyle)
            {
                Background = new SolidBrush(Color.Transparent),
                Border = new SolidBrush(new Color(0, 0, 0, MyraStyle.STANDARD_BORDER_ALPHA)),
                BorderThickness = new Thickness(1),
                LabelStyle = { Font = MyraStyle.UiFont },
                OverBackground = new SolidBrush(new Color(0, 0, 0, 55)),
                PressedBackground = new SolidBrush(new Color(0, 0, 0, 155)),
                MinWidth = 150
            };
        }

        tabButton.ApplyButtonStyle(_tabButtonStyle);

        return tabButton;
    }

    private void ShowPage(string category)
    {
        foreach (Panel visualContainer in _visualContainers) visualContainer.RemoveFromParent();
        _visualContainers.Clear();

        _searchField.Text = string.Empty;
        _optionsStack.Widgets.Clear();
        _optionsStack.Widgets.Add(_optionsPanel);

        _optionsPanel.Widgets.Clear();

        List<VisualContainerData> visualContainers = new();
        VisualContainerData current = new();

        int cY = 0;

        foreach (OptionItem optionItem in _options[category])
        {
            _optionsPanel.Widgets.Add(optionItem.GetWidget);
            //Not beautiful, but can't seem to get accurate height, x or y until it renders without doing this
            Point size = optionItem.GetWidget.Measure(new Point(1000, 1000));

            if (optionItem.IsStartVisualContainer) current.StartY = cY;

            cY += size.Y + MyraStyle.STANDARD_SPACING;

            if (optionItem.IsEndVisualContainer)
            {
                current.EndY = cY;
                visualContainers.Add(current);
                current = new();
            }
        }

        Point optSize = _optionsPanel.Measure(new Point(1000, 1000));

        foreach (VisualContainerData cont in visualContainers)
        {
            var d = new Panel();
            // d.Background = MyraStyle.NinePatchButtonDown;
            d.Background = new SolidBrush(new Color(0, 0, 0, 25));
            d.Border = new SolidBrush(new Color(0, 0, 0, 75));
            d.BorderThickness = new Thickness(2);
            Rectangle rect = cont.GetRect(optSize.X);
            d.Width = rect.Width;
            d.Height = rect.Height + MyraStyle.STANDARD_SPACING + 5;
            d.Left = rect.Left;
            d.Top = rect.Top - 5;

            _mainArea.Widgets.Insert(0, d);
            Grid.SetRow(d, 1);
            Grid.SetColumn(d, 1);
            _visualContainers.Add(d);
        }

        _lastCategory = category;
    }

    private static OptionItem CreateCheckboxOption(string label, bool enabled, Action<bool> onChange,
        string? tooltip = null) =>
        new(label, () => MyraCheckButton.CreateWithCallback(enabled, onChange, label, tooltip));

    private static OptionItem CreateSliderOption(string label, float min, float max, float value,
        Action<float> onChange) =>
        new(label, () => MyraHSlider.SliderWithLabel(label, out _, onChange, min, max, value));

    private static OptionItem CreateComboBox(string label, int value, string[] options, Action<int> onChange,
        string? tooltip = null)
    {
        var comboView = new ComboView
        {
            MinWidth = 200,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        if (tooltip != null) comboView.Tooltip = tooltip;

        for (int i = 0; i < options.Length; i++)
        {
            string option = options[i];
            comboView.ListView.Widgets.Add(new Label { Text = option, Tag = i });
        }

        comboView.ListView.SelectedIndex = value;

        comboView.ListView.SelectedIndexChanged += (_, _) =>
        {
            if (comboView.ListView.SelectedIndex != null)
                onChange(comboView.ListView.SelectedIndex.Value);
        };

        return new OptionItem(label, () => new MyraLabel(label, MyraLabel.TextStyle.P).PlaceBefore(comboView));
    }

    private static OptionItem CreateHuePicker(string label, ushort hue, Action<ushort> onChange) =>
        new(label, () =>
        {
            var item = new MyraArtTexture(0x0FAB) { Tooltip = $"Current hue: {hue}" };
            item.TouchUp += (_, _) =>
            {
                UIManager.GetGump<ModernColorPicker>()?.Dispose();
                UIManager.Add(new ModernColorPicker(World.Instance, onChange));
            };

            return item.PlaceBefore(new MyraLabel(label, MyraLabel.TextStyle.P));
        });

    private static OptionItem CreateInputField(string label, string text, Action<string> onChange,
        string? tooltip = null) => new(label, () =>
    {
        HorizontalStackPanel wid = MyraInputBox.WithLabel(label, out MyraInputBox inputBox, text: text, tooltip: tooltip);
        inputBox.TextChangedByUser += (_, _) => onChange(inputBox.Text);
        return wid;
    });

    private static OptionItem CreateSpacer() => new(string.Empty, () => new MyraSpacer(1, 4), skipSearch: true);

    private void SetupGeneralOptions()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;

        if (!_options.ContainsKey("General")) _options.Add("General", new List<OptionItem>());

        List<OptionItem> general = _options["General"];
        ModernOptionsGumpLanguage.General genLang = lang.GetGeneral;

        general.Add(CreateCheckboxOption(genLang.HighlightObjects, profile.HighlightGameObjects,
            b => profile.HighlightGameObjects = b));

        general.Add(CreateSpacer());

        general.Add(
            CreateCheckboxOption(genLang.Pathfinding, profile.EnablePathfind, b => profile.EnablePathfind = b)
                .SetTags("pathfinding, pathing, path"));
        general.Add(
            CreateCheckboxOption(genLang.ShiftPathfinding, profile.UseShiftToPathfind,
                b => profile.UseShiftToPathfind = b).SetTags("pathfinding, pathing, path"));
        general.Add(CreateCheckboxOption(genLang.SingleClickPathfind, profile.PathfindSingleClick,
            b => profile.PathfindSingleClick = b).SetTags("pathfinding, pathing, path"));

        general.Add(CreateSpacer());

        general.Add(CreateCheckboxOption(genLang.AlwaysRun, profile.AlwaysRun, b => profile.AlwaysRun = b));
        general.Add(CreateCheckboxOption(genLang.RunUnlessHidden, profile.AlwaysRunUnlessHidden,
            b => profile.AlwaysRunUnlessHidden = b));

        general.Add(CreateSpacer());

        general.Add(CreateCheckboxOption(genLang.AutoOpenDoors, profile.AutoOpenDoors,
            b => profile.AutoOpenDoors = b));
        general.Add(CreateCheckboxOption(genLang.AutoOpenPathfinding, profile.SmoothDoors,
            b => profile.SmoothDoors = b));

        general.Add(CreateSpacer());

        general.Add(CreateCheckboxOption(genLang.AutoOpenCorpse, profile.AutoOpenCorpses,
            b => profile.AutoOpenCorpses = b).SetTags("corpse, loot"));
        general.Add(CreateSliderOption(genLang.CorpseOpenDistance, 0, 5, profile.AutoOpenCorpseRange,
            f => profile.AutoOpenCorpseRange = (int)f).SetTags("corpse, loot"));
        general.Add(CreateCheckboxOption(genLang.CorpseSkipEmpty, profile.SkipEmptyCorpse,
                b => profile.SkipEmptyCorpse = b,
                "Most servers don't send corpse contents until it's opened.\nEnabling this will make this feature not work on most servers.")
            .SetTags("corpse, loot"));
        general.Add(CreateComboBox(genLang.CorpseOpenOptions, profile.CorpseOpenOptions, [
            genLang.CorpseOptNone, genLang.CorpseOptNotTarg,
            genLang.CorpseOptNotHiding, genLang.CorpseOptBoth
        ], i => profile.CorpseOpenOptions = i).SetTags("corpse, loot"));

        general.Add(CreateSpacer());

        general.Add(CreateCheckboxOption(genLang.OutRangeColor, profile.NoColorObjectsOutOfRange,
            b => profile.NoColorObjectsOutOfRange = b));
        general.Add(CreateCheckboxOption(genLang.SallosEasyGrab, profile.SallosEasyGrab,
            b => profile.SallosEasyGrab = b, genLang.SallosTooltip));
        general.Add(CreateCheckboxOption(genLang.ShowHouseContent, profile.ShowHouseContent,
            b => profile.ShowHouseContent = b, genLang.ClientVersionLimitedTooltip));
        general.Add(CreateCheckboxOption(genLang.SmoothBoat, profile.UseSmoothBoatMovement,
            b => profile.UseSmoothBoatMovement = b, genLang.ClientVersionLimitedTooltip));
        //general.Add(CreateCheckboxOption(, , b =>  = b));
    }

    private void SetupMobileOptions()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;

        if (!_options.ContainsKey("Mobiles")) _options.Add("Mobiles", new List<OptionItem>());
        List<OptionItem> mobiles = _options["Mobiles"];
        mobiles.Add(
            CreateCheckboxOption(lang.GetGeneral.ShowMobileHP, profile.ShowMobilesHP, b => profile.ShowMobilesHP = b)
                .SetTags("hp, health, hit point"));
        mobiles.Add(CreateComboBox(lang.GetGeneral.MobileHPType, profile.MobileHPType,
            [lang.GetGeneral.HPTypePerc, lang.GetGeneral.HPTypeBar, lang.GetGeneral.HPTypeNBoth],
            i => profile.MobileHPType = i).SetTags("hp, health, hit point"));
        mobiles.Add(CreateComboBox(lang.GetGeneral.HPShowWhen, profile.MobileHPShowWhen,
            [lang.GetGeneral.HPShowWhen_Always, lang.GetGeneral.HPShowWhen_Less100, lang.GetGeneral.HPShowWhen_Smart],
            i => profile.MobileHPShowWhen = i).SetTags("hp, health, hit point"));

        mobiles.Add(CreateSpacer());

        mobiles.Add(CreateCheckboxOption(lang.GetGeneral.HighlightPoisoned, profile.HighlightMobilesByPoisoned,
            b => profile.HighlightMobilesByPoisoned = b));
        mobiles.Add(
            CreateHuePicker(lang.GetGeneral.PoisonHighlightColor, profile.PoisonHue, h => profile.PoisonHue = h));

        mobiles.Add(CreateCheckboxOption(lang.GetGeneral.HighlightPara, profile.HighlightMobilesByParalize,
            b => profile.HighlightMobilesByParalize = b));
        mobiles.Add(CreateHuePicker(lang.GetGeneral.ParaHighlightColor, profile.ParalyzedHue,
            h => profile.ParalyzedHue = h));

        mobiles.Add(CreateCheckboxOption(lang.GetGeneral.HighlightInvul, profile.HighlightMobilesByInvul,
            b => profile.HighlightMobilesByInvul = b));
        mobiles.Add(CreateHuePicker(lang.GetGeneral.InvulHighlightColor, profile.InvulnerableHue,
            h => profile.InvulnerableHue = h));

        mobiles.Add(CreateCheckboxOption(lang.GetGeneral.IncomingMobiles, profile.ShowNewMobileNameIncoming,
            b => profile.ShowNewMobileNameIncoming = b));
        mobiles.Add(CreateCheckboxOption(lang.GetGeneral.IncomingCorpses, profile.ShowNewCorpseNameIncoming,
            b => profile.ShowNewCorpseNameIncoming = b));

        mobiles.Add(CreateComboBox(lang.GetGeneral.AuraUnderFeet, profile.AuraUnderFeetType, [
                lang.GetGeneral.AuraOptDisabled, lang.GetGeneral.AuroOptWarmode,
                lang.GetGeneral.AuraOptCtrlShift, lang.GetGeneral.AuraOptAlways
            ],
            i => profile.AuraUnderFeetType = i));

        mobiles.Add(CreateCheckboxOption(lang.GetGeneral.AuraForParty, profile.PartyAura, b => profile.PartyAura = b));
        mobiles.Add(
            CreateHuePicker(lang.GetGeneral.AuraPartyColor, profile.PartyAuraHue, h => profile.PartyAuraHue = h));
    }

    private void SetupInterfaceOptions()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;

        if (!_options.ContainsKey("Interface")) _options.Add("Interface", new List<OptionItem>());
        List<OptionItem> opt = _options["Interface"];

        opt.Add(CreateCheckboxOption(lang.GetGeneral.DisableTopMenu, profile.TopbarGumpIsDisabled,
            b => profile.TopbarGumpIsDisabled = b,
            "The top menu is pretty vital in TazUO, we recommend leaving this unchecked."));

        opt.Add(CreateSpacer());

        opt.Add(CreateCheckboxOption(lang.GetGeneral.AltForAnchorsGumps, profile.HoldDownKeyAltToCloseAnchored,
            b => profile.HoldDownKeyAltToCloseAnchored = b));
        opt.Add(CreateCheckboxOption(lang.GetGeneral.AltToMoveGumps, profile.HoldAltToMoveGumps,
            b => profile.HoldAltToMoveGumps = b));
        opt.Add(CreateCheckboxOption(lang.GetGeneral.CloseEntireAnchorWithRClick,
            profile.CloseAllAnchoredGumpsInGroupWithRightClick,
            b => profile.CloseAllAnchoredGumpsInGroupWithRightClick = b));

        opt.Add(CreateSpacer());

        opt.Add(CreateCheckboxOption(lang.GetGeneral.OriginalSkillsGump, profile.StandardSkillsGump,
            b => profile.StandardSkillsGump = b));
        opt.Add(CreateCheckboxOption(lang.GetGeneral.OldStatusGump, profile.UseOldStatusGump,
            b => profile.UseOldStatusGump = b));
        opt.Add(CreateCheckboxOption(lang.GetGeneral.PartyInviteGump, profile.PartyInviteGump,
            b => profile.PartyInviteGump = b));

        opt.Add(CreateSpacer());

        opt.Add(CreateCheckboxOption(lang.GetGeneral.ModernHealthBars, profile.CustomBarsToggled,
            b => profile.CustomBarsToggled = b));
        opt.Add(CreateCheckboxOption(lang.GetGeneral.ModernHPBlackBG, profile.CBBlackBGToggled,
            b => profile.CBBlackBGToggled = b));
        opt.Add(CreateCheckboxOption(lang.GetGeneral.SaveHPBars, profile.SaveHealthbars,
            b => profile.SaveHealthbars = b));
        opt.Add(CreateComboBox(lang.GetGeneral.CloseHPGumpsWhen, profile.CloseHealthBarType, [
            lang.GetGeneral.CloseHPOptDisable, lang.GetGeneral.CloseHPOptOOR,
            lang.GetGeneral.CloseHPOptDead, lang.GetGeneral.CloseHPOptBoth
        ], b => profile.CloseHealthBarType = b));

        opt.Add(CreateSpacer());

        opt.Add(CreateComboBox(lang.GetGeneral.GridLoot, profile.GridLootType, [
            lang.GetGeneral.GridLootOptDisable, lang.GetGeneral.GridLootOptOnly,
            lang.GetGeneral.GridLootOptBoth
        ], i => profile.GridLootType = i, "This is not the same as grid containers."));

        opt.Add(CreateSpacer());

        opt.Add(CreateCheckboxOption(lang.GetGeneral.ShiftContext, profile.HoldShiftForContext,
            b => profile.HoldShiftForContext = b));
        opt.Add(CreateCheckboxOption(lang.GetGeneral.ShiftSplit, profile.HoldShiftToSplitStack,
            b => profile.HoldShiftToSplitStack = b));
    }

    private void SetupMiscOptions()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;

        if (!_options.ContainsKey("Misc")) _options.Add("Misc", new List<OptionItem>());
        List<OptionItem> opt = _options["Misc"];

        opt.Add(CreateCheckboxOption(lang.GetGeneral.EnableCOT, profile.UseCircleOfTransparency,
            b => profile.UseCircleOfTransparency = b).SetTags("cot, circle of transparency"));
        opt.Add(CreateSliderOption(lang.GetGeneral.COTDistance, Constants.MIN_CIRCLE_OF_TRANSPARENCY_RADIUS,
            Constants.MAX_CIRCLE_OF_TRANSPARENCY_RADIUS, profile.CircleOfTransparencyRadius,
            f => profile.CircleOfTransparencyRadius = (int)f).SetTags("cot, circle of transparency"));
        opt.Add(CreateComboBox(lang.GetGeneral.COTType, profile.CircleOfTransparencyType, [
            lang.GetGeneral.COTTypeOptFull, lang.GetGeneral.COTTypeOptGrad,
            lang.GetGeneral.COTTypeOptModern
        ], i => profile.CircleOfTransparencyType = i).SetTags("cot, circle of transparency"));

        opt.Add(CreateSpacer());

        opt.Add(CreateCheckboxOption(lang.GetGeneral.HideScreenshotMessage, profile.HideScreenshotStoredInMessage,
            b => profile.HideScreenshotStoredInMessage = b));
        opt.Add(CreateCheckboxOption(lang.GetGeneral.ObjFade, profile.UseObjectsFading,
            b => profile.UseObjectsFading = b));
        opt.Add(CreateCheckboxOption(lang.GetGeneral.TextFade, profile.TextFading, b => profile.TextFading = b));
        opt.Add(CreateCheckboxOption(lang.GetGeneral.CursorRange, profile.ShowTargetRangeIndicator,
            b => profile.ShowTargetRangeIndicator = b));

        opt.Add(CreateSpacer());

        opt.Add(CreateCheckboxOption(lang.GetGeneral.DragSelectHP, profile.EnableDragSelect,
            b => profile.EnableDragSelect = b));
        opt.Add(CreateComboBox(lang.GetGeneral.DragKeyMod, profile.DragSelectModifierKey, [
            lang.GetGeneral.SharedNone, lang.GetGeneral.SharedCtrl, lang.GetGeneral.SharedShift,
            lang.GetGeneral.SharedAlt
        ], i => profile.DragSelectModifierKey = i));
        opt.Add(CreateComboBox(lang.GetGeneral.DragPlayersOnly, profile.DragSelect_PlayersModifier, [
            lang.GetGeneral.SharedNone, lang.GetGeneral.SharedCtrl, lang.GetGeneral.SharedShift,
            lang.GetGeneral.SharedAlt
        ], i => profile.DragSelect_PlayersModifier = i));
        opt.Add(CreateComboBox(lang.GetGeneral.DragMobsOnly, profile.DragSelect_MonstersModifier, [
            lang.GetGeneral.SharedNone, lang.GetGeneral.SharedCtrl, lang.GetGeneral.SharedShift,
            lang.GetGeneral.SharedAlt
        ], i => profile.DragSelect_MonstersModifier = i));
        opt.Add(CreateComboBox(lang.GetGeneral.DragNameplatesOnly, profile.DragSelect_NameplateModifier, [
            lang.GetGeneral.SharedNone, lang.GetGeneral.SharedCtrl, lang.GetGeneral.SharedShift,
            lang.GetGeneral.SharedAlt
        ], i => profile.DragSelect_NameplateModifier = i));
        opt.Add(CreateInputField(lang.GetGeneral.DragX, profile.DragSelectStartX.ToString(), s =>
        {
            if (int.TryParse(s, out int result)) profile.DragSelectStartX = result;
        }));
        opt.Add(CreateInputField(lang.GetGeneral.DragY, profile.DragSelectStartY.ToString(), s =>
        {
            if (int.TryParse(s, out int result)) profile.DragSelectStartY = result;
        }));

        opt.Add(CreateSpacer());

        opt.Add(CreateCheckboxOption(lang.GetGeneral.ShowStatsChangedMsg, profile.ShowStatsChangedMessage,
            b => profile.ShowStatsChangedMessage = b));
        opt.Add(CreateCheckboxOption(lang.GetGeneral.ShowSkillsChangedMsg, profile.ShowSkillsChangedMessage,
            b => profile.ShowSkillsChangedMessage = b));
        opt.Add(CreateSliderOption(lang.GetGeneral.ChangeVolume, 0, 100, profile.ShowSkillsChangedDeltaValue,
            f => profile.ShowSkillsChangedDeltaValue = (int)f));
    }

    private void SetupTerrainStatics()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;

        if (!_options.ContainsKey("Terrain & Statics")) _options.Add("Terrain & Statics", new List<OptionItem>());
        List<OptionItem> opt = _options["Terrain & Statics"];

        opt.Add(CreateCheckboxOption(lang.GetGeneral.HideRoof, !profile.DrawRoofs, b => profile.DrawRoofs = !b));
        opt.Add(CreateCheckboxOption(lang.GetGeneral.TreesToStump, profile.TreeToStumps, b => profile.TreeToStumps = b));
        opt.Add(CreateCheckboxOption(lang.GetGeneral.HideVegetation, profile.HideVegetation, b => profile.HideVegetation = b));
        opt.Add(CreateComboBox(lang.GetGeneral.MagicFieldType, profile.FieldsType, [
            lang.GetGeneral.MagicFieldOpt_Normal, lang.GetGeneral.MagicFieldOpt_Static,
            lang.GetGeneral.MagicFieldOpt_Tile
        ], i => profile.FieldsType = i));
    }

    private void SetupSound()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;

        if (!_options.ContainsKey("Sound")) _options.Add("Sound", new List<OptionItem>());
        List<OptionItem> opt = _options["Sound"];

        opt.Add(CreateCheckboxOption(lang.GetSound.EnableSound, profile.EnableSound, b => profile.EnableSound = b));
        opt.Add(CreateSliderOption(lang.GetSound.SharedVolume, 0, 100, profile.SoundVolume, f=>profile.SoundVolume=(int)f));
        opt.Add(CreateCheckboxOption(lang.GetSound.EnableMusic, profile.EnableMusic, b => profile.EnableMusic = b));
        opt.Add(CreateSliderOption(lang.GetSound.SharedVolume, 0, 100, profile.MusicVolume, f=>profile.MusicVolume=(int)f));
        opt.Add(CreateCheckboxOption(lang.GetSound.LoginMusic, Settings.GlobalSettings.LoginMusic, b => Settings.GlobalSettings.LoginMusic = b));
        opt.Add(CreateSliderOption(lang.GetSound.SharedVolume, 0, 100, Settings.GlobalSettings.LoginMusicVolume, f=>Settings.GlobalSettings.LoginMusicVolume=(int)f));
        opt.Add(CreateCheckboxOption(lang.GetSound.PlayFootsteps, profile.EnableFootstepsSound, b => profile.EnableFootstepsSound = b));
        opt.Add(CreateCheckboxOption(lang.GetSound.CombatMusic, profile.EnableCombatMusic, b => profile.EnableCombatMusic = b));
        opt.Add(CreateCheckboxOption(lang.GetSound.BackgroundMusic, profile.ReproduceSoundsInBackground, b => profile.ReproduceSoundsInBackground = b));

        opt.Add(CreateSpacer());

        opt.Add(new OptionItem("Voice to text", () => new MyraButton("Create voice toggle button", () =>
        {
            var macroManager = MacroManager.TryGetMacroManager(World.Instance);
            if (macroManager == null) return;
            var macro = Macro.CreateFastMacro("Toggle Voice", MacroType.ToggleVoiceRecognition,
                MacroSubType.MSC_NONE);
            macroManager.PushToBack(macro);
            UIManager.Add(new MacroButtonGump(World.Instance, macro, Mouse.Position.X, Mouse.Position.Y));
        })));
        ModernOptionsGumpLanguage.TazUO voiceLang = lang.GetTazUO;
        opt.Add(CreateInputField(voiceLang.VoiceModelPath, profile.VoiceModelPath, s => profile.VoiceModelPath = s, voiceLang.VoiceModelPathTooltip));
    }

    private void SetupVideo()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;

        if (!_options.ContainsKey("Video")) _options.Add("Video", new List<OptionItem>());
        List<OptionItem> opt = _options["Video"];

        opt.Add(CreateSliderOption(lang.GetVideo.FPSCap, Constants.MIN_FPS, Constants.MAX_FPS, Settings.GlobalSettings.FPS,
            f =>
            {
                Settings.GlobalSettings.FPS = (int)f;
                Client.Game.SetRefreshRate((int)f);
            }));
        opt.Add(CreateCheckboxOption(lang.GetVideo.BackgroundFPS, profile.ReduceFPSWhenInactive, b => profile.ReduceFPSWhenInactive = b));
        opt.Add(CreateCheckboxOption(lang.GetVideo.EnableVSync, profile.EnableVSync, b => { profile.EnableVSync = b; Client.Game?.SetVSync(b); }));
        opt.Add(CreateCheckboxOption(lang.GetVideo.FullsizeViewport, profile.GameWindowFullSize, b =>
        {
            profile.GameWindowFullSize = b;

            WorldViewportGump viewport = WorldViewportGump.Instance;
            if (viewport == null) return;

            if (b)
            {
                viewport.ResizeGameWindow(new Point(Client.Game.Window.ClientBounds.Width,
                    Client.Game.Window.ClientBounds.Height));
                viewport.SetGameWindowPosition(new Point(0, 0));
                profile.GameWindowPosition = new Point(0, 0);
            }
            else
            {
                viewport.ResizeGameWindow(new Point(600, 480));
                viewport.SetGameWindowPosition(new Point(25, 25));
                profile.GameWindowPosition = new Point(25, 25);
            }

            // Trigger a full update to ensure borders and positioning are correct
            viewport.OnWindowResized();
        }).BeginVisualContainer());
        opt.Add(CreateCheckboxOption(lang.GetVideo.FullScreen, profile.WindowBorderless, b =>
        {
            profile.WindowBorderless = b;
            Client.Game.SetWindowBorderless(b);
        }));
        opt.Add(CreateCheckboxOption(lang.GetVideo.LockViewport, profile.GameWindowLock, b => profile.GameWindowLock = b));
        opt.Add(CreateSliderOption(lang.GetVideo.ViewportX, 0, Client.Game.Window.ClientBounds.Width, profile.GameWindowPosition.X,
            f =>
            {
                profile.GameWindowPosition = new Point((int)f, profile.GameWindowPosition.Y);
                WorldViewportGump.Instance?.SetGameWindowPosition(profile.GameWindowPosition);
            }));
        opt.Add(CreateSliderOption(lang.GetVideo.ViewportY, 0, Client.Game.Window.ClientBounds.Height, profile.GameWindowPosition.Y,
            f =>
            {
                profile.GameWindowPosition = new Point( profile.GameWindowPosition.Y, (int)f);
                WorldViewportGump.Instance?.SetGameWindowPosition(profile.GameWindowPosition);
            }));

        opt.Add(CreateSliderOption(lang.GetVideo.ViewportW, 0, Client.Game.Window.ClientBounds.Width, profile.GameWindowSize.X,
            f =>
            {
                profile.GameWindowSize = new Point((int)f, profile.GameWindowSize.Y);
                WorldViewportGump.Instance?.SetGameWindowPosition(profile.GameWindowPosition);
            }));
        opt.Add(CreateSliderOption(lang.GetVideo.ViewportH, 0, Client.Game.Window.ClientBounds.Height, profile.GameWindowSize.Y,
            f =>
            {
                profile.GameWindowSize = new Point(profile.GameWindowSize.X, (int)f);
                WorldViewportGump.Instance?.SetGameWindowPosition(profile.GameWindowPosition);
            }).EndVisualContainer());

        opt.Add(CreateSpacer());

        int cameraZoomCount = (int)((Client.Game.Scene.Camera.ZoomMax - Client.Game.Scene.Camera.ZoomMin) /
                                    Client.Game.Scene.Camera.ZoomStep);
        int cameraZoomIndex = cameraZoomCount -
                              (int)((Client.Game.Scene.Camera.ZoomMax - Client.Game.Scene.Camera.Zoom) /
                                    Client.Game.Scene.Camera.ZoomStep);

        opt.Add(CreateSliderOption(lang.GetVideo.DefaultZoom, 0, cameraZoomCount, cameraZoomIndex, f =>
        {
            profile.DefaultScale = Client.Game.Scene.Camera.Zoom =
                ((int)f * Client.Game.Scene.Camera.ZoomStep) + Client.Game.Scene.Camera.ZoomMin;
        }));
        opt.Add(CreateCheckboxOption(lang.GetVideo.ZoomWheel, profile.EnableMousewheelScaleZoom, b => profile.EnableMousewheelScaleZoom = b));
        opt.Add(CreateCheckboxOption(lang.GetVideo.ReturnDefaultZoom, profile.RestoreScaleAfterUnpressCtrl, b => profile.RestoreScaleAfterUnpressCtrl = b));

        opt.Add(CreateSpacer());

        opt.Add(CreateCheckboxOption(lang.GetVideo.AltLights, profile.UseAlternativeLights, b => profile.UseAlternativeLights = b));
        opt.Add(CreateCheckboxOption(lang.GetVideo.CustomLLevel, profile.UseCustomLightLevel, b =>
        {
            profile.UseCustomLightLevel = b;

            if (b)
            {
                World.Instance.Light.Overall = profile.LightLevelType == 1
                    ? Math.Min(World.Instance.Light.RealOverall, profile.LightLevel)
                    : profile.LightLevel;
                World.Instance.Light.Personal = 0;
            }
            else
            {
                World.Instance.Light.Overall = World.Instance.Light.RealOverall;
                World.Instance.Light.Personal = World.Instance.Light.RealPersonal;
            }
        }));
        opt.Add(CreateSliderOption(lang.GetVideo.Level, 0, 0x1E, 0x1E - profile.LightLevel, f =>
        {
            profile.LightLevel = (byte)(0x1E - (int)f);

            if (profile.UseCustomLightLevel)
            {
                World.Instance.Light.Overall = profile.LightLevelType == 1
                    ? Math.Min(World.Instance.Light.RealOverall, profile.LightLevel)
                    : profile.LightLevel;
                World.Instance.Light.Personal = 0;
            }
            else
            {
                World.Instance.Light.Overall = World.Instance.Light.RealOverall;
                World.Instance.Light.Personal = World.Instance.Light.RealPersonal;
            }
        }));
        opt.Add(CreateComboBox(lang.GetVideo.LightType, profile.LightLevelType, [lang.GetVideo.LightType_Absolute, lang.GetVideo.LightType_Minimum
        ], i => profile.LightLevelType = i));
        opt.Add(CreateCheckboxOption(lang.GetVideo.DarkNight, profile.UseDarkNights, b => profile.UseDarkNights = b));
        opt.Add(CreateCheckboxOption(lang.GetVideo.ColoredLight, profile.UseColoredLights, b => profile.UseColoredLights = b));

        opt.Add(CreateSpacer());

        opt.Add(CreateCheckboxOption(lang.GetVideo.EnableDeathScreen, profile.EnableDeathScreen, b => profile.EnableDeathScreen = b));
        opt.Add(CreateCheckboxOption(lang.GetVideo.BWDead, profile.EnableBlackWhiteEffect, b => profile.EnableBlackWhiteEffect = b));
        opt.Add(CreateCheckboxOption(lang.GetVideo.MouseThread, Settings.GlobalSettings.RunMouseInASeparateThread, b => Settings.GlobalSettings.RunMouseInASeparateThread = b));
        opt.Add(CreateCheckboxOption(lang.GetVideo.TargetAura, profile.AuraOnMouse, b => profile.AuraOnMouse = b));
        opt.Add(CreateCheckboxOption(lang.GetVideo.AnimWater, profile.AnimatedWaterEffect, b => profile.AnimatedWaterEffect = b));
        opt.Add(CreateCheckboxOption("Enable post processing effects", profile.EnablePostProcessingEffects, b => { profile.EnablePostProcessingEffects = b; GameScene.Instance?.SetPostProcessingSettings(); }));
        opt.Add(CreateComboBox("Processing type", profile.PostProcessingType, ["point", "linear", "anisotropic", "xbr"], i =>
        {
            profile.PostProcessingType = (ushort)i;
            GameScene.Instance?.SetPostProcessingSettings();
        }));

        opt.Add(CreateSpacer());
        opt.Add(CreateCheckboxOption(lang.GetVideo.EnableShadows, profile.ShadowsEnabled, b => profile.ShadowsEnabled = b));
        opt.Add(CreateCheckboxOption(lang.GetVideo.RockTreeShadows, profile.ShadowsStatics, b => profile.ShadowsStatics = b));
        opt.Add(CreateSliderOption(lang.GetVideo.TerrainShadowLevel, Constants.MIN_TERRAIN_SHADOWS_LEVEL, Constants.MAX_TERRAIN_SHADOWS_LEVEL,
            profile.TerrainShadowsLevel, f => profile.TerrainShadowsLevel = (int)f));
    }

    private void SetupInfoBarOptions()
    {
        if (!_options.ContainsKey("Info Bar")) _options.Add("Info Bar", new List<OptionItem>());
        _options["Info Bar"].Add(new OptionItem("Info Bar", InfoBarOptionsContent.Build));
    }

    private void SetupTooltipOptions()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;

        if (!_options.ContainsKey("Tooltips")) _options.Add("Tooltips", new List<OptionItem>());
        List<OptionItem> opt = _options["Tooltips"];

        opt.Add(CreateSliderOption(lang.GetToolTips.ToolTipDelay, 0, 1000, profile.TooltipDelayBeforeDisplay, f => profile.TooltipDelayBeforeDisplay = (int)f));
        opt.Add(CreateSliderOption(lang.GetToolTips.ToolTipBG, 0, 100, profile.TooltipBackgroundOpacity, f => profile.TooltipBackgroundOpacity = (int)f));
        opt.Add(CreateHuePicker(lang.GetToolTips.ToolTipFont, profile.TooltipTextHue, h => profile.TooltipTextHue = h));
    }

    private void SetupSpeechOptions()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;

        if (!_options.ContainsKey("Speech")) _options.Add("Speech", new List<OptionItem>());
        List<OptionItem> opt = _options["Speech"];

        opt.Add(CreateCheckboxOption(lang.GetSpeech.ScaleSpeechDelay, profile.ScaleSpeechDelay, b => profile.ScaleSpeechDelay = b));
        opt.Add(CreateSliderOption(lang.GetSpeech.SpeechDelay, 0, 1000, profile.SpeechDelay, f => profile.SpeechDelay = (int)f));
        opt.Add(CreateCheckboxOption(lang.GetSpeech.SaveJournalE, profile.SaveJournalToFile, b => profile.SaveJournalToFile = b));

        opt.Add(CreateSpacer());

        opt.Add(CreateCheckboxOption(lang.GetSpeech.ChatEnterActivation, profile.ActivateChatAfterEnter, b => profile.ActivateChatAfterEnter = b));
        opt.Add(CreateCheckboxOption(lang.GetSpeech.ChatEnterSpecial, profile.ActivateChatAdditionalButtons, b => profile.ActivateChatAdditionalButtons = b));
        opt.Add(CreateCheckboxOption(lang.GetSpeech.ShiftEnterChat, profile.ActivateChatShiftEnterSupport, b => profile.ActivateChatShiftEnterSupport = b));

        opt.Add(CreateSpacer());

        opt.Add(CreateCheckboxOption(lang.GetSpeech.ChatGradient, profile.HideChatGradient, b => profile.HideChatGradient = b));
        opt.Add(CreateCheckboxOption(lang.GetSpeech.HideGuildChat, profile.IgnoreGuildMessages, b => profile.IgnoreGuildMessages = b));
        opt.Add(CreateCheckboxOption(lang.GetSpeech.HideAllianceChat, profile.IgnoreAllianceMessages, b => profile.IgnoreAllianceMessages = b));

        opt.Add(CreateSpacer());

        opt.Add(CreateHuePicker(lang.GetSpeech.SpeechColor, profile.SpeechHue,  b => profile.SpeechHue = b).BeginVisualContainer());
        opt.Add(CreateHuePicker(lang.GetSpeech.YellColor, profile.YellHue, b => profile.YellHue = b));
        opt.Add(CreateHuePicker(lang.GetSpeech.PartyColor, profile.PartyMessageHue, b => profile.PartyMessageHue = b));
        opt.Add(CreateHuePicker(lang.GetSpeech.AllianceColor, profile.AllyMessageHue, b => profile.AllyMessageHue = b));
        opt.Add(CreateHuePicker(lang.GetSpeech.EmoteColor, profile.EmoteHue, b => profile.EmoteHue = b));
        opt.Add(CreateHuePicker(lang.GetSpeech.WhisperColor, profile.WhisperHue, b => profile.WhisperHue = b));
        opt.Add(CreateHuePicker(lang.GetSpeech.GuildColor, profile.GuildMessageHue, b => profile.GuildMessageHue = b));
        opt.Add(CreateHuePicker(lang.GetSpeech.CharColor, profile.ChatMessageHue, b => profile.ChatMessageHue = b).EndVisualContainer());
    }

    private void SetupCombatOptions()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;

        if (!_options.ContainsKey("Combat")) _options.Add("Combat", new List<OptionItem>());
        List<OptionItem> opt = _options["Combat"];

        opt.Add(CreateCheckboxOption(lang.GetCombatSpells.HoldTabForCombat, profile.HoldDownKeyTab, b => profile.HoldDownKeyTab = b));
        opt.Add(CreateCheckboxOption(lang.GetCombatSpells.QueryBeforeAttack, profile.EnabledCriminalActionQuery, b => profile.EnabledCriminalActionQuery = b));
        opt.Add(CreateCheckboxOption(lang.GetCombatSpells.QueryBeforeBeneficial, profile.EnabledBeneficialCriminalActionQuery, b => profile.EnabledBeneficialCriminalActionQuery = b));
        opt.Add(CreateCheckboxOption(lang.GetCombatSpells.EnableOverheadSpellFormat, profile.EnabledSpellFormat, b => profile.EnabledSpellFormat = b).BeginVisualContainer());
        opt.Add(CreateInputField(lang.GetCombatSpells.SpellOverheadFormat, profile.SpellDisplayFormat, s => profile.SpellDisplayFormat = s).EndVisualContainer());

        opt.Add(CreateCheckboxOption(lang.GetCombatSpells.EnableOverheadSpellHue, profile.EnabledSpellHue, b => profile.EnabledSpellHue = b));
        opt.Add(CreateCheckboxOption(lang.GetCombatSpells.SingleClickForSpellIcons, profile.CastSpellsByOneClick, b => profile.CastSpellsByOneClick = b));
        opt.Add(CreateCheckboxOption(lang.GetCombatSpells.ShowBuffDurationOnOldStyleBuffBar, profile.BuffBarTime, b => profile.BuffBarTime = b));
        opt.Add(CreateCheckboxOption(lang.GetCombatSpells.EnableFastSpellHotkeyAssigning, profile.FastSpellsAssign, b => profile.FastSpellsAssign = b));
        opt.Add(CreateCheckboxOption(lang.GetCombatSpells.EnableDPSCounter, profile.ShowDPS, b => profile.ShowDPS = b));

        opt.Add(CreateHuePicker(lang.GetCombatSpells.InnocentColor, profile.InnocentHue, b => profile.InnocentHue = b).BeginVisualContainer());
        opt.Add(CreateHuePicker(lang.GetCombatSpells.BeneficialSpell, profile.BeneficHue, b => profile.BeneficHue = b));
        opt.Add(CreateHuePicker(lang.GetCombatSpells.FriendColor, profile.FriendHue, b => profile.FriendHue = b));
        opt.Add(CreateHuePicker(lang.GetCombatSpells.HarmfulSpell, profile.HarmfulHue, b => profile.HarmfulHue = b));
        opt.Add(CreateHuePicker(lang.GetCombatSpells.Criminal, profile.CriminalHue, b => profile.CriminalHue = b));
        opt.Add(CreateHuePicker(lang.GetCombatSpells.NeutralSpell, profile.NeutralHue, b => profile.NeutralHue = b));
        opt.Add(CreateHuePicker(lang.GetCombatSpells.CanBeAttackedHue, profile.CanAttackHue, b => profile.CanAttackHue = b));
        opt.Add(CreateHuePicker(lang.GetCombatSpells.Murderer, profile.MurdererHue, b => profile.MurdererHue = b));
        opt.Add(CreateHuePicker(lang.GetCombatSpells.Enemy, profile.EnemyHue, b => profile.EnemyHue = b).EndVisualContainer());
    }

    private class OptionItem(string searchText, Func<Widget> createWidget, string? tags = null, bool skipSearch = false)
    {
        public bool IsStartVisualContainer { get; private set; }
        public bool IsEndVisualContainer { get; private set; }

        private string? _tags = tags;

        public bool MatchesSearch(string text)
        {
            if (skipSearch) return false;

            if (searchText.Contains(text, StringComparison.OrdinalIgnoreCase)) return true;

            return _tags.NotNullNotEmpty() && _tags!.Contains(text, StringComparison.OrdinalIgnoreCase);
        }

        public Widget GetWidget
        {
            get
            {
                field ??= createWidget();

                return field;
            }
        }

        public OptionItem SetTags(string tags)
        {
            _tags = tags;
            return this;
        }

        public OptionItem BeginVisualContainer()
        {
            IsStartVisualContainer = true;
            return this;
        }

        public OptionItem EndVisualContainer()
        {
            IsEndVisualContainer = true;
            return this;
        }
    }

    private class VisualContainerData
    {
        public int StartY;
        public int EndY;

        public Rectangle GetRect(int width)
        {
            var current = new Rectangle(0, StartY, width, EndY - StartY);

            return current;
        }
    }
}
