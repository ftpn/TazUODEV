#nullable enable
using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace ClassicUO.Game.UI.MyraWindows;

public class OptionsWindow : MyraControl
{
    /// <summary>
    /// Category, (sub category?, widget)
    /// </summary>
    private Dictionary<string, List<OptionItem>> _options = new();

    private MyraGrid _mainArea = new();
    private VerticalStackPanel _optionsPanel = new();

    public OptionsWindow() : base("Options")
    {
        UIManager.ForEach<OptionsWindow>(w => { if(w != this) w.Dispose(); });

        SetupOptions();
        Build();

        CenterInViewPort();
    }

    private void SetupOptions()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;

        if(!_options.ContainsKey("General")) _options.Add("General", new List<OptionItem>());

        _options["General"].Add(new (() => MyraCheckButton.CreateWithCallback(
            profile.HighlightGameObjects,
            b => profile.HighlightGameObjects = b,
            lang.GetGeneral.HighlightObjects))
            );
    }

    private void Build()
    {
        _mainArea.MinWidth = 400;
        _mainArea.MinHeight = 400;

        _mainArea.AddColumn(Proportion.Auto);
        _mainArea.AddColumn(Proportion.Fill);

        VerticalStackPanel categoryPanel = new();
        _mainArea.AddWidget(categoryPanel, 0, 0);

        var optionsStack = new VerticalStackPanel() { Height = 500 };
        optionsStack.Widgets.Add(_optionsPanel);
        _mainArea.AddWidget(optionsStack, 0, 1);

        foreach (string category in _options.Keys) categoryPanel.Widgets.Add(ApplyTabStyleToButton(new MyraButton(category, () => { ShowPage(category); })));

        SetRootContent(_mainArea);
    }

    private static ButtonStyle _tabButtonStyle;
    private MyraButton ApplyTabStyleToButton(MyraButton tabButton)
    {
        if (_tabButtonStyle == null)
        {
            ButtonStyle tabControlStyle = Stylesheet.Current.ButtonStyle;
            _tabButtonStyle = new(tabControlStyle);


            _tabButtonStyle.Background = new SolidBrush(Color.Transparent);
            _tabButtonStyle.Border = new SolidBrush(new Color(0, 0, 0, MyraStyle.STANDARD_BORDER_ALPHA));
            _tabButtonStyle.BorderThickness = new Thickness(1);
            _tabButtonStyle.LabelStyle.Font = MyraStyle.UiFont;
        }

        tabButton.ApplyButtonStyle(_tabButtonStyle);

        return tabButton;
    }

    private void ShowPage(string category)
    {
        _optionsPanel.Widgets.Clear();

        foreach (OptionItem optionItem in _options[category]) _optionsPanel.Widgets.Add(optionItem.GetWidget);
    }

    private class OptionItem(Func<Widget> createWidget)
    {
        public Widget GetWidget
        {
            get
            {
                if (field is null)
                    field = createWidget();

                return field;
            }
        }
    }
}
