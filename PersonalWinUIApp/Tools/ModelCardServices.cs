using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;

namespace PersonalWinUIApp.Tools
{
    public class ModelCardServices : IDisposable
    {
        public ObservableCollection<VideoCardViewModel> VideoCardViewModels { get; } = [];
        public ObservableCollection<MusicCardViewModel> MusicCardViewModels { get; } = [];
        private static readonly StorageFolder mainFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
        private IReadOnlyList<StorageFile>? _files;

        private async Task GetFolder(string targetFolder)
        {
            var folder = await StorageFolder.GetFolderFromPathAsync(targetFolder);
            _files = await folder.GetFilesAsync();
        }

        public async Task LoadMusicCard(string targetFolder)
        {
            await GetFolder(targetFolder);

            MusicCardViewModels.Clear();
            if (_files is null) return;

            var extensions = new[] { ".mp3", ".m4a", ".aac", ".oga" };

            List<MusicCache> check =
                await MyWinUIUtilities.Application.Settings.LocalFolderStorage
                    .LoadAsync<List<MusicCache>>("music")
                ?? new List<MusicCache>();

            const int batchSize = 8;

            var musicFiles = _files
                .Where(file => extensions.Contains(file.FileType.ToLowerInvariant()))
                .ToList();

            for (int i = 0; i < musicFiles.Count; i += batchSize)
            {
                var batch = musicFiles.Skip(i).Take(batchSize);

                foreach (var file in batch)
                {
                    try
                    {
                        var props = await file.Properties.GetVideoPropertiesAsync();
                        Uri uri = new(file.Path);
                        var musicName = file.DisplayName;
                        var cachedMusic = check.FirstOrDefault(x => x.CachePath == musicName);
                        string? iconPath;
                        var bitmap = new BitmapImage();

                        if (File.Exists(cachedMusic?.Thumbnail))
                        {
                            bitmap.UriSource = new Uri(cachedMusic!.Thumbnail!);
                        }
                        else
                        {
                            var thumbnail = await file.GetThumbnailAsync(
                                ThumbnailMode.MusicView, 318);

                            if (thumbnail != null)
                            {
                                thumbnail.Seek(0);
                                await bitmap.SetSourceAsync(thumbnail);

                                var thumbnailPath = await saveThumbnail(musicName, thumbnail);

                                check.Add(new MusicCache
                                {
                                    CachePath = musicName,
                                    Thumbnail = thumbnailPath
                                });
                            }
                        }

                        iconPath = file.FileType.ToLowerInvariant() switch
                        {
                            ".mp3" => "/Assets/mp4.png",
                            ".m4a" => "/Assets/wmv.png",
                            ".aac" => "/Assets/wmv.png",
                            ".oga" => "/Assets/wmv.png",
                            _ => "/Assets/default.png"
                        };

                        MusicCardViewModels.Add(new MusicCardViewModel
                        {
                            Title = musicName ?? "",
                            Duration = props.Duration.ToString(@"hh\:mm\:ss"),
                            Thumbnail = bitmap,
                            FormatIcon = iconPath,
                            MusicPath = uri
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed: {file.Path} - {ex.Message}");
                    }
                }

                // اجازه بده UI فرصت نمایش دسته فعلی را داشته باشد.
                await Task.Yield();
            }

            await MyWinUIUtilities.Application.Settings.LocalFolderStorage
                .SaveAsync("music", check);
        }

        public async Task LoadVideoCard(string targetFolder)
        {
            await GetFolder(targetFolder);

            VideoCardViewModels.Clear();
            if (_files is null) return;

            var extensions = new[] { ".mp4", ".mkv", ".avi", ".mov", ".avchd", ".webm", ".wmv" };

            List<VideoCache> check =
                await MyWinUIUtilities.Application.Settings.LocalFolderStorage
                    .LoadAsync<List<VideoCache>>("movie")
                ?? new List<VideoCache>();

            const int batchSize = 8;

            // فقط فایل‌های ویدیویی
            var videoFiles = _files
                .Where(file => extensions.Contains(file.FileType.ToLowerInvariant()))
                .ToList();

            for (int i = 0; i < videoFiles.Count; i += batchSize)
            {
                var batch = videoFiles.Skip(i).Take(batchSize);

                foreach (var file in batch)
                {
                    try
                    {
                        var props = await file.Properties.GetVideoPropertiesAsync();
                        Uri uri = new(file.Path);
                        var videoName = file.DisplayName;
                        var cachedVideo = check.FirstOrDefault(x => x.CachePath == videoName);
                        string? iconPath;
                        var bitmap = new BitmapImage();

                        if (File.Exists(cachedVideo?.Thumbnail))
                        {
                            bitmap.UriSource = new Uri(cachedVideo!.Thumbnail!);
                        }
                        else
                        {
                            var thumbnail = await file.GetThumbnailAsync(
                                ThumbnailMode.VideosView, 318);

                            if (thumbnail != null)
                            {
                                thumbnail.Seek(0);
                                await bitmap.SetSourceAsync(thumbnail);

                                var thumbnailPath = await saveThumbnail(videoName, thumbnail);

                                check.Add(new VideoCache
                                {
                                    CachePath = videoName,
                                    Thumbnail = thumbnailPath
                                });
                            }
                        }

                        iconPath = file.FileType.ToLowerInvariant() switch
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
                            Title = videoName ?? "",
                            Duration = props.Duration.ToString(@"hh\:mm\:ss"),
                            Thumbnail = bitmap,
                            FormatIcon = iconPath,
                            VideoPath = uri
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed: {file.Path} - {ex.Message}");
                    }
                }

                // اجازه بده UI فرصت نمایش دسته فعلی را داشته باشد.
                await Task.Yield();
            }

            await MyWinUIUtilities.Application.Settings.LocalFolderStorage
                .SaveAsync("movie", check);
        }


        public void Dispose()
        {
            VideoCardViewModels.Clear();
            MusicCardViewModels.Clear();
            _files = null;
        }

        private async Task<string> saveThumbnail(string videoName, IRandomAccessStream thumbnailStream)
        {
            var thumbnailFolder = await mainFolder.CreateFolderAsync(".thumbnail", CreationCollisionOption.OpenIfExists);
            var fileName = videoName + ".jpg";
            var thumbnailFile = await thumbnailFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

            thumbnailStream.Seek(0);

            using var outputStream = await thumbnailFile.OpenAsync(FileAccessMode.ReadWrite);
            await RandomAccessStream.CopyAsync(thumbnailStream, outputStream);
            await outputStream.FlushAsync();

            return thumbnailFile.Path;
        }
    }

    public class MusicCache
    {
        public string? CachePath { get; set; }
        public string? Thumbnail { get; set; }
    }

    public class VideoCache
    {
        public string? CachePath { get; set; }
        public string? Thumbnail { get; set; }
    }

    public class MusicCardViewModel
    {
        public string? Title { get; set; }
        public string? Duration { get; set; }
        public BitmapImage? Thumbnail { get; set; }
        public Uri? MusicPath { get; set; }
        public string? FormatIcon { get; set; }
    }

    public class VideoCardViewModel
    {
        public string? Title { get; set; }
        public string? Duration { get; set; }
        public BitmapImage? Thumbnail { get; set; }
        public Uri? VideoPath { get; set; }
        public string? FormatIcon { get; set; }
    }
}
