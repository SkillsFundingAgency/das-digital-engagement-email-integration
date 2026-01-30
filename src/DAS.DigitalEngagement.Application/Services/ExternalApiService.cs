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
            if (config.Value.ApiBaseUrl == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            if (config.Value.ApiClientId == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            _apiUrl = config.Value.ApiBaseUrl;
            _apiKey = config.Value.ApiClientId;
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

        public async Task<string> PostDataAsync(string endpoint, Stream csvBodyStream)
        {
            var requestUrl = $"{_apiUrl}/{endpoint}";
            _logger.LogInformation("Making POST request to {RequestUrl}", requestUrl);

            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Token", _apiKey);
            request.Content = new StreamContent(csvBodyStream);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");

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

        public async Task<string> PostDataAsync(string endpoint, string csvBodyString)
        {
            var requestUrl = $"{_apiUrl}/{endpoint}";
            _logger.LogInformation("Making POST request to {RequestUrl}", requestUrl);

            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Token", _apiKey);



            var bytes = Encoding.UTF8.GetBytes(csvBodyString);
            var bodyContent = new ByteArrayContent(bytes);
            bodyContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            bodyContent.Headers.ContentLength = bytes.Length;

            request.Content = bodyContent;



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
