using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.Models.Campaigns;
using DAS.DigitalEngagement.Models.Import;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;

namespace DAS.DigitalEngagement.Application.Services
{
    public class ExternalApiService : IExternalApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl;
        private readonly string _apiKey;
        private readonly ILogger<ExternalApiService> _logger;

        public ExternalApiService(
            HttpClient httpClient,
            IOptions<EmailMarketingApi> config,
            ILogger<ExternalApiService> logger)
        {
            _httpClient = httpClient;
            if (config.Value.ApiBaseUrl == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            if (config.Value.ApiKey == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            _apiUrl = config.Value.ApiBaseUrl;
            _apiKey = config.Value.ApiKey;
            _logger = logger;
        }

        public async Task<string> GetDataAsync(string endpoint)
        {
            var requestUrl = $"{_apiUrl}/{endpoint}";
            _logger.LogInformation("Making GET request to the external email marketing API");

            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Token", _apiKey);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to retrieve data from the external email marketing API. Status Code: {StatusCode}", response.StatusCode);
                response.EnsureSuccessStatusCode();
            }

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetCsvAsync(string endpoint)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiUrl}/{endpoint}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Token", _apiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/csv"));

            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<BatchResultDetail> PostDataAsync(string endpoint, string csvBodyString)
        {
            var result = new BatchResultDetail
            {
                Status = BatchStatus.InProgress,
                Error = null
            };

            var requestUrl = $"{_apiUrl}/{endpoint}";
            _logger.LogInformation("Making POST request to the external email marketing API");

            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Token", _apiKey);

            var bytes = Encoding.UTF8.GetBytes(csvBodyString);
            var bodyContent = new ByteArrayContent(bytes);
            bodyContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            bodyContent.Headers.ContentLength = bytes.Length;

            request.Content = bodyContent;

            try
            {
                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Failed to post data to the external email marketing API. Status Code: {StatusCode}",
                        response.StatusCode);
                    result.Status = BatchStatus.Failed;
                    result.Error = $"Failed to post data to {requestUrl}. Status Code: {response.StatusCode}";
                    response.EnsureSuccessStatusCode();
                }

                var content = await response.Content.ReadAsStringAsync();
                result.TokenFromEshot = content;
                result.Status = BatchStatus.Completed;

                _logger.LogInformation("Received successful response from the external email marketing API");
            }
            catch (Exception ex)
            {
                result.Status = BatchStatus.Failed;
                result.Error = $"Failed to post data to {requestUrl}. Error: {ex.Message}";
                _logger.LogError(ex, "Exception occurred while posting data to the external email marketing API");
            }

            return result;
        }
    }
}
