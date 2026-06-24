using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyWinUIUtilities.Application.Settings;
using PersonalWinUIApp.Views.Account;
using PersonalWinUIApp.Views.Files;
using PersonalWinUIApp.Views.Home;
using PersonalWinUIApp.Views.Musics;
using PersonalWinUIApp.Views.Pictures;
using PersonalWinUIApp.Views.Settings;
using PersonalWinUIApp.Views.Settings.Pages;
using PersonalWinUIApp.Views.Videos;
using PersonalWinUIApp.Views.Videos.Pages;

namespace PersonalWinUIApp.Tools
{
    public enum StartPage
    {
        Default,
        LastPage,
        CustomPage
    }

    public static class StartPageHelper
    {
        public static Type PageType(string name)
        {
            Type target = name switch
            {
                "AppearancePage" => typeof(AppearancePage),
                "LanguagePage" => typeof(LanguagePage),
                "StartupPage" => typeof(StartupPage),
                "NotificationsPage" => typeof(NotificationsPage),
                "NetworkPage" => typeof(NetworkPage),
                "PerformancePage" => typeof(PerformancePage),
                "PrivacyPage" => typeof(PrivacyPage),
                "AboutPage" => typeof(AboutPage),
                "HomePage" => typeof(HomePage),
                "VideosPage" => typeof(VideosPage),
                "MoviesPage" => typeof(MoviesPage),
                "SeriesPage" => typeof(SeriesPage),
                "AnimationPage" => typeof(AnimationPage),
                "MusicsPage" => typeof(MusicsPage),
                "PicturesPage" => typeof(PicturesPage),
                "AccountPage" => typeof(AccountPage),
                "FilesPage" => typeof(FilesPage),
                "SettingsPage" => typeof(SettingsPage),
                _ => typeof(HomePage)
            };

            return target;
        }

        public static Type CheckStatus(StartPage kind)
        {
            return kind switch
            {
                StartPage.Default => typeof(HomePage),

                StartPage.LastPage => PageType(LocalSettingsStorage.LoadOrCreate("AppLastNavigation", "HomePage")),

                StartPage.CustomPage => PageType(LocalSettingsStorage.LoadOrCreate("AppCustomStartPage", "HomePage")),

                _ => typeof(HomePage)
            };
        }
    }
}
