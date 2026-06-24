using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using MyWinUIUtilities.Application;
using MyWinUIUtilities.Application.Settings;
using MyWinUIUtilities.Tools;
using PersonalWinUIApp.Tools;
using Windows.UI.ViewManagement;

namespace PersonalWinUIApp
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        public static MainWindow? MainWindow { get; private set; }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            PreLoaded();

            MainWindow = new MainWindow();
            MainWindow.Activate();

            Loaded();
        }

        private static void Loaded()
        {
            var savedTheme = LocalSettingsStorage.LoadOrCreate("AppTheme", ThemeType.System);
            var savedTransparency = LocalSettingsStorage.LoadOrCreate("AppTransparency", true);
            var savedDirection = LocalSettingsStorage.LoadOrCreate("AppDirection", false);
            var savedFontName = LocalSettingsStorage.LoadOrCreate("AppFontName", "Segoe UI");
            var savedFontFamily = AppFont.AllFonts.FirstOrDefault(f => f.Name == savedFontName)!.FontFamily ?? new FontFamily("Segoe UI");
            var savedFontScale = LocalSettingsStorage.LoadOrCreate("AppFontScale", 1.0);
            var savedStartPageKind = LocalSettingsStorage.LoadOrCreate("AppStartPageKind", StartPage.Default);
            var savedIsSize = LocalSettingsStorage.LoadOrCreate("AppIsSize", false);
            var savedIsPosition = LocalSettingsStorage.LoadOrCreate("AppIsPosition", false);
            var kind = LocalSettingsStorage.LoadOrCreate("AppTransparencyType", TransparencyKind.MicaBaseAlt);
            var IsOn = LocalSettingsStorage.LoadOrCreate("WindowMaximizedIsOn", true);
            var root = MainWindow?.Content as FrameworkElement;

            AppWindowSetting.SetWindowPlacement(MainWindow, savedIsSize, savedIsPosition);
            AppWindowSetting.SetMaximize(MainWindow, IsOn);
            AppWindowSetting.SetAlwaysOnTop(MainWindow);
            MainWindow?.ShellPage.SetMainPage(StartPageHelper.CheckStatus(savedStartPageKind));
            FontManager.Instance.FontScale = savedFontScale;
            FontManager.Instance.CurrentFont = savedFontFamily;
            FlowDirectionChanger.SetDirection(savedDirection);
            TitleBarSetColorLogic.SetColors(savedTheme);
            AppWindowSetting.ApplyTransparency(savedTransparency, MainWindow, MainWindow!.Grid, savedTheme, kind);
            AppWindowSetting.SetApplicationTheme(root, savedTheme);
        }

        private static void PreLoaded()
        {
            var settings = new UISettings();
            var systemAccent = settings.GetColorValue(UIColorType.Accent);

            var savedAccent = LocalSettingsStorage.LoadOrCreate("AppAccent", systemAccent);
            AccentPaletteBuilder.ApplyAccentPalette(savedAccent, Current.Resources);
        }
    }
}