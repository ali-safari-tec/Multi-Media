using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using PersonalWinUIApp.Tools;
using PersonalWinUIApp.Views.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PersonalWinUIApp.Views.Videos.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MoviesPage : Page
{
    private readonly ModelCardServices _videoCardServices = new();
    private static ShellPage Shell => App.MainWindow!.ShellPage;

    public MoviesPage()
    {
        InitializeComponent();
        Loaded += AboutUserControl_Loaded;
        Unloaded += AboutUserControl_Unloaded;
    }


    private void AboutUserControl_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Unloaded -= AboutUserControl_Unloaded;
        _videoCardServices.VideoCardViewModels.Clear();
    }

    private async void AboutUserControl_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Loaded -= AboutUserControl_Loaded;

        await _videoCardServices.LoadVideoCard(@"E:\New folder\New folder\backups\personal\learning\wpf");
    }

    private async void Button_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is VideoCardViewModel vm)
        {
            //if (vm.VideoPath is null) return;
            //var file = await StorageFile.GetFileFromPathAsync(vm.VideoPath.LocalPath);
            //await Launcher.LaunchFileAsync(file);
            if (vm.Title is null) return;
            string videoName = vm.Title;
            Shell.VidoeInfoNavigation(videoName);
        }
    }

    private void TextBlock_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Button button && button.FindName("videoTitle") is TextBlock textBlock)
        {
            textBlock.TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap;
        }
    }

    private void TextBlock_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Button button && button.FindName("videoTitle") is TextBlock textBlock)
        {
            textBlock.TextWrapping = Microsoft.UI.Xaml.TextWrapping.NoWrap;
        }
    }
}
