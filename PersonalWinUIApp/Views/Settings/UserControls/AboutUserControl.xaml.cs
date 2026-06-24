using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using PersonalWinUIApp.Tools;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.System;

namespace PersonalWinUIApp.Views.Settings.UserControls;

public sealed partial class AboutUserControl : UserControl
{
    public ObservableCollection<VideoCardViewModel> VideoCardViewModels { get; private set; } = [];

    public AboutUserControl()
    {
        InitializeComponent();
        _ = LoadVideoCards(@"C:\Users\alisa\OneDrive\Desktop\New folder");
    }

    private async Task LoadVideoCards(string FilePath)
    {
        VideoCardViewModels.Clear();

        var folder = await StorageFolder.GetFolderFromPathAsync(FilePath);
        var files = await folder.GetFilesAsync();
        var extensions = new[] { ".mp4", ".mkv", ".avi", ".mov", "avchd", "webm", "wmv" };

        foreach (var file in files)
        {
            try
            {
                if (extensions.Contains(file.FileType.ToLower()))
                {
                    var props = await file.Properties.GetVideoPropertiesAsync();
                    var thumbnail = await file.GetThumbnailAsync(ThumbnailMode.VideosView, 318);
                    var bitmap = new BitmapImage();
                    await bitmap.SetSourceAsync(thumbnail);
                    Uri uri = new(file.Path);

                    string iconPath = file.FileType.ToLower() switch
                    {
                        ".mp4" => "/Assets/mp4.png",
                        ".mkv" => "/Assets/mkv.png",
                        ".avi" => "/Assets/avi.png",
                        ".mov" => "/Assets/mov.png",
                        ".avchd" => "/Assets/avchd.png",
                        ".webm" => "/Assets/webm.png",
                        ".wmv" => "/Assets/wmv.png",
                        _ => "/Assets/default.png"
                    };

                    VideoCardViewModels.Add(new VideoCardViewModel
                    {
                        Title = file.DisplayName ?? "Empty",
                        Duration = props.Duration.ToString(@"hh\:mm\:ss") ?? "00:00:00",
                        Thumbnail = bitmap,
                        FormatIcon = iconPath,
                        VideoPath = uri
                    });
                }
            }
            catch
            {
                Debug.WriteLine($"Failed: {file.Path}");
            }
        }
    }

    private async void Button_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is VideoCardViewModel vm)
        {
            if (vm.VideoPath is null)
                return;

            // اگر vm.VideoPath = new Uri(file.Path) باشد، این یک مسیر فایل است.
            // بهتر: فایل را از Path بگیر و با Launcher اجرا کن.
            var file = await StorageFile.GetFileFromPathAsync(vm.VideoPath.LocalPath);
            await Launcher.LaunchFileAsync(file);
        }
    }
}
