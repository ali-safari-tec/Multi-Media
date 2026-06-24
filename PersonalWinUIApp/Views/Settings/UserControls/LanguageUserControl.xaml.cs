using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualBasic;
using MyWinUIUtilities.Application.Settings;
using MyWinUIUtilities.Tools;
using PersonalWinUIApp.Tools;
using PersonalWinUIApp.Views.Shell;
using Windows.System;

namespace PersonalWinUIApp.Views.Settings.UserControls;

public sealed partial class LanguageUserControl : UserControl
{
    public sealed record LanguagePageNavigationParameter(bool ShowDirectionInfoBar);

    private bool _isLanguageLoaded;
    private bool _isRegionLoaded;
    private bool _isDirectionLoaded;
    private bool _isFontInitialize;

    private static AppFont? DefaultPersianFont => AppFont.AllFonts.FirstOrDefault(i => i.Language == FontLanguage.Persian);
    private static AppFont? DefaultEnglishFont => AppFont.AllFonts.FirstOrDefault(i => i.Language == FontLanguage.English);

    public List<RegionName> Regions { get; } = RegionName.Regions();
    public ClockView ClockView { get; } = new ClockView();
    public string TimeZone { get; } = TimeZoneInfo.Local.DisplayName;
    private static ShellPage Shell => App.MainWindow!.ShellPage;
    private FontLanguage CurrentLanguage;

    public IEnumerable<AppFont> AppFonts => CurrentLanguage == FontLanguage.Persian ? AppFont.AllFonts.Where(x => x.Language == FontLanguage.Persian) : AppFont.AllFonts.Where(x => x.Language == FontLanguage.English);


    public LanguageUserControl()
    {
        InitializeCurrentLanguage();
        InitializeComponent();
        ClockView.PropertyChanged += ClockView_PropertyChanged;
    }

    public void ShowDirectionInfoBar(bool show)
    {
        DirectionInfoBar.IsOpen = show;
    }

    private void InitializeCurrentLanguage()
    {
        var lang = LocalSettingsStorage.LoadOrCreate("AppLanguage", "en");
        CurrentLanguage = lang == "fa" ? FontLanguage.Persian : FontLanguage.English;
    }

    private void ClockView_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpdateClock();
    }

    private void UpdateClock()
    {
        var lang = LocalSettingsStorage.Load<string>("AppLanguage");

        if (lang == "fa")
        {
            Date.Text = ClockView.PersianFullDateWithDay;
            Time.Text = ClockView.PersianHourMinute12Short;
        }
        else //Because we only have 2 language
        {
            Date.Text = ClockView.FullDateWithDay;
            Time.Text = ClockView.HourMinute12Short;
        }
    }

    private void LoadLanguageCombo()
    {
        var theme = LocalSettingsStorage.Load<string>("AppLanguage");

        LanguageCombo.SelectedIndex = theme switch
        {
            "en" => 0,
            "fa" => 1,
            _ => 0
        };

        if (theme == "fa")
        {
            LanguageExpander.IsOpen = true;
            var IsRightToLeft = LocalSettingsStorage.Load<bool>("AppDirection");

            DirectionToggle.IsOn = IsRightToLeft;

            _isDirectionLoaded = true;
        }
        else _isDirectionLoaded = false;

    _isLanguageLoaded = true;
    }

    private void LoadFontCombo()
    {
        var currentFont = LocalSettingsStorage.Load<string>("AppFontName");

        if (CurrentLanguage == FontLanguage.Persian)
        {
            FontCombo.SelectedIndex = currentFont switch
            {
                "وزیر" => 0,
                "ساحل" => 1,
                "صمیم" => 2,
                "شبنم" => 3,
                "استعداد" => 4,
                _ => 0
            };
        }
        else
        {
            FontCombo.SelectedIndex = currentFont switch
            {
                "Segoe UI" => 0,
                "Inter" => 1,
                "IBM Plex Sans" => 2,
                "Noto Sans" => 3,
                "Manrope" => 4,
                _ => 0
            };
        }

        _isFontInitialize = true;
    }

    private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isLanguageLoaded) return;

        if (LanguageCombo.SelectedItem is ComboBoxItem selectedItem)
        {
            var lang = selectedItem.Tag.ToString();
            var loc = Application.Current.Resources["Loc"] as LocalizationManager;

            loc?.ChangeLanguage(lang);
            LocalSettingsStorage.Save("AppLanguage", lang);

            if (lang == "fa")
            {
                LanguageExpander.IsOpen = true;
                CurrentLanguage = FontLanguage.Persian;

                var fontFamily = DefaultPersianFont!.FontFamily ?? new FontFamily("ms-appx:///Assets/Fonts/Vazirmatn-FD-Regular.ttf#Vazirmatn FD");

                FontManager.Instance.CurrentFont = fontFamily;
                LocalSettingsStorage.Save("AppFontName", DefaultPersianFont.Name);

                _isDirectionLoaded = true;
            }
            else
            {
                LanguageExpander.IsOpen = false;

                FlowDirectionChanger.SetDirection(false);
                LocalSettingsStorage.Save("AppDirection", false);
                CurrentLanguage = FontLanguage.English;

                var fontFamily = DefaultEnglishFont!.FontFamily ?? new FontFamily("Segoe UI");

                FontManager.Instance.CurrentFont = fontFamily;
                LocalSettingsStorage.Save("AppFontName", DefaultEnglishFont.Name);

                _isDirectionLoaded = false;
            }

            Shell.ReloadFrame();
        }
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        LoadLanguageCombo();
        LoadFontCombo();
        LoadRegionCombo();
        UpdateClock();
    }

    private void LoadRegionCombo()
    {
        var saveRegion = LocalSettingsStorage.Load<string>("AppRegion");
        RegionCombo.SelectedItem = Regions.FirstOrDefault(s => s.Code == saveRegion);

        _isRegionLoaded = true;
    }

    private void RegionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isRegionLoaded) return;

        if (RegionCombo.SelectedItem is RegionName selectItem)
        {
            var region = selectItem.Code;
            LocalSettingsStorage.Save("AppRegion", region);
        }
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        ClockView.PropertyChanged -= ClockView_PropertyChanged;
        ClockView.Dispose();
    }

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri("ms-settings:dateandtime"));
    }

    private void DirectionToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_isDirectionLoaded) return;

        var check = DirectionToggle.IsOn;

        FlowDirectionChanger.SetDirection(check);
        LocalSettingsStorage.Save("AppDirection", check);
        LocalSettingsStorage.Save("DirectionInfoBar", check);


        var parameter = new LanguagePageNavigationParameter(check);


        Shell.ReloadFrame(parameter);
    }

    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isFontInitialize) return;

        if (FontCombo.SelectedItem is AppFont appFont)
        {
            FontManager.Instance.CurrentFont = appFont.FontFamily;
            LocalSettingsStorage.Save("AppFontName", appFont.Name);
        }
    }

    private void Slider_ValueChanged(object sender,RangeBaseValueChangedEventArgs e)
    {
        var value = Slider.Value;
        LocalSettingsStorage.Save("AppFontScale", value);
    }
}
