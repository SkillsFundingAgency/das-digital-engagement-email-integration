using DAS.DigitalEngagement.Application.Services;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.Models.Campaigns;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Microsoft.Extensions.Options;
using DAS.DigitalEngagement.Models.Infrastructure;

namespace DAS.DigitalEngagement.EmailIntegration.UnitTests.Services
{
    [TestFixture]
    public class CampaignServiceTests
    {
        private Mock<IExternalApiService> _externalApiServiceMock;
        private Mock<ILogger<CampaignService>> _loggerMock;
        private Mock<IOptions<EmailMarketingApi>> _apiConfig;
        private CampaignService _sut;

        [SetUp]
        public void SetUp()
        {
            _externalApiServiceMock = new Mock<IExternalApiService>();
            _loggerMock = new Mock<ILogger<CampaignService>>();
            _sut = new CampaignService(_externalApiServiceMock.Object, _loggerMock.Object, _apiConfig.Object);
        }

        [Test]
        public async Task GetSendsForSubAccountAsync_WithValidResponse_ReturnsSends()
        {
            // Arrange
            const int subAccountId = 123;
            var jsonResponse = @"{
                ""value"": [
                    {
                        ""ID"": 1,
                        ""Name"": ""Campaign 1"",
                        ""SendCompleteDate"": ""2024-01-15T10:30:00Z"",
                        ""ContactCount"": 1000
                    },
                    {
                        ""ID"": 2,
                        ""Name"": ""Campaign 2"",
                        ""SendCompleteDate"": ""2024-01-16T14:20:00Z"",
                        ""ContactCount"": 2000
                    }
                ]
            }";

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(jsonResponse);

            // Act
            var result = await _sut.GetAllSendsAsync(subAccountId);

            // Assert
            _externalApiServiceMock.Verify(
                x => x.GetDataAsync(It.Is<string>(endpoint => 
                    endpoint.Contains("Sends/?$filter=") &&
                    endpoint.Contains("SubAccountID%20eq%20123") &&
                    endpoint.Contains("$select=ID,Name,SendCompleteDate,ContactCount"))), 
                Times.Once());
            
            var sendsList = result.ToList();
            Assert.That(sendsList, Is.Not.Null);
            Assert.That(sendsList.Count, Is.EqualTo(2));
            Assert.That(sendsList[0].ID, Is.EqualTo(1));
            Assert.That(sendsList[0].Name, Is.EqualTo("Campaign 1"));
            Assert.That(sendsList[0].ContactCount, Is.EqualTo(1000));
            Assert.That(sendsList[1].ID, Is.EqualTo(2));
            Assert.That(sendsList[1].Name, Is.EqualTo("Campaign 2"));
            Assert.That(sendsList[1].ContactCount, Is.EqualTo(2000));

        }

        [Test]
        public async Task GetSendsForSubAccountAsync_WithEmptyResponse_ReturnsEmptyCollection()
        {
            // Arrange
            const int subAccountId = 123;
            var jsonResponse = @"{ ""value"": [] }";

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(jsonResponse);

            // Act
            var result = await _sut.GetAllSendsAsync(subAccountId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetSendsForSubAccountAsync_WithInvalidSends_SkipsInvalidRecords()
        {
            // Arrange
            const int subAccountId = 123;
            var jsonResponse = @"{
                ""value"": [
                    {
                        ""ID"": 1,
                        ""Name"": ""Campaign 1"",
                        ""SendCompleteDate"": ""2024-01-15T10:30:00Z"",
                        ""ContactCount"": 1000
                    },
                    {
                        ""ID"": 0,
                        ""Name"": ""Invalid Campaign"",
                        ""SendCompleteDate"": null,
                        ""ContactCount"": 500
                    },
                    {
                        ""ID"": 2,
                        ""Name"": ""Campaign 2"",
                        ""SendCompleteDate"": ""2024-01-16T14:20:00Z"",
                        ""ContactCount"": 2000
                    }
                ]
            }";

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(jsonResponse);

            // Act
            var result = await _sut.GetAllSendsAsync(subAccountId);

            // Assert
            var sendsList = result.ToList();
            Assert.That(sendsList.Count, Is.EqualTo(2));
            Assert.That(sendsList, Has.All.Matches<Send>(send => send.ID > 0 && !string.IsNullOrEmpty(send.SendCompletedDate)));
        }

        [Test]
        public async Task GetSendsForSubAccountAsync_WithNullResponse_ThrowsException()
        {
            // Arrange
            const int subAccountId = 123;
            var exception = new Exception("API Error");

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ThrowsAsync(exception);

            // Act & Assert
            Assert.ThrowsAsync<Exception>(() => _sut.GetAllSendsAsync(subAccountId));

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Failed to retrieve Sends for sub-account {subAccountId}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        //[Test]
        //public void GetSendsForSubAccountAsync_WithMalformedJson_ThrowsException()
        //{
        //    // Arrange
        //    const int subAccountId = 123;
        //    var jsonResponse = "{ invalid json }";

        //    _externalApiServiceMock
        //        .Setup(x => x.GetDataAsync(It.IsAny<string>()))
        //        .ReturnsAsync(jsonResponse);

        //    // Act & Assert
        //    Assert.ThrowsAsync<Exception>(() => _sut.GetSendsForSubAccountAsync(subAccountId));
        //}

        [Test]
        public async Task GetSendsForSubAccountAsync_WithValidSubAccountId_CallsGetDataAsyncWithCorrectEndpoint()
        {
            // Arrange
            const int subAccountId = 456;
            var jsonResponse = @"{ ""value"": [] }";

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(jsonResponse);

            // Act
            await _sut.GetAllSendsAsync(subAccountId);

            // Assert
            _externalApiServiceMock.Verify(
                x => x.GetDataAsync(It.Is<string>(endpoint => 
                    endpoint.Contains("Sends/") && 
                    endpoint.Contains("SubAccountID%20eq%20456") &&
                    endpoint.Contains("ID") &&
                    endpoint.Contains("Name") &&
                    endpoint.Contains("SendCompleteDate") &&
                    endpoint.Contains("ContactCount"))),
                Times.Once);
        }

        [Test]
        public async Task GetSendsForSubAccountAsync_WithPartiallyMissingFields_HandlesGracefully()
        {
            // Arrange
            const int subAccountId = 123;
            var jsonResponse = @"{
                ""value"": [
                    {
                        ""ID"": 1,
                        ""SendCompleteDate"": ""2024-01-15T10:30:00Z""
                    },
                    {
                        ""ID"": 2,
                        ""Name"": ""Campaign 2"",
                        ""SendCompleteDate"": ""2024-01-16T14:20:00Z"",
                        ""ContactCount"": 2000
                    }
                ]
            }";

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(jsonResponse);

            // Act
            var result = await _sut.GetAllSendsAsync(subAccountId);

            // Assert
            var sendsList = result.ToList();
            Assert.That(sendsList.Count, Is.EqualTo(2));
            Assert.That(sendsList[0].Name, Is.Null);
            Assert.That(sendsList[0].ContactCount, Is.EqualTo(0));
            Assert.That(sendsList[1].Name, Is.EqualTo("Campaign 2"));
        }

        [Test]
        public async Task GetSendsForSubAccountAsync_LogsInformationMessages()
        {
            // Arrange
            const int subAccountId = 123;
            var jsonResponse = @"{
                ""value"": [
                    {
                        ""ID"": 1,
                        ""Name"": ""Campaign 1"",
                        ""SendCompleteDate"": ""2024-01-15T10:30:00Z"",
                        ""ContactCount"": 1000
                    }
                ]
            }";

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(jsonResponse);

            // Act
            await _sut.GetAllSendsAsync(subAccountId);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Retrieving Sends for sub-account {subAccountId}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Successfully retrieved 1 Sends")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Test]
        public async Task GetSendsForSubAccountAsync_WithCancellationToken_PassesThroughCorrectly()
        {
            // Arrange
            const int subAccountId = 789;
            var jsonResponse = @"{ ""value"": [] }";
            var cancellationToken = System.Threading.CancellationToken.None;

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(jsonResponse);

            // Act
            var result = await _sut.GetAllSendsAsync(subAccountId, cancellationToken);

            // Assert
            Assert.That(result, Is.Not.Null);
            _externalApiServiceMock.Verify(x => x.GetDataAsync(It.IsAny<string>()), Times.Once);
        }
    }
}