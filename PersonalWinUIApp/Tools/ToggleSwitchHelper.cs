using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace PersonalWinUIApp.Tools
{
    public static class ToggleSwitchHelper
    {
        public static bool GetAutoReverse(DependencyObject obj)
            => (bool)obj.GetValue(AutoReverseProperty);

        public static void SetAutoReverse(DependencyObject obj, bool value)
            => obj.SetValue(AutoReverseProperty, value);

        public static readonly DependencyProperty AutoReverseProperty =
            DependencyProperty.RegisterAttached(
                "AutoReverse",
                typeof(bool),
                typeof(ToggleSwitchHelper),
                new PropertyMetadata(false, OnChanged));

        private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ToggleSwitch toggle)
            {
                var shell = App.MainWindow!.ShellPage as FrameworkElement;

                if (shell != null)
                {
                    toggle.FlowDirection =
                        shell.FlowDirection == FlowDirection.RightToLeft
                        ? FlowDirection.LeftToRight
                        : FlowDirection.RightToLeft;
                }
            }
        }
    }
}
