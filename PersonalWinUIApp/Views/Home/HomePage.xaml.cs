using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using MyWinUIUtilities.Application.Settings;
using PersonalWinUIApp.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PersonalWinUIApp.Views.Home;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class HomePage : Page
{
    private readonly ModelMediaServices _mediaServices = new();
    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromHours(2)
    };

    public HomePage()
    {
        InitializeComponent();

        string text = "Original";
        int[] nums = { 1, 2, 3 };
        Test(text, nums);
        Debug.WriteLine(text);
        Debug.WriteLine(nums[0]);

    }

    static void Test(string s, int[] arr)
    {
        s = "Changed";
        arr[0] = 99;
    }

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // انتخاب ویدیو از کامپیوتر
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.VideosLibrary
            };

            picker.FileTypeFilter.Add(".mp4");
            picker.FileTypeFilter.Add(".mpg");
            picker.FileTypeFilter.Add(".mpeg");
            picker.FileTypeFilter.Add(".mkv");
            picker.FileTypeFilter.Add(".avi");
            picker.FileTypeFilter.Add(".mov");
            picker.FileTypeFilter.Add(".webm");
            picker.FileTypeFilter.Add(".wmv");

            var hwnd = WindowNative.GetWindowHandle(
                App.MainWindow);

            InitializeWithWindow.Initialize(picker, hwnd);

            StorageFile? file = await picker.PickSingleFileAsync();

            if (file is null)
                return;

            // دریافت کلید برای تست؛ آن را داخل کد ثابت ننویس
            var dialog = new ContentDialog
            {
                Title = "Upload API Key",
                Content = new PasswordBox
                {
                    Name = "UploadKeyBox",
                    PlaceholderText = "کلید آپلود را وارد کن"
                },
                PrimaryButtonText = "آپلود",
                CloseButtonText = "لغو",
                XamlRoot = this.Content.XamlRoot
            };

            var passwordBox = (PasswordBox)dialog.Content;

            var dialogResult = await dialog.ShowAsync();

            if (dialogResult != ContentDialogResult.Primary)
                return;

            string uploadKey = passwordBox.Password;

            if (string.IsNullOrWhiteSpace(uploadKey))
            {
                ali.Text = "کلید آپلود وارد نشده است.";
                return;
            }

            string extension = Path.GetExtension(file.Name);

            string url =
                "https://upload.ali-safari-dev.ir/api/videos/upload" +
                $"?extension={Uri.EscapeDataString(extension)}";

            await using var input = await file.OpenStreamForReadAsync();
            using var content = new StreamContent(input);

            content.Headers.ContentType =
                new MediaTypeHeaderValue("application/octet-stream");

            using var request = new HttpRequestMessage(HttpMethod.Post, url);

            request.Headers.Add("X-Upload-Key", uploadKey);
            request.Content = content;

            ali.Text = $"در حال آپلود {file.Name}...";

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead);

            string responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using var json = JsonDocument.Parse(responseBody);

                string storedName =
                    json.RootElement.GetProperty("fileName").GetString() ?? "";

                ali.Text =
                    $"آپلود موفق بود!\n" +
                    $"نام فایل روی سرور: {storedName}\n";
            }
            else
            {
                ali.Text =
                    $"آپلود ناموفق بود: {(int)response.StatusCode}\n" +
                    responseBody;
            }
        }
        catch (Exception ex)
        {
            ali.Text = ex.ToString();
            System.Diagnostics.Debug.WriteLine(ex.ToString());
        }




        //var videos = await _mediaServices.GetVideoAsync();
        //ali.Text = string.Join("\n", videos.Select(x => $"{x.Name} - {x.SizeBytes:N0} bytes"));



        //PlayVideo("https://www.ali-safari-dev.ir/videos/950cee763e0d4d38ba6e7524a31a793e.mpg");
    }

    private async void Button1_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ali.Text = "در حال دریافت اطلاعات فیلم‌ها...";

            var videos = await _mediaServices.GetVideoAsync();

            if (videos.Count == 0)
            {
                ali.Text = "هیچ فیلمی در دیتابیس ثبت نشده است.";
                return;
            }

            ali.Text = string.Join(
                "\n",
                videos.Select(x =>
                    $"{x.Id} | {x.Name} | {x.SizeBytes:N0} bytes"));
        }
        catch (Exception ex)
        {
            ali.Text = $"خطا در دریافت اطلاعات:\n{ex.Message}";
        }
    }

    private void PlayVideo(string videoUrl)
    {

        ali2.Source =
        MediaSource.CreateFromUri(new Uri(videoUrl));

        ali2.MediaPlayer.Play();

        //if (!Uri.TryCreate(videoUrl, UriKind.Absolute, out Uri? uri))
        //    return;

        //if (uri.Scheme != Uri.UriSchemeHttp &&
        //    uri.Scheme != Uri.UriSchemeHttps)
        //    return;

        //ali2.Source = MediaSource.CreateFromUri(uri);
        //ali2.MediaPlayer.Play();
    }
}
