using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using MyWinUIUtilities.Application.Settings;
using System.ComponentModel;
using Microsoft.UI.Windowing;
using MyWinUIUtilities.Application;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PersonalWinUIApp.Views.Settings.UserControls;

public sealed partial class NotificationUserControl : UserControl
{
    private bool _isNotificationEnableLoaded;
    private bool _isDisturbLoaded;
    private bool _isSuppressLoaded;
    private bool _isSoundLoaded;
    public NotificationUserControl()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        IsEnableLoaded();
    }

    private void IsEnableLoaded()
    {
        NotificationEnabledSwitch.IsOn = LocalSettingsStorage.Load<bool>("EnableNotifications");
        DisturbSwitch.IsOn = LocalSettingsStorage.Load<bool>("DoNotDisturb");
        SuppressSwitch.IsOn = LocalSettingsStorage.Load<bool>("SuppressFullscreen");
        SoundSwitch.IsOn = LocalSettingsStorage.Load<bool>("IsNotificationSoundOn");

        _isNotificationEnableLoaded = true;
        _isDisturbLoaded = true;
        _isSuppressLoaded = true;
        _isSoundLoaded = true;
    }

    private void NotificationEnabledSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_isNotificationEnableLoaded) return;

        var check = NotificationEnabledSwitch.IsOn;
        LocalSettingsStorage.Save("EnableNotifications", check);
        if (!check)
        {
            DisturbSwitch.IsOn = false;
            SuppressSwitch.IsOn = false;
            SoundSwitch.IsOn = false;
        }
        else
        {
            NotificationServices.ShowNotification(notif =>
            {
                return notif
                    .AddText("Activate Notification!")
                    .AddText("You activated your notifications. Congratulations!");
            });
        }
    }

    private void DisturbSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_isDisturbLoaded) return;

        var check = DisturbSwitch.IsOn;
        LocalSettingsStorage.Save("DoNotDisturb", check);
    }

    private void SuppressSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_isSuppressLoaded) return;

        var check = SuppressSwitch.IsOn;
        LocalSettingsStorage.Save("SuppressFullscreen", check);
    }

    private void SoundSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_isSoundLoaded) return;

        var check = SoundSwitch.IsOn;
        LocalSettingsStorage.Save("IsNotificationSoundOn", check);
    }
}
