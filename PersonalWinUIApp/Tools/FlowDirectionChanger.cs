using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace PersonalWinUIApp.Tools
{
    public static class FlowDirectionChanger
    {
        public static void SetDirection(bool isRightToLeft)
        {
            var mainWindow = App.MainWindow;

            if (mainWindow is not null)
            {
                var shell = mainWindow.ShellPage;
                var suggestBox = mainWindow.SuggestBox;

                shell.FlowDirection = isRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
                suggestBox.FlowDirection = isRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            }
        }
    }
}
