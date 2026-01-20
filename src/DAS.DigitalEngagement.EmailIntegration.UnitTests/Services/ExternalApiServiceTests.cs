using DAS.DigitalEngagement.Application.Services;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DAS.DigitalEngagement.EmailIntegration.UnitTests.Services
{
    [TestFixture]
    public class ExternalApiServiceTests
    {
        private Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private Mock<IOptions<EShotAPIM>> _configMock;
        private Mock<ILogger<ExternalApiService>> _loggerMock;
        private ExternalApiService _service;

        [SetUp]
        public void SetUp()
        {
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _configMock = new Mock<IOptions<EShotAPIM>>();
            _loggerMock = new Mock<ILogger<ExternalApiService>>();

            var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            var config = new EShotAPIM
            {
                ApiBaseUrl = "https://api.example.com",
                ApiClientId = "test-api-key"
            };
            _configMock.Setup(c => c.Value).Returns(config);

            _service = new ExternalApiService(httpClient, _configMock.Object, _loggerMock.Object);
        }

        [Test]
        public async Task GetDataAsync_ShouldReturnData_WhenResponseIsSuccessful()
        {
            // Arrange
            var endpoint = "test-endpoint";
            var expectedResponse = "response-data";

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(request =>
                        request.Method == HttpMethod.Get &&
                        request.RequestUri.ToString() == "https://api.example.com/test-endpoint" &&
                        request.Headers.Authorization.Scheme == "Token" &&
                        request.Headers.Authorization.Parameter == "test-api-key"),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(expectedResponse)
                });

            // Act
            var result = await _service.GetDataAsync(endpoint);

            // Assert
            Assert.That(result, Is.EqualTo(expectedResponse));
        }

        [Test]
        public void GetDataAsync_ShouldThrowException_WhenResponseIsUnsuccessful()
        {
            // Arrange
            var endpoint = "test-endpoint";

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(request =>
                        request.Method == HttpMethod.Get &&
                        request.RequestUri.ToString() == "https://api.example.com/test-endpoint"),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest
                });

            // Act & Assert
            var ex = Assert.ThrowsAsync<HttpRequestException>(() => _service.GetDataAsync(endpoint));
            Assert.That(ex, Is.Not.Null);
        }

        [Test]
        public async Task PostDataAsync_ShouldReturnData_WhenResponseIsSuccessful()
        {
            // Arrange
            var endpoint = "test-endpoint";
            var requestBody = new { Name = "Test" };
            var expectedResponse = "response-data";

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(request =>
                        request.Method == HttpMethod.Post &&
                        request.RequestUri.ToString() == "https://api.example.com/test-endpoint" &&
                        request.Headers.Authorization.Scheme == "Token" &&
                        request.Headers.Authorization.Parameter == "test-api-key" &&
                        request.Content.ReadAsStringAsync().Result == "{\"Name\":\"Test\"}"),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(expectedResponse)
                });

            // Act
            var result = await _service.PostDataAsync(endpoint, requestBody);

            // Assert
            Assert.That(result, Is.EqualTo(expectedResponse));
        }

        [Test]
        public void PostDataAsync_ShouldThrowException_WhenResponseIsUnsuccessful()
        {
            // Arrange
            var endpoint = "test-endpoint";
            var requestBody = new { Name = "Test" };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(request =>
                        request.Method == HttpMethod.Post &&
                        request.RequestUri.ToString() == "https://api.example.com/test-endpoint" &&
                        request.Headers.Authorization.Scheme == "Token" &&
                        request.Headers.Authorization.Parameter == "test-api-key" &&
                        request.Content.ReadAsStringAsync().Result == "{\"Name\":\"Test\"}"),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest
                });

            // Act & Assert
            var ex = Assert.ThrowsAsync<HttpRequestException>(() => _service.PostDataAsync(endpoint, requestBody));
            Assert.That(ex, Is.Not.Null);
        }
    }
}