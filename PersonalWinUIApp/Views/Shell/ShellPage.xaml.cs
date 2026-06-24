using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.ApplicationModel.Resources;
using MyWinUIUtilities.Navigation;
using PersonalWinUIApp.Tools;
using PersonalWinUIApp.Views.Account;
using PersonalWinUIApp.Views.Home;
using PersonalWinUIApp.Views.Settings;
using PersonalWinUIApp.Views.Settings.Pages;
using Windows.UI;

namespace PersonalWinUIApp.Views.Shell;

public sealed partial class ShellPage : Page
{
    private readonly NavigationServices services;
    public INavigationServices Services => services;
    public ObservableCollection<BreadcrumbItem> BreadcrumbItems { get; } = [];

    public ShellPage()
    {
        InitializeComponent();
        services = new NavigationServices(navigationView, navigationFrame);

        Services.BreadcrumbChanged += OnBreadcrumbChanged;
        AppBreadcrumbBar.ItemClicked += AppBreadcrumbBar_ItemClicked;
    }

    private void AppBreadcrumbBar_ItemClicked(object? sender, BreadcrumbItem items)
    {
        Services.NavigateFromBreadcrumb(items, NavigationTransition.SlideFromLeft);
    }

    private void OnBreadcrumbChanged(IReadOnlyList<BreadcrumbItem> list)
    {
        BreadcrumbItems.Clear();

        foreach (var item in list)
        {
            BreadcrumbItems.Add(item);
        }
    }

    public void SetMainPage(Type mainPage)
    {
        Services.SetMainPage(mainPage);
    }

    private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        Services.ItemsInvoked(args, level:0);
    }

    private void NavigationView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        services.GoBack();
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        Services.Dispose();
    }

    public void NavigateToAccountPage()
    {
        Services.ExternalNavigate(typeof(AccountPage));
    }

    public void SettingsNavigation(SettingsPages pages)
    {
        Type target = pages switch
        {
            SettingsPages.Appearance => typeof(AppearancePage),
            SettingsPages.Language => typeof(LanguagePage),
            SettingsPages.Startup => typeof(StartupPage),
            SettingsPages.Notifications => typeof(NotificationsPage),
            SettingsPages.Network => typeof(NetworkPage),
            SettingsPages.Performance => typeof(PerformancePage),
            SettingsPages.Privacy => typeof(PrivacyPage),
            SettingsPages.About => typeof(AboutPage),
            _ => typeof(SettingsPage)
        };

        Services.ExternalNavigate(target, level: 1, transition:NavigationTransition.SlideFromRight);
    }

    public void ReloadFrame(object? parameter = null)
    {
        Services.ReloadCurrentPage(parameter);
    }
}
