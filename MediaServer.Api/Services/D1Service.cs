using System.Net.Http.Json;
using System.Text.Json;

namespace MediaServer.Api.Services
{
    public class D1Service
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public D1Service(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<JsonDocument> QueryAsync(string sql, object?[]? parameters = null, CancellationToken cancellationToken = default)
        {
            var accountId = _configuration["Cloudflare:AccountId"];
            var databaseId = _configuration["Cloudflare:DatabaseId"];
            var apiToken = _configuration["Cloudflare:ApiToken"];

            if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(databaseId) || string.IsNullOrWhiteSpace(apiToken))
            {
                throw new InvalidOperationException("Cloudflare D1 configuration is incomplete.");
            }

            var url = $"accounts/{accountId}/d1/database/{databaseId}/query";

            using var request = new HttpRequestMessage(HttpMethod.Post, url);

            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiToken);

            request.Content = JsonContent.Create(new
            {
                sql,
                @params = parameters
            });

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonDocument.Parse(responseBody);

            if (!response.IsSuccessStatusCode || !result.RootElement.GetProperty("success").GetBoolean())
            {
                result.Dispose();

                throw new InvalidOperationException($"Cloudflare D1 request failed: {responseBody}");
            }

            return result;
        }
    }
}
