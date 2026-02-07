using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DAS.DigitalEngagement.Application.Services;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace DAS.DigitalEngagement.EmailIntegration.UnitTests.Services
{
    [TestFixture]
    public class ExternalApiServiceTests
    {
        private Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private HttpClient _httpClient;
        private Mock<ILogger<ExternalApiService>> _loggerMock;
        private IOptions<EShotAPIM> _options;

        [SetUp]
        public void SetUp()
        {
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _loggerMock = new Mock<ILogger<ExternalApiService>>();
            _options = Options.Create(new EShotAPIM
            {
                ApiBaseUrl = "https://api.test.com",
                ApiClientId = "test-api-key",
                ApiRetryCount = 3,
                ChunkSizeKB = 1024
            });
        }

        [Test]
        public void Constructor_ThrowsArgumentNullException_WhenApiBaseUrlIsNull()
        {
            var options = Options.Create(new EShotAPIM { ApiBaseUrl = null, ApiClientId = "key", ApiRetryCount = 3, ChunkSizeKB = 1024 });
            Assert.Throws<ArgumentNullException>(() =>
                new ExternalApiService(_httpClient, options, _loggerMock.Object));
        }

        [Test]
        public void Constructor_ThrowsArgumentNullException_WhenApiClientIdIsNull()
        {
            var options = Options.Create(new EShotAPIM { ApiBaseUrl = "url", ApiClientId = null, ApiRetryCount = 3, ChunkSizeKB = 1024 });
            Assert.Throws<ArgumentNullException>(() =>
                new ExternalApiService(_httpClient, options, _loggerMock.Object));
        }

        [Test]
        public async Task GetDataAsync_ReturnsContent_WhenResponseIsSuccess()
        {
            var expectedContent = "response data";
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(expectedContent)
                });

            var service = new ExternalApiService(_httpClient, _options, _loggerMock.Object);

            var result = await service.GetDataAsync("endpoint");

            Assert.That(result, Is.EqualTo(expectedContent));
        }

        [Test]
        public void GetDataAsync_ThrowsException_WhenResponseIsNotSuccess()
        {
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("error")
                });

            var service = new ExternalApiService(_httpClient, _options, _loggerMock.Object);

            Assert.ThrowsAsync<HttpRequestException>(async () =>
                await service.GetDataAsync("endpoint"));
        }

        [Test]
        public async Task PostDataAsync_ReturnsCompletedResult_WhenResponseIsSuccess()
        {
            var expectedContent = "token123";
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(expectedContent)
                });

            var service = new ExternalApiService(_httpClient, _options, _loggerMock.Object);

            var result = await service.PostDataAsync("endpoint", "csv,data");

            Assert.That(result.Status, Is.EqualTo("Completed"));
            Assert.That(result.TokenFromEshot, Is.EqualTo(expectedContent));
            Assert.That(result.Error, Is.Null);
        }

        [Test]
        public async Task PostDataAsync_ReturnsFailedResult_WhenResponseIsNotSuccess()
        {
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("error")
                });

            var service = new ExternalApiService(_httpClient, _options, _loggerMock.Object);

            var result = await service.PostDataAsync("endpoint", "csv,data");

            Assert.That(result.Status, Is.EqualTo("Failed"));
            Assert.That(result.Error, Does.Contain("Failed to post data to"));
            Assert.That(result.Error, Does.Contain("Response status code does not indicate success: 400 (Bad Request)"));
        }

        [Test]
        public async Task PostDataAsync_ReturnsFailedResult_WhenExceptionThrown()
        {
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new Exception("network error"));

            var service = new ExternalApiService(_httpClient, _options, _loggerMock.Object);

            var result = await service.PostDataAsync("endpoint", "csv,data");

            Assert.That(result.Status, Is.EqualTo("Failed"));
            Assert.That(result.Error, Does.Contain("network error"));
        }   
    }
}