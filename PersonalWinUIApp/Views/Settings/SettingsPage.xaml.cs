using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MyWinUIUtilities.Navigation;
using PersonalWinUIApp.Views.Settings.Pages;
using PersonalWinUIApp.Views.Shell;

namespace PersonalWinUIApp.Views.Settings;

public enum SettingsPages
{
    Appearance,
    Language,
    Startup,
    Notifications,
    Network,
    Performance,
    Privacy,
    About
}

public sealed partial class SettingsPage : Page
{
    private static ShellPage Shell => App.MainWindow!.ShellPage;
    public SettingsPage()
    {
        InitializeComponent();
    }

    private void Appearance_Click(object sender, RoutedEventArgs e)
    {
        Shell.SettingsNavigation(SettingsPages.Appearance);
    }

    private void AppLanguage_Click(object sender, RoutedEventArgs e)
    {
        Shell.SettingsNavigation(SettingsPages.Language);
    }

    private void Startup_Click(object sender, RoutedEventArgs e)
    {
        Shell.SettingsNavigation(SettingsPages.Startup);
    }

    private void Notifications_Click(object sender, RoutedEventArgs e)
    {
        Shell.SettingsNavigation(SettingsPages.Notifications);
    }

    private void Network_Click(object sender, RoutedEventArgs e)
    {
        Shell.SettingsNavigation(SettingsPages.Network);
    }

    private void Performance_Click(object sender, RoutedEventArgs e)
    {
        Shell.SettingsNavigation(SettingsPages.Performance);
    }

    private void Privacy_Click(object sender, RoutedEventArgs e)
    {
        Shell.SettingsNavigation(SettingsPages.Privacy);
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        Shell.SettingsNavigation(SettingsPages.About);
    }
}
