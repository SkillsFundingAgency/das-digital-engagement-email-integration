using DAS.DigitalEngagement.Application.Services.Interfaces;
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
            IOptions<EShotAPIM> config,
            ILogger<ExternalApiService> logger)
        {
            _httpClient = httpClient;
            _apiUrl = config.Value.ApiBaseUrl ?? throw new ArgumentNullException(nameof(config.Value.ApiBaseUrl));
            _apiKey = config.Value.ApiClientId ?? throw new ArgumentNullException(nameof(config.Value.ApiClientId));
            _logger = logger;
        }

        public async Task<string> GetDataAsync(string endpoint)
        {
            var requestUrl = $"{_apiUrl}/{endpoint}";
            _logger.LogInformation("Making GET request to {RequestUrl}", requestUrl);

            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Token", _apiKey);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to retrieve data from {RequestUrl}. Status Code: {StatusCode}",
                    requestUrl,
                    response.StatusCode);
                response.EnsureSuccessStatusCode();
            }

            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Received response: {Content}", content);
        
            return content;
        }

        public async Task<string> PostDataAsync(string endpoint, object body)
        {
            var requestUrl = $"{_apiUrl}/{endpoint}";
            _logger.LogInformation("Making POST request to {RequestUrl}", requestUrl);

            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Token", _apiKey);

            // Serialize the body object to JSON and set it as the content
            var jsonBody = System.Text.Json.JsonSerializer.Serialize(body);
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to post data to {RequestUrl}. Status Code: {StatusCode}",
                    requestUrl,
                    response.StatusCode);
                response.EnsureSuccessStatusCode();
            }

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Received response: {Content}", content);

            return content;
        }
    }
}
