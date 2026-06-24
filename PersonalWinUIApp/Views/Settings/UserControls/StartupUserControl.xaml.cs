using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyWinUIUtilities.Application;
using MyWinUIUtilities.Application.Settings;
using PersonalWinUIApp.Tools;
using PersonalWinUIApp.Views.Home;
using Windows.ApplicationModel;

namespace PersonalWinUIApp.Views.Settings.UserControls;

public sealed partial class StartupUserControl : UserControl
{
    private bool _isInitializing;
    private bool _isPageKindLoaded;
    private bool _isPageCustomLoaded;
    private bool _isSizeLoaded;
    private bool _isPositionLoaded;
    private bool _isMaximizeStatLoaded;
    private bool _isAlwaysOnTopLoaded;

    public StartupUserControl()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        LoadRunAtStartup();
        LoadMaximizeStat();
        LoadMinToTray();
        LoadSize();
        LoadAlwaysOnTop();
        LoadPosition();
        LoadCloseToTray();
        LoadPageKind();
        LoadCustomPage();
    }

    private void LoadSize()
    {
        var check = LocalSettingsStorage.Load<bool>("AppIsSize");
        reSizeSwitch.IsOn = check;

        _isSizeLoaded = true;
    }

    private void LoadAlwaysOnTop()
    {
        var check = LocalSettingsStorage.Load<bool>("AlwaysOnTop");
        AlwaysOnTop.IsOn = check;

        _isAlwaysOnTopLoaded = true;
    }

    private void LoadMaximizeStat()
    {
        var check = LocalSettingsStorage.Load<bool>("WindowMaximizedIsOn");
        reMaximizeStat.IsOn = check;

        _isMaximizeStatLoaded = true;
    }

    private void LoadPosition()
    {
        var check = LocalSettingsStorage.Load<bool>("AppIsPosition");
        rePositionSwitch.IsOn = check;

        _isPositionLoaded = true;
    }

    private async void LoadRunAtStartup()
    {
        RunAtStartup.IsOn = await StartupManager.IsStartupEnabledAsync("MyStartupTask");
        _isInitializing = true;
    }

    private void LoadMinToTray()
    {
        var check = LocalSettingsStorage.LoadOrCreate("AppMinToTray", false);
        MinToTray.IsOn = check;
    }

    private void LoadCloseToTray()
    {
        var check = LocalSettingsStorage.LoadOrCreate("AppCloseToTray", false);
        CloseToTray.IsOn = check;
    }

    private void LoadPageKind()
    {
        var startPage = LocalSettingsStorage.Load<StartPage>("AppStartPageKind");
        StartPageKind.SelectedIndex = startPage switch
        {
            StartPage.Default => 0,
            StartPage.LastPage => 1,
            StartPage.CustomPage => 2,
            _ => 0
        };

        if (startPage == StartPage.CustomPage) PageKindExpander.IsOpen = true;


        _isPageKindLoaded = true;
    }

    private void LoadCustomPage()
    {
        var customPage = LocalSettingsStorage.Load<string>("AppCustomStartPage");
        StartCustomPage.SelectedIndex = customPage switch
        {
            "HomePage" => 0,
            "VideosPage" => 1,
            "MoviesPage" => 2,
            "SeriesPage" => 3,
            "AnimationPage" => 4,
            "MusicsPage" => 5,
            "PicturesPage" => 6,
            "FilesPage" => 7,
            "SettingsPage" => 8,
            _ => 0
        };

        _isPageCustomLoaded = true;
    }

    private async void RunAtStartup_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_isInitializing) return;

        if (RunAtStartup.IsOn)
        {
            StartupTaskState state = await StartupManager.EnableStartupAsync("MyStartupTask");
            System.Diagnostics.Debug.WriteLine($"Startup state: {state}");
        }
        else
        {
            await StartupManager.DisableStartupAsync("MyStartupTask");
        }
    }

    private void MinToTray_Toggled(object sender, RoutedEventArgs e)
    {
        var check = MinToTray.IsOn;
        LocalSettingsStorage.Save("AppMinToTray", check);
    }

    private void CloseToTray_Toggled(object sender, RoutedEventArgs e)
    {
        var check = CloseToTray.IsOn;
        LocalSettingsStorage.Save("AppCloseToTray", check);
    }

    private void StartPageKind_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isPageKindLoaded) return;

        if (StartPageKind.SelectedItem is ComboBoxItem selectedItem)
        {
            var kind = selectedItem.Tag?.ToString();

            StartPage startPage = kind switch
            {
                "default" => StartPage.Default,
                "last" => StartPage.LastPage,
                "custom" => StartPage.CustomPage,
                _ => StartPage.Default
            };
            LocalSettingsStorage.Save("AppStartPageKind", startPage);

            if (kind == "custom") PageKindExpander.IsOpen = true;
            else PageKindExpander.IsOpen = false;
        }
    }

    private void StartCustomPage_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isPageCustomLoaded) return;

        if (StartCustomPage.SelectedItem is ComboBoxItem selectedItem)
        {
            var pageName = selectedItem.Tag?.ToString();
            LocalSettingsStorage.Save("AppCustomStartPage", pageName);
        }
    }

    private void ReSizeSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_isSizeLoaded) return;

        var check = reSizeSwitch.IsOn;
        LocalSettingsStorage.Save("AppIsSize", check);
    }

    private void RePositionSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_isPositionLoaded) return;

        var check = rePositionSwitch.IsOn;
        LocalSettingsStorage.Save("AppIsPosition", check);
    }

    private void ReMaximizeStat_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_isMaximizeStatLoaded) return;

        var check = reMaximizeStat.IsOn;
        LocalSettingsStorage.Save("WindowMaximizedIsOn", check);
    }

    private void AlwaysOnTop_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_isAlwaysOnTopLoaded) return;

        var check = AlwaysOnTop.IsOn;
        var window = App.MainWindow!;
        AppWindowSetting.SaveAlwaysOnTop(window, check);
    }
}
