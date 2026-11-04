using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PersonalWinUIApp.Tools
{
    public class ModelMediaServices
    {
        private static readonly HttpClient _httpClient = new()
        {
            BaseAddress = new Uri("https://multi-media-hill-6654.alisafari179.workers.dev/")
        };

        public async Task<List<VideoInfoModel>> GetVideoAsync(CancellationToken cancellationToken = default)
        {
            var videos = await _httpClient.GetFromJsonAsync<List<VideoInfoModel>>("api/videos", cancellationToken);
            return videos ?? [];
        }
    }

    public class VideoInfoModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
    }
}
