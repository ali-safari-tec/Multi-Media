using System;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.FileProperties;

namespace PersonalWinUIApp.Tools
{
    public class VideoCardViewModel
    {
        public string? Title { get; set; }
        public string? Duration { get; set; }
        public BitmapImage? Thumbnail { get; set; }
        public Uri? VideoPath { get; set; }
        public string? FormatIcon { get; set; }
    }
}
