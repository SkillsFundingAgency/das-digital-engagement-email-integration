using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.Models.Import;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace DAS.DigitalEngagement.Application.Services
{
    public class ExternalApiService : IExternalApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl;
        private readonly string _apiKey;
        private readonly int _apiRetryCount;
        private readonly ILogger<ExternalApiService> _logger;
        private readonly ResiliencePipeline<HttpResponseMessage> _getRetryPipeline;

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
            _apiRetryCount = config.Value.ApiRetryCount > 0 ? config.Value.ApiRetryCount : 3;
            _logger = logger;
            _getRetryPipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
                .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                {
                    MaxRetryAttempts = _apiRetryCount,
                    Delay = TimeSpan.FromMilliseconds(500),
                    BackoffType = DelayBackoffType.Constant,
                    MaxDelay = TimeSpan.FromSeconds(5),
                    ShouldHandle = args => ValueTask.FromResult(
                        args.Outcome.Exception is HttpRequestException ||
                        args.Outcome.Exception is TaskCanceledException &&
                        !args.Context.CancellationToken.IsCancellationRequested ||
                        args.Outcome.Result is { } response && IsTransientStatusCode(response.StatusCode)),
                    OnRetry = args =>
                    {
                        if (args.Outcome.Result is { } response)
                        {
                            _logger.LogWarning(
                                "GET request returned transient status {StatusCode} on attempt {AttemptCount}; retrying.",
                                response.StatusCode,
                                args.AttemptNumber + 1);
                            response.Dispose();
                        }
                        else
                        {
                            _logger.LogWarning(
                                args.Outcome.Exception,
                                "GET request failed on attempt {AttemptCount}; retrying.",
                                args.AttemptNumber + 1);
                        }

                        return default;
                    }
                })
                .Build();
        }

        public Task<string> GetDataAsync(string endpoint) => GetDataAsync(endpoint, CancellationToken.None);

        public async Task<string> GetDataAsync(string endpoint, CancellationToken cancellationToken)
        {
            var requestUrl = $"{_apiUrl}/{endpoint}";
            _logger.LogDebug("Making GET request to {RequestUrl}", requestUrl);

            HttpResponseMessage response;
            try
            {
                response = await _getRetryPipeline.ExecuteAsync(
                    async token =>
                    {
                        using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                        request.Headers.Authorization = new AuthenticationHeaderValue("Token", _apiKey);
                        return await _httpClient.SendAsync(request, token);
                    },
                    cancellationToken);
            }
            catch (Exception exception) when (
                exception is HttpRequestException ||
                exception is TaskCanceledException && !cancellationToken.IsCancellationRequested)
            {
                throw new HttpRequestException(
                    $"GET request to '{requestUrl}' failed after {_apiRetryCount + 1} attempts.",
                    exception);
            }

            using (response)
            {
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Failed to retrieve data from {RequestUrl} after {AttemptCount} attempts. Status Code: {StatusCode}",
                        requestUrl,
                        _apiRetryCount + 1,
                        response.StatusCode);
                    response.EnsureSuccessStatusCode();
                }

                return await response.Content.ReadAsStringAsync(cancellationToken);
            }
        }

        private static bool IsTransientStatusCode(HttpStatusCode statusCode) =>
            statusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests ||
            (int)statusCode >= 500;

        public async Task<BatchResultDetail> PostDataAsync(string endpoint, string csvBodyString)
        {
            var result = new BatchResultDetail
            {
                Status = BatchStatus.InProgress,
                Error = null
            };

            var requestUrl = $"{_apiUrl}/{endpoint}";
            _logger.LogInformation("Making POST request to {RequestUrl}", requestUrl);

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
                        "Failed to post data to {RequestUrl}. Status Code: {StatusCode}",
                        requestUrl,
                        response.StatusCode);
                    result.Status = BatchStatus.Failed;
                    result.Error = $"Failed to post data to {requestUrl}. Status Code: {response.StatusCode}";
                    response.EnsureSuccessStatusCode();
                }

                var content = await response.Content.ReadAsStringAsync();
                result.TokenFromEshot = content;
                result.Status = BatchStatus.Completed;

                _logger.LogInformation("Received response: {Content}", content);
            }
            catch (Exception ex)
            {
                result.Status = BatchStatus.Failed;
                result.Error = $"Failed to post data to {requestUrl}. Error: {ex.Message}";
                _logger.LogError(ex, "Exception occurred while posting data to {RequestUrl}", requestUrl);
            }

            return result;
        }
    }
}
