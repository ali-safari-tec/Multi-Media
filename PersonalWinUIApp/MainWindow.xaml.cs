using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyWinUIUtilities.Application;
using PersonalWinUIApp.Views.Shell;

namespace PersonalWinUIApp
{
    public sealed partial class MainWindow : Window
    {
        public ShellPage ShellPage { get; }
        public TitleBar TitleBar { get; }
        public Grid Grid { get; }
        public AutoSuggestBox SuggestBox { get; }

        public MainWindow()
        {
            InitializeComponent();

            ShellPage = new ShellPage();
            ShellHost.Content = ShellPage;
            TitleBar = TitleApp;
            Grid = MainAppGrid;
            SuggestBox = MainSuggestBox;

            AppWindowSetting.SetCustomTitleBar(this, TitleBar);
            AppWindowSetting.SetTitleBarHeightOption(this, TitleBarHeightOption.Tall);
            AppWindowSetting.SetWindowSizeConstraints(this, 700, 600, 5000, 5000);
        }


        private void AccountPageButton_Click(object sender, RoutedEventArgs e)
        {
            ShellPage.NavigateToAccountPage();
        }

        private void mainWindow_Closed(object sender, WindowEventArgs args)
        {
            AppWindowSetting.SaveWindowPlacement(this);
            AppWindowSetting.SaveWindowState(this);
        }
    }
}
