using Microsoft.UI.Xaml;
using MyWinUIUtilities.Application;
using Windows.UI;

namespace PersonalWinUIApp.Tools
{
    public static class TitleBarSetColorLogic
    {
        private static Window MainWindow => App.MainWindow!;
        public static void SetColors(ThemeType theme)
        {
            var systemTheme = AppWindowSetting.GetSystemTheme().ToString();
            var isLight = systemTheme == "Light";

            switch (theme)
            {
                case ThemeType.Light:
                    SetForLight();
                    break;
                case ThemeType.Dark:
                    SetForDark();
                    break;
                case ThemeType.System:
                    if (isLight) SetForLight();
                    else SetForDark();
                    break;
            }
        }

        private static void SetForLight()
        {
            AppWindowSetting.SetTitleBarColors(MainWindow, tb => 
            {
                tb.ButtonForegroundColor = Color.FromArgb(255, 0, 0, 0);
                tb.ButtonHoverBackgroundColor = Color.FromArgb(155, 185, 185, 185);
                tb.ButtonHoverForegroundColor = Color.FromArgb(255, 0, 0, 0);
                tb.ButtonInactiveForegroundColor = Color.FromArgb(205, 155, 155, 155);
                tb.ButtonPressedForegroundColor = Color.FromArgb(255, 105, 105, 105);
                tb.ButtonPressedBackgroundColor = Color.FromArgb(155, 200, 200, 200);
            });
        }

        private static void SetForDark()
        {
            AppWindowSetting.SetTitleBarColors(MainWindow, tb =>
            {
                tb.ButtonForegroundColor = Color.FromArgb(255, 255, 255, 255); 
                tb.ButtonHoverBackgroundColor = Color.FromArgb(155, 50, 50, 50);
                tb.ButtonHoverForegroundColor = Color.FromArgb(255, 255, 255, 255); 
                tb.ButtonInactiveForegroundColor = Color.FromArgb(205, 105, 105, 105);
                tb.ButtonPressedForegroundColor = Color.FromArgb(255, 125, 125, 125); 
                tb.ButtonPressedBackgroundColor = Color.FromArgb(155, 40, 40, 40);
            });
        }
    }
}
