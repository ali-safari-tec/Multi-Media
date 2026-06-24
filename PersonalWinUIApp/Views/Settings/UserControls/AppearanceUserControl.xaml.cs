using System;
using System.Collections.Generic;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using MyWinUIUtilities.Application;
using MyWinUIUtilities.Application.Settings;
using PersonalWinUIApp.Tools;
using Windows.System;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace PersonalWinUIApp.Views.Settings.UserControls;

public sealed partial class AppearanceUserControl : UserControl
{
    private bool _isThemeLoaded;
    private bool _isAccentLoaded;
    private bool _isEffectLoaded;

    public List<SolidColorBrush> AccentColors { get; } =
    [
        new SolidColorBrush(Color.FromArgb(255, 225, 185, 0)),
        new SolidColorBrush(Color.FromArgb(255, 225, 140, 0)),
        new SolidColorBrush(Color.FromArgb(255, 247, 99, 12)),
        new SolidColorBrush(Color.FromArgb(255, 202, 80, 16)),
        new SolidColorBrush(Color.FromArgb(255, 218, 59, 1)),
        new SolidColorBrush(Color.FromArgb(255, 239, 105, 80)),
        new SolidColorBrush(Color.FromArgb(255, 232, 17, 35)),
        new SolidColorBrush(Color.FromArgb(255, 234, 0, 94)),
        new SolidColorBrush(Color.FromArgb(255, 227, 0, 140)),
        new SolidColorBrush(Color.FromArgb(255, 191, 0, 119)),
        new SolidColorBrush(Color.FromArgb(255, 154, 0, 137)),
        new SolidColorBrush(Color.FromArgb(255, 104, 33, 122)),
        new SolidColorBrush(Color.FromArgb(255, 91, 55, 177)),
        new SolidColorBrush(Color.FromArgb(255, 0, 120, 215)),
        new SolidColorBrush(Color.FromArgb(255, 0,  99, 177)),
        new SolidColorBrush(Color.FromArgb(255, 3, 131, 135)),
        new SolidColorBrush(Color.FromArgb(255, 0, 153, 188)),
        new SolidColorBrush(Color.FromArgb(255, 0, 183, 195)),
        new SolidColorBrush(Color.FromArgb(255, 16, 137, 62)),
        new SolidColorBrush(Color.FromArgb(255, 16, 124, 16))
    ];

    public AppearanceUserControl()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void LoadThemeCombo()
    {
        var theme = LocalSettingsStorage.Load<ThemeType>("AppTheme");

        ThemeCombo.SelectedIndex = theme switch
        {
            ThemeType.Light => 0,
            ThemeType.Dark => 1,
            _ => 2
        };

        _isThemeLoaded = true;
    }

    private void LoadAccentCombo()
    {
        var isCustom = LocalSettingsStorage.Load<bool>("IsAppAccentCustom");
        var lastIndext = LocalSettingsStorage.Load<int>("AppAccentGridIndex");

        AccentCombo.SelectedIndex = isCustom switch
        {
            true => 0,
            false => 1
        };

        if (isCustom) CSEAccent.IsOpen = true;
        if (isCustom && lastIndext >= 0) AccentColorsGrid.SelectedIndex = isCustom ? lastIndext : 0;

        _isAccentLoaded = true;
    }

    private void LoadTransparency()
    {
        var transparency = LocalSettingsStorage.Load<bool>("AppTransparency");

        SwitchEffect.IsOn = transparency switch
        {
            true => true,
            false => false
        };

        if (transparency)
        {
            var kind = LocalSettingsStorage.Load<TransparencyKind>("AppTransparencyType");

            TransparencyCombo.SelectedIndex = kind switch
            {
                TransparencyKind.Acrylic => 0,
                TransparencyKind.MicaBaseAlt => 1,
                _ => 2,
            };
        }

        _isEffectLoaded = true;
    }

    private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isThemeLoaded) return;

        if (ThemeCombo.SelectedItem is ComboBoxItem selectedItem)
        {
            var selectedContent = selectedItem.Tag.ToString();
            var root = this.XamlRoot.Content as FrameworkElement;
            var IsTransparent = LocalSettingsStorage.Load<bool>("AppTransparency");

            ApplicationTheme systemTheme = AppWindowSetting.GetSystemTheme();
            bool isLight = systemTheme == ApplicationTheme.Light;

            var lightBrush = new SolidColorBrush(Color.FromArgb(255, 232, 232, 232));
            var darkBrush = new SolidColorBrush(Color.FromArgb(255, 32, 32, 32));

            switch (selectedContent)
            {
                case "Light":
                    if (!IsTransparent) App.MainWindow!.Grid.Background = lightBrush;
                    AppWindowSetting.SetApplicationTheme(root, ThemeType.Light);
                    TitleBarSetColorLogic.SetColors(ThemeType.Light);
                    LocalSettingsStorage.Save("AppTheme", ThemeType.Light);
                    break;
                case "Dark":
                    if (!IsTransparent) App.MainWindow!.Grid.Background = darkBrush;
                    AppWindowSetting.SetApplicationTheme(root, ThemeType.Dark);
                    TitleBarSetColorLogic.SetColors(ThemeType.Dark);
                    LocalSettingsStorage.Save("AppTheme", ThemeType.Dark);
                    break;
                case "System":
                default:
                    if (!IsTransparent) App.MainWindow!.Grid.Background = isLight
                            ? lightBrush
                            : darkBrush;
                    AppWindowSetting.SetApplicationTheme(root, ThemeType.System);
                    TitleBarSetColorLogic.SetColors(ThemeType.System);
                    LocalSettingsStorage.Save("AppTheme", ThemeType.System);
                    break;
            }
        }
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        LoadThemeCombo();
        LoadTransparency();
        LoadAccentCombo();
    }

    private async void ThemeExpanderButton_Click(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri("ms-settings:themes"));
    }

    private void AccentColorsGrid_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is SolidColorBrush brush)
        {
            var check = LocalSettingsStorage.Load<Color>("AppAccent");
            var color = brush.Color;

            if (check == color) return;

            var root = this.XamlRoot.Content as FrameworkElement;
            var resource = Application.Current.Resources;
            var itemIndext = AccentColors.IndexOf(brush);

            AccentPaletteBuilder.ApplyAccentPalette(color, resource);
            AccentPaletteBuilder.ForceThemeRefresh(root);
            LocalSettingsStorage.Save("AppAccent", color);
            LocalSettingsStorage.Save("IsAppAccentCustom", true);
            LocalSettingsStorage.Save("AppAccentGridIndex", itemIndext);
        }
    }

    private void SwitchEffect_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_isEffectLoaded) return;
        var window = App.MainWindow;
        var savedTheme = LocalSettingsStorage.Load<ThemeType>("AppTheme");
        bool check = SwitchEffect.IsOn;

        if (window is not null)
        {

            AppWindowSetting.ApplyTransparency(check, window, window!.Grid, savedTheme, TransparencyKind.MicaBaseAlt);
            LocalSettingsStorage.Save("AppTransparency", check);
        }
    }

    private void AccentCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isAccentLoaded)
            return;

        if (AccentCombo.SelectedIndex == 0)
        {
            CSEAccent.IsOpen = true;

            var lastIndex = LocalSettingsStorage.Load<int>("AppAccentGridIndex");

            if (lastIndex >= 0 && lastIndex < AccentColors.Count)
            {
                AccentColorsGrid.SelectedIndex = lastIndex;

                var brush = AccentColors[lastIndex];
                var color = brush.Color;

                if (this.XamlRoot?.Content is FrameworkElement root)
                {
                    var resources = Application.Current.Resources;

                    AccentPaletteBuilder.ApplyAccentPalette(color, resources);
                    AccentPaletteBuilder.ForceThemeRefresh(root);
                }

                LocalSettingsStorage.Save("AppAccent", color);
                LocalSettingsStorage.Save("IsAppAccentCustom", true);
            }
        }
        else
        {
            CSEAccent.IsOpen = false;

            var settings = new UISettings();
            var systemAccent = settings.GetColorValue(UIColorType.Accent);

            if (this.XamlRoot?.Content is FrameworkElement root)
            {
                var resources = Application.Current.Resources;
                AccentPaletteBuilder.ApplyAccentPalette(systemAccent, resources);

                AccentPaletteBuilder.ForceThemeRefresh(root);
            }

            LocalSettingsStorage.Save("AppAccent", systemAccent);
            LocalSettingsStorage.Save("IsAppAccentCustom", false);
        }
    }

    private async void AccentButton_Click(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri("ms-settings:colors"));
    }

    private void TransparencyCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isEffectLoaded) return;

        if (TransparencyCombo.SelectedItem is ComboBoxItem selectedItem)
        {
            var selectedContent = selectedItem.Tag.ToString();
            var savedTheme = LocalSettingsStorage.Load<ThemeType>("AppTheme");
            var window = App.MainWindow;

            switch (selectedContent)
            {
                case "Acrylic":
                    AppWindowSetting.ApplyTransparency(true, window, window!.Grid, savedTheme, TransparencyKind.Acrylic);
                    LocalSettingsStorage.Save("AppTransparencyType", TransparencyKind.Acrylic);
                    break;
                case "MicaBaseAlt":
                    AppWindowSetting.ApplyTransparency(true, window, window!.Grid, savedTheme, TransparencyKind.MicaBaseAlt);
                    LocalSettingsStorage.Save("AppTransparencyType", TransparencyKind.MicaBaseAlt);
                    break;
                case "MicaBase":
                default:
                    AppWindowSetting.ApplyTransparency(true, window, window!.Grid, savedTheme, TransparencyKind.MicaBase);
                    LocalSettingsStorage.Save("AppTransparencyType", TransparencyKind.MicaBase);
                    break;
            }
        }
    }
}