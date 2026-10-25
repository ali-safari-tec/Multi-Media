using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using PersonalWinUIApp.Tools;
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

namespace PersonalWinUIApp.Views.Musics;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MusicsPage : Page
{
    private readonly ModelCardServices _musicCardServices = new();

    public MusicsPage()
    {
        InitializeComponent();
        Loaded += AboutUserControl_Loaded;
        Unloaded += AboutUserControl_Unloaded;
    }

    private void AboutUserControl_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Unloaded -= AboutUserControl_Unloaded;
        _musicCardServices.MusicCardViewModels.Clear();
    }

    private async void AboutUserControl_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Loaded -= AboutUserControl_Loaded;

        await _musicCardServices.LoadMusicCard(@"E:\New folder\desktop\music");
    }

    private async void Button_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is MusicCardViewModel vm)
        {
            if (vm.MusicPath is null)
                return;

            // اگر vm.VideoPath = new Uri(file.Path) باشد، این یک مسیر فایل است.
            // بهتر: فایل را از Path بگیر و با Launcher اجرا کن.
            var file = await StorageFile.GetFileFromPathAsync(vm.MusicPath.LocalPath);
            await Launcher.LaunchFileAsync(file);
        }
    }

    private void TextBlock_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Button button && button.FindName("musicTitle") is TextBlock textBlock)
        {
            textBlock.TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap;
        }
    }

    private void TextBlock_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Button button && button.FindName("musicTitle") is TextBlock textBlock)
        {
            textBlock.TextWrapping = Microsoft.UI.Xaml.TextWrapping.NoWrap;
        }
    }
}
