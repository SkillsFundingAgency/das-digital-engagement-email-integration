using DAS.DigitalEngagement.Application.Services;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.Models.Campaigns;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;   
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

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
            _apiConfig = new Mock<IOptions<EmailMarketingApi>>();
            _apiConfig.Setup(x => x.Value).Returns(new EmailMarketingApi
            {
                PageSize = 5000,  
                ApiBaseUrl = "https://api.eshot.com/api/v1.0",
                ApiKey = "test-api",
                ApiRetryCount = 3,
                ChunkSizeKB = 100,
            });
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
                        ""SendCompletedDate"": ""2024-01-15T10:30:00Z"",
                        ""ContactCount"": 1000
                    },
                    {
                        ""ID"": 2,
                        ""Name"": ""Campaign 2"",
                        ""SendCompletedDate"": ""2024-01-16T14:20:00Z"",
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
                    endpoint.Contains("Sends?$expand=") &&
                    endpoint.Contains("SubAccount") &&
                    endpoint.Contains("Campaign") &&
                    endpoint.Contains("SubAccountID%20eq%20123"))), 
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
                        ""SendCompletedDate"": ""2024-01-15T10:30:00Z"",
                        ""ContactCount"": 1000
                    },
                    {
                        ""ID"": 0,
                        ""Name"": ""Invalid Campaign"",
                        ""SendCompletedDate"": null,
                        ""ContactCount"": 500
                    },
                    {
                        ""ID"": 2,
                        ""Name"": ""Campaign 2"",
                        ""SendCompletedDate"": ""2024-01-16T14:20:00Z"",
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
                    endpoint.Contains("Sends"))),
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
                        ""SendCompletedDate"": ""2024-01-15T10:30:00Z""
                    },
                    {
                        ""ID"": 2,
                        ""Name"": ""Campaign 2"",
                        ""SendCompletedDate"": ""2024-01-16T14:20:00Z"",
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
                        ""SendCompletedDate"": ""2024-01-15T10:30:00Z"",
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

        #region GetUserAgentInfoForSendAsync Tests

        /// <summary>
        /// Tests that GetUserAgentInfoForSendAsync successfully retrieves and returns user agent information
        /// when a single page of data is returned from the API.
        /// </summary>
        [Test]
        public async Task GetUserAgentInfoForSendAsync_SinglePageResponse_ReturnsUserAgentInfo()
        {
            // Arrange
            int sendId = 123;
            var jsonResponse = @"{
                ""value"": [
                    {
                        ""ID"": 1,
                        ""SendID"": 123,
                        ""CampaignID"": 456,
                        ""IPAddress"": ""192.168.1.1"",
                        ""ClientName"": ""Gmail"",
                        ""ClientType"": ""Webmail"",
                        ""ClientFamily"": ""Gmail"",
                        ""Device"": ""Desktop"",
                        ""OperatingSystemFamily"": ""Windows"",
                        ""OperatingSystem"": ""Windows 10""
                    },
                    {
                        ""ID"": 2,
                        ""SendID"": 123,
                        ""CampaignID"": 456,
                        ""IPAddress"": ""192.168.1.2"",
                        ""ClientName"": ""Outlook"",
                        ""ClientType"": ""Desktop"",
                        ""ClientFamily"": ""Outlook"",
                        ""Device"": ""Desktop"",
                        ""OperatingSystemFamily"": ""Windows"",
                        ""OperatingSystem"": ""Windows 11""
                    }
                ]
            }";

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(jsonResponse);

            // Act
            var result = await _sut.GetUserAgentInfoForSendAsync(sendId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(2));
            
            var userAgents = result.ToList();
            Assert.That(userAgents[0].ID, Is.EqualTo(1));
            Assert.That(userAgents[0].ClientName, Is.EqualTo("Gmail"));
            Assert.That(userAgents[0].IPAddress, Is.EqualTo("192.168.1.1"));
            
            Assert.That(userAgents[1].ID, Is.EqualTo(2));
            Assert.That(userAgents[1].ClientName, Is.EqualTo("Outlook"));
            Assert.That(userAgents[1].IPAddress, Is.EqualTo("192.168.1.2"));

            _externalApiServiceMock.Verify(x => x.GetDataAsync(It.IsAny<string>()), Times.Once);
        }

        /// <summary>
        /// Tests that GetUserAgentInfoForSendAsync correctly handles pagination by making multiple
        /// API calls when the returned data equals the page size.
        /// </summary>
        [Test]
        public async Task GetUserAgentInfoForSendAsync_MultiplePages_ReturnsCombinedResults()
        {
            // Arrange
            int sendId = 123;
            int pageSize = 2;

            // First page response (full page, indicates more data)
            var firstPageResponse = @"{
                ""value"": [
                    {
                        ""ID"": 1,
                        ""SendID"": 123,
                        ""CampaignID"": 456,
                        ""IPAddress"": ""192.168.1.1"",
                        ""ClientName"": ""Gmail"",
                        ""ClientType"": ""Webmail"",
                        ""ClientFamily"": ""Gmail"",
                        ""Device"": ""Desktop"",
                        ""OperatingSystemFamily"": ""Windows"",
                        ""OperatingSystem"": ""Windows 10""
                    },
                    {
                        ""ID"": 2,
                        ""SendID"": 123,
                        ""CampaignID"": 456,
                        ""IPAddress"": ""192.168.1.2"",
                        ""ClientName"": ""Outlook"",
                        ""ClientType"": ""Desktop"",
                        ""ClientFamily"": ""Outlook"",
                        ""Device"": ""Desktop"",
                        ""OperatingSystemFamily"": ""Windows"",
                        ""OperatingSystem"": ""Windows 11""
                    }
                ]
            }";

            // Second page response (partial page, indicates end of data)
            var secondPageResponse = @"{
                ""value"": [
                    {
                        ""ID"": 3,
                        ""SendID"": 123,
                        ""CampaignID"": 456,
                        ""IPAddress"": ""192.168.1.3"",
                        ""ClientName"": ""Apple Mail"",
                        ""ClientType"": ""Desktop"",
                        ""ClientFamily"": ""Apple Mail"",
                        ""Device"": ""Mobile"",
                        ""OperatingSystemFamily"": ""iOS"",
                        ""OperatingSystem"": ""iOS 15""
                    }
                ]
            }";

            _externalApiServiceMock
                .SetupSequence(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(firstPageResponse)
                .ReturnsAsync(secondPageResponse);

            var apiConfigSmallPageSize = new Mock<IOptions<EmailMarketingApi>>();
            apiConfigSmallPageSize.Setup(x => x.Value).Returns(new EmailMarketingApi
            {
                PageSize = pageSize,
                ApiBaseUrl = "https://api.eshot.com/api/v1.0",
                ApiKey = "test-api",
                ApiRetryCount = 3,
                ChunkSizeKB = 100,
            });

            var service = new CampaignService(
                _externalApiServiceMock.Object,
                _loggerMock.Object,
                apiConfigSmallPageSize.Object);

            // Act
            var result = await service.GetUserAgentInfoForSendAsync(sendId);

            // Assert
            Assert.That(result.Count(), Is.EqualTo(3));
            var userAgents = result.ToList();
            
            Assert.That(userAgents[0].ID, Is.EqualTo(1));
            Assert.That(userAgents[1].ID, Is.EqualTo(2));
            Assert.That(userAgents[2].ID, Is.EqualTo(3));
            Assert.That(userAgents[2].ClientName, Is.EqualTo("Apple Mail"));

            _externalApiServiceMock.Verify(x => x.GetDataAsync(It.IsAny<string>()), Times.Exactly(2));
        }

        /// <summary>
        /// Tests that GetUserAgentInfoForSendAsync returns an empty collection when the API
        /// response contains no user agent data.
        /// </summary>
        [Test]
        public async Task GetUserAgentInfoForSendAsync_EmptyResponse_ReturnsEmptyCollection()
        {
            // Arrange
            int sendId = 123;
            var emptyResponse = @"{ ""value"": [] }";

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(emptyResponse);

            // Act
            var result = await _sut.GetUserAgentInfoForSendAsync(sendId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(0));

            _externalApiServiceMock.Verify(x => x.GetDataAsync(It.IsAny<string>()), Times.Once);
        }

        /// <summary>
        /// Tests that GetUserAgentInfoForSendAsync correctly deduplicates user agent records
        /// when identical entries are returned from the API.
        /// </summary>
        [Test]
        public async Task GetUserAgentInfoForSendAsync_DuplicateRecords_DeduplicatesResults()
        {
            // Arrange
            int sendId = 123;
            var jsonResponse = @"{
                ""value"": [
                    {
                        ""ID"": 1,
                        ""SendID"": 123,
                        ""CampaignID"": 456,
                        ""IPAddress"": ""192.168.1.1"",
                        ""ClientName"": ""Gmail"",
                        ""ClientType"": ""Webmail"",
                        ""ClientFamily"": ""Gmail"",
                        ""Device"": ""Desktop"",
                        ""OperatingSystemFamily"": ""Windows"",
                        ""OperatingSystem"": ""Windows 10""
                    },
                    {
                        ""ID"": 1,
                        ""SendID"": 123,
                        ""CampaignID"": 456,
                        ""IPAddress"": ""192.168.1.1"",
                        ""ClientName"": ""Gmail"",
                        ""ClientType"": ""Webmail"",
                        ""ClientFamily"": ""Gmail"",
                        ""Device"": ""Desktop"",
                        ""OperatingSystemFamily"": ""Windows"",
                        ""OperatingSystem"": ""Windows 10""
                    }
                ]
            }";

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(jsonResponse);

            // Act
            var result = await _sut.GetUserAgentInfoForSendAsync(sendId);

            // Assert
            Assert.That(result.Count(), Is.EqualTo(1));
            var userAgent = result.First();
            Assert.That(userAgent.ID, Is.EqualTo(1));
            Assert.That(userAgent.ClientName, Is.EqualTo("Gmail"));
        }

        /// <summary>
        /// Tests that GetUserAgentInfoForSendAsync correctly handles user agent records with
        /// null or missing optional properties.
        /// </summary>
        [Test]
        public async Task GetUserAgentInfoForSendAsync_MissingOptionalProperties_ReturnsWithNulls()
        {
            // Arrange
            int sendId = 123;
            var jsonResponse = @"{
                ""value"": [
                    {
                        ""ID"": 1,
                        ""SendID"": 123,
                        ""CampaignID"": 456,
                        ""IPAddress"": null,
                        ""ClientName"": null,
                        ""ClientType"": null,
                        ""ClientFamily"": null,
                        ""Device"": null,
                        ""OperatingSystemFamily"": null,
                        ""OperatingSystem"": null
                    }
                ]
            }";

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(jsonResponse);

            // Act
            var result = await _sut.GetUserAgentInfoForSendAsync(sendId);

            // Assert
            Assert.That(result.Count(), Is.EqualTo(1));
            var userAgent = result.First();
            Assert.That(userAgent.ID, Is.EqualTo(1));
            Assert.That(userAgent.IPAddress, Is.Null);
            Assert.That(userAgent.ClientName, Is.Null);
            Assert.That(userAgent.Device, Is.Null);
        }

        /// <summary>
        /// Tests that GetUserAgentInfoForSendAsync throws an exception when the external API
        /// service fails and properly logs the error.
        /// </summary>
        [Test]
        public void GetUserAgentInfoForSendAsync_ApiServiceThrowsException_PropagatesException()
        {
            // Arrange
            int sendId = 123;
            var exceptionMessage = "API service error";

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException(exceptionMessage));

            // Act & Assert
            var ex = Assert.ThrowsAsync<HttpRequestException>(
                async () => await _sut.GetUserAgentInfoForSendAsync(sendId));

            Assert.That(ex.Message, Contains.Substring(exceptionMessage));
        }

        /// <summary>
        /// Tests that GetUserAgentInfoForSendAsync constructs the correct endpoint URL with
        /// proper pagination parameters.
        /// </summary>
        [Test]
        public async Task GetUserAgentInfoForSendAsync_CorrectEndpointFormat_ConstructsProperUrl()
        {
            // Arrange
            int sendId = 123;
            var jsonResponse = @"{ ""value"": [] }";

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(jsonResponse);

            // Act
            await _sut.GetUserAgentInfoForSendAsync(sendId);

            // Assert - Verify the endpoint contains the correct format
            // Note: Uri.EscapeDataString encodes spaces as %20 in the filter parameter
            _externalApiServiceMock.Verify(
                x => x.GetDataAsync(It.Is<string>(url =>
                    url.Contains("UserAgents") &&
                    url.Contains($"SendID%20eq%20{sendId}") &&
                    url.Contains("$skip=0") &&
                    url.Contains($"$top={_apiConfig.Object.Value.PageSize}"))),
                Times.Once);
        }

        /// <summary>
        /// Tests that GetUserAgentInfoForSendAsync respects the CancellationToken parameter
        /// and propagates cancellation when requested.
        /// </summary>
        [Test]
        public void GetUserAgentInfoForSendAsync_WithCancellationToken_CancelsProperly()
        {
            // Arrange
            int sendId = 123;
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            Assert.ThrowsAsync<OperationCanceledException>(
                async () => await _sut.GetUserAgentInfoForSendAsync(
                    sendId,
                    cancellationTokenSource.Token));
        }

        /// <summary>
        /// Tests that GetUserAgentInfoForSendAsync correctly logs information about retrieval
        /// progress during pagination.
        /// </summary>
        [Test]
        public async Task GetUserAgentInfoForSendAsync_LogsRetrievalInformation()
        {
            // Arrange
            int sendId = 123;
            var jsonResponse = @"{
                ""value"": [
                    {
                        ""ID"": 1,
                        ""SendID"": 123,
                        ""CampaignID"": 456,
                        ""IPAddress"": ""192.168.1.1"",
                        ""ClientName"": ""Gmail"",
                        ""ClientType"": ""Webmail"",
                        ""ClientFamily"": ""Gmail"",
                        ""Device"": ""Desktop"",
                        ""OperatingSystemFamily"": ""Windows"",
                        ""OperatingSystem"": ""Windows 10""
                    }
                ]
            }";

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(jsonResponse);

            // Act
            await _sut.GetUserAgentInfoForSendAsync(sendId);

            // Assert - Verify logging calls occurred
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Retrieving user agent information for Send {sendId}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        /// <summary>
        /// Tests that GetUserAgentInfoForSendAsync handles invalid JSON response gracefully
        /// by throwing an appropriate exception.
        /// </summary>
        [Test]
        public void GetUserAgentInfoForSendAsync_InvalidJsonResponse_ThrowsException()
        {
            // Arrange
            int sendId = 123;
            var invalidJsonResponse = "{ invalid json }";

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(invalidJsonResponse);

            // Act & Assert
            Assert.That(async () => await _sut.GetUserAgentInfoForSendAsync(sendId),
                Throws.InstanceOf<JsonException>());
        }

        /// <summary>
        /// Tests that GetUserAgentInfoForSendAsync correctly processes all properties of the
        /// UserAgentInfo model including optional fields.
        /// </summary>
        [Test]
        public async Task GetUserAgentInfoForSendAsync_AllPropertiesPopulated_MapsCorrectly()
        {
            // Arrange
            int sendId = 123;
            var jsonResponse = @"{
                ""value"": [
                    {
                        ""ID"": 999,
                        ""SendID"": 123,
                        ""CampaignID"": 456,
                        ""IPAddress"": ""10.0.0.1"",
                        ""ClientName"": ""Mozilla Thunderbird"",
                        ""ClientType"": ""Desktop Client"",
                        ""ClientFamily"": ""Thunderbird"",
                        ""Device"": ""Laptop"",
                        ""OperatingSystemFamily"": ""Linux"",
                        ""OperatingSystem"": ""Ubuntu 20.04""
                    }
                ]
            }";

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(jsonResponse);

            // Act
            var result = await _sut.GetUserAgentInfoForSendAsync(sendId);

            // Assert
            var userAgent = result.First();
            Assert.That(userAgent.ID, Is.EqualTo(999));
            Assert.That(userAgent.SendID, Is.EqualTo(123));
            Assert.That(userAgent.CampaignID, Is.EqualTo(456));
            Assert.That(userAgent.IPAddress, Is.EqualTo("10.0.0.1"));
            Assert.That(userAgent.ClientName, Is.EqualTo("Mozilla Thunderbird"));
            Assert.That(userAgent.ClientType, Is.EqualTo("Desktop Client"));
            Assert.That(userAgent.ClientFamily, Is.EqualTo("Thunderbird"));
            Assert.That(userAgent.Device, Is.EqualTo("Laptop"));
            Assert.That(userAgent.OperatingSystemFamily, Is.EqualTo("Linux"));
            Assert.That(userAgent.OperatingSystem, Is.EqualTo("Ubuntu 20.04"));
        }

        #endregion

        #region GetAllSendsAsync without SubAccountId Tests

        [Test]
        public async Task GetAllSendsAsync_WithNullSubAccountId_DoesNotIncludeFilterInEndpoint()
        {
            // Arrange
            var jsonResponse = @"{ ""value"": [] }";

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(jsonResponse);

            // Act
            await _sut.GetAllSendsAsync(null);

            // Assert
            _externalApiServiceMock.Verify(
                x => x.GetDataAsync(It.Is<string>(endpoint =>
                    endpoint.Contains("Sends?$expand=") &&
                    !endpoint.Contains("$filter"))),
                Times.Once);
        }

        [Test]
        public async Task GetAllSendsAsync_WithNoSubAccountId_DefaultsToNull()
        {
            // Arrange
            var jsonResponse = @"{ ""value"": [] }";

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(jsonResponse);

            // Act
            await _sut.GetAllSendsAsync();

            // Assert
            _externalApiServiceMock.Verify(
                x => x.GetDataAsync(It.Is<string>(endpoint =>
                    !endpoint.Contains("$filter"))),
                Times.Once);
        }

        [Test]
        public async Task GetAllSendsAsync_MapsAllSendProperties()
        {
            // Arrange
            var jsonResponse = @"{
                ""value"": [
                    {
                        ""ID"": 10,
                        ""Name"": ""My Campaign"",
                        ""CampaignID"": 20,
                        ""Status"": ""Completed"",
                        ""SubStatus"": ""Delivered"",
                        ""SendDate"": ""2024-01-10T08:00:00Z"",
                        ""SendCompletedDate"": ""2024-01-10T09:00:00Z"",
                        ""CampaignType"": ""Standard"",
                        ""ContactCount"": 500,
                        ""CreatedBy"": ""admin"",
                        ""CreatedDate"": ""2024-01-01T00:00:00Z"",
                        ""Campaign"": {
                            ""FirstSendDate"": ""2024-01-10T08:00:00Z"",
                            ""LastSendDate"": ""2024-01-10T09:00:00Z""
                        },
                        ""FromEmail"": ""noreply@test.com"",
                        ""FromName"": ""Test Sender"",
                        ""ReplyEmail"": ""reply@test.com"",
                        ""SubjectLine"": ""Test Subject"",
                        ""Subaccount"": { ""Name"": ""Sub1"" }
                    }
                ]
            }";

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(jsonResponse);

            // Act
            var result = (await _sut.GetAllSendsAsync()).ToList();

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            var send = result[0];
            Assert.That(send.ID, Is.EqualTo(10));
            Assert.That(send.Name, Is.EqualTo("My Campaign"));
            Assert.That(send.CampaignID, Is.EqualTo(20));
            Assert.That(send.Status, Is.EqualTo("Completed"));
            Assert.That(send.SubStatus, Is.EqualTo("Delivered"));
            Assert.That(send.SendDate, Is.EqualTo("2024-01-10T08:00:00Z"));
            Assert.That(send.SendCompletedDate, Is.EqualTo("2024-01-10T09:00:00Z"));
            Assert.That(send.CampaignType, Is.EqualTo("Standard"));
            Assert.That(send.ContactCount, Is.EqualTo(500));
            Assert.That(send.CreatedBy, Is.EqualTo("admin"));
            Assert.That(send.CreatedDate, Is.EqualTo("2024-01-01T00:00:00Z"));
            Assert.That(send.FirstSendDate, Is.EqualTo("2024-01-10T08:00:00Z"));
            Assert.That(send.LastSendDate, Is.EqualTo("2024-01-10T09:00:00Z"));
            Assert.That(send.FromEmail, Is.EqualTo("noreply@test.com"));
            Assert.That(send.FromName, Is.EqualTo("Test Sender"));
            Assert.That(send.ReplyEmail, Is.EqualTo("reply@test.com"));
            Assert.That(send.SubjectLine, Is.EqualTo("Test Subject"));
            Assert.That(send.Account, Is.EqualTo("Sub1"));
        }

        #endregion

        #region GetDisplayedContactsForSendAsync Tests

        [Test]
        public async Task GetDisplayedContactsForSendAsync_WithValidResponse_ReturnsDisplayedContacts()
        {
            // Arrange
            int sendId = 100;
            var userAgentInfos = new List<UserAgentInfo>
            {
                new UserAgentInfo { ID = 50, SendID = 100, CampaignID = 200, Device = "Desktop", ClientName = "Gmail", OperatingSystem = "Windows 10", OperatingSystemFamily = "Windows", IPAddress = "10.0.0.1", ClientType = "Webmail", ClientFamily = "Gmail" }
            };

            var jsonResponse = @"{
                ""value"": [
                    {
                        ""ID"": 1,
                        ""DisplayDate"": ""2024-02-01T12:00:00Z"",
                        ""Contact"": { ""Email"": ""user@test.com"" },
                        ""Format"": ""HTML"",
                        ""SendID"": 100,
                        ""CampaignID"": 200,
                        ""TimeInSecondsSpentReadingEmail"": 30,
                        ""IsSuspectedBOT"": false,
                        ""UserAgentID"": 50
                    }
                ]
            }";

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(jsonResponse);

            // Act
            var result = (await _sut.GetDisplayedContactsForSendAsync(sendId, userAgentInfos)).ToList();

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].ID, Is.EqualTo(1));
            Assert.That(result[0].DisplayDate, Is.EqualTo("2024-02-01T12:00:00Z"));
            Assert.That(result[0].ContactEmail, Is.EqualTo("user@test.com"));
            Assert.That(result[0].Format, Is.EqualTo("HTML"));
            Assert.That(result[0].SendID, Is.EqualTo(100));
            Assert.That(result[0].CampaignID, Is.EqualTo(200));
            Assert.That(result[0].TimeInSecondsSpentReadingEmail, Is.EqualTo(30));
            Assert.That(result[0].IsSuspectedBOT, Is.False);
            Assert.That(result[0].Device, Is.EqualTo("Desktop"));
            Assert.That(result[0].ClientName, Is.EqualTo("Gmail"));
            Assert.That(result[0].OperatingSystem, Is.EqualTo("Windows 10"));
            Assert.That(result[0].IPAddress, Is.EqualTo("10.0.0.1"));
        }

        [Test]
        public async Task GetDisplayedContactsForSendAsync_EmptyResponse_ReturnsEmptyCollection()
        {
            // Arrange
            var jsonResponse = @"{ ""value"": [] }";

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(jsonResponse);

            // Act
            var result = await _sut.GetDisplayedContactsForSendAsync(100, Enumerable.Empty<UserAgentInfo>());

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetDisplayedContactsForSendAsync_InvalidRecords_SkipsInvalidEntries()
        {
            // Arrange
            var jsonResponse = @"{
                ""value"": [
                    {
                        ""ID"": 0,
                        ""DisplayDate"": ""2024-02-01T12:00:00Z"",
                        ""SendID"": 100,
                        ""CampaignID"": 200
                    },
                    {
                        ""ID"": 2,
                        ""DisplayDate"": null,
                        ""SendID"": 100,
                        ""CampaignID"": 200
                    },
                    {
                        ""ID"": 3,
                        ""DisplayDate"": ""2024-02-01T13:00:00Z"",
                        ""SendID"": 100,
                        ""CampaignID"": 200
                    }
                ]
            }";

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(jsonResponse);

            // Act
            var result = (await _sut.GetDisplayedContactsForSendAsync(100, Enumerable.Empty<UserAgentInfo>())).ToList();

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].ID, Is.EqualTo(3));
        }

        [Test]
        public async Task GetDisplayedContactsForSendAsync_WithNoMatchingUserAgent_LeavesUserAgentFieldsNull()
        {
            // Arrange
            var userAgentInfos = new List<UserAgentInfo>
            {
                new UserAgentInfo { ID = 999, Device = "Mobile" }
            };

            var jsonResponse = @"{
                ""value"": [
                    {
                        ""ID"": 1,
                        ""DisplayDate"": ""2024-02-01T12:00:00Z"",
                        ""SendID"": 100,
                        ""CampaignID"": 200,
                        ""UserAgentID"": 888
                    }
                ]
            }";

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(jsonResponse);

            // Act
            var result = (await _sut.GetDisplayedContactsForSendAsync(100, userAgentInfos)).ToList();

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Device, Is.Null);
            Assert.That(result[0].ClientName, Is.Null);
        }

        [Test]
        public async Task GetDisplayedContactsForSendAsync_WithZeroUserAgentId_LeavesUserAgentFieldsNull()
        {
            // Arrange
            var jsonResponse = @"{
                ""value"": [
                    {
                        ""ID"": 1,
                        ""DisplayDate"": ""2024-02-01T12:00:00Z"",
                        ""SendID"": 100,
                        ""CampaignID"": 200,
                        ""UserAgentID"": 0
                    }
                ]
            }";

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(jsonResponse);

            // Act
            var result = (await _sut.GetDisplayedContactsForSendAsync(100, Enumerable.Empty<UserAgentInfo>())).ToList();

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Device, Is.Null);
        }

        [Test]
        public void GetDisplayedContactsForSendAsync_ApiThrowsException_PropagatesAndLogsError()
        {
            // Arrange
            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException("API error"));

            // Act & Assert
            Assert.ThrowsAsync<HttpRequestException>(
                async () => await _sut.GetDisplayedContactsForSendAsync(100, Enumerable.Empty<UserAgentInfo>()));

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to retrieve displayed contacts for Send 100")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Test]
        public async Task GetDisplayedContactsForSendAsync_MultiplePages_ReturnsCombinedResults()
        {
            // Arrange
            int pageSize = 1;
            var firstPage = @"{
                ""value"": [
                    { ""ID"": 1, ""DisplayDate"": ""2024-02-01T12:00:00Z"", ""SendID"": 100, ""CampaignID"": 200 }
                ]
            }";
            var secondPage = @"{ ""value"": [] }";

            _externalApiServiceMock
                .SetupSequence(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(firstPage)
                .ReturnsAsync(secondPage);

            var apiConfigSmall = new Mock<IOptions<EmailMarketingApi>>();
            apiConfigSmall.Setup(x => x.Value).Returns(new EmailMarketingApi
            {
                PageSize = pageSize,
                ApiBaseUrl = "https://api.eshot.com/api/v1.0",
                ApiKey = "test-api",
                ApiRetryCount = 3,
                ChunkSizeKB = 100,
            });

            var service = new CampaignService(
                _externalApiServiceMock.Object,
                _loggerMock.Object,
                apiConfigSmall.Object);

            // Act
            var result = (await service.GetDisplayedContactsForSendAsync(100, Enumerable.Empty<UserAgentInfo>())).ToList();

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            _externalApiServiceMock.Verify(x => x.GetDataAsync(It.IsAny<string>()), Times.Exactly(2));
        }

        [Test]
        public async Task GetDisplayedContactsForSendAsync_CorrectEndpointFormat()
        {
            // Arrange
            int sendId = 555;
            var jsonResponse = @"{ ""value"": [] }";

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(jsonResponse);

            // Act
            await _sut.GetDisplayedContactsForSendAsync(sendId, Enumerable.Empty<UserAgentInfo>());

            // Assert
            _externalApiServiceMock.Verify(
                x => x.GetDataAsync(It.Is<string>(url =>
                    url.Contains("DisplayedContacts") &&
                    url.Contains($"SendID%20eq%20{sendId}") &&
                    url.Contains("$skip=0") &&
                    url.Contains($"$top={_apiConfig.Object.Value.PageSize}"))),
                Times.Once);
        }

        #endregion

        #region GetClickedLinkContactsForSendAsync Tests

        [Test]
        public async Task GetClickedLinkContactsForSendAsync_WithValidResponse_ReturnsClickedLinkContacts()
        {
            // Arrange
            int sendId = 100;
            var userAgentInfos = new List<UserAgentInfo>
            {
                new UserAgentInfo { ID = 50, Device = "Desktop", ClientName = "Chrome", OperatingSystem = "Windows 11", OperatingSystemFamily = "Windows", IPAddress = "10.0.0.1", ClientType = "Browser", ClientFamily = "Chrome" }
            };

            var jsonResponse = @"{
                ""value"": [
                    {
                        ""ID"": 1,
                        ""ClickDate"": ""2024-02-01T12:30:00Z"",
                        ""Contact"": { ""Email"": ""clicker@test.com"" },
                        ""SendID"": 100,
                        ""CampaignID"": 200,
                        ""FriendlyName"": ""CTA Button"",
                        ""LinkID"": 555,
                        ""Link"": {
                            ""URL"": ""https://example.com/landing"",
                            ""IsMonitored"": true,
                            ""ReceivedInMessageFormat"": ""HTML""
                        },
                        ""IsSuspectedBOT"": false,
                        ""UserAgentID"": 50
                    }
                ]
            }";

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(jsonResponse);

            // Act
            var result = (await _sut.GetClickedLinkContactsForSendAsync(sendId, userAgentInfos)).ToList();

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].ID, Is.EqualTo(1));
            Assert.That(result[0].ClickedDate, Is.EqualTo("2024-02-01T12:30:00Z"));
            Assert.That(result[0].ContactEmail, Is.EqualTo("clicker@test.com"));
            Assert.That(result[0].SendID, Is.EqualTo(100));
            Assert.That(result[0].CampaignID, Is.EqualTo(200));
            Assert.That(result[0].FriendlyName, Is.EqualTo("CTA Button"));
            Assert.That(result[0].LinkID, Is.EqualTo(555));
            Assert.That(result[0].URL, Is.EqualTo("https://example.com/landing"));
            Assert.That(result[0].IsMonitored, Is.True);
            Assert.That(result[0].ReceivedInMessageFormat, Is.EqualTo("HTML"));
            Assert.That(result[0].IsSuspectedBOT, Is.False);
            Assert.That(result[0].Device, Is.EqualTo("Desktop"));
            Assert.That(result[0].ClientName, Is.EqualTo("Chrome"));
            Assert.That(result[0].IPAddress, Is.EqualTo("10.0.0.1"));
        }

        [Test]
        public async Task GetClickedLinkContactsForSendAsync_EmptyResponse_ReturnsEmptyCollection()
        {
            // Arrange
            var jsonResponse = @"{ ""value"": [] }";

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(jsonResponse);

            // Act
            var result = await _sut.GetClickedLinkContactsForSendAsync(100, Enumerable.Empty<UserAgentInfo>());

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetClickedLinkContactsForSendAsync_InvalidRecords_SkipsInvalidEntries()
        {
            // Arrange
            var jsonResponse = @"{
                ""value"": [
                    {
                        ""ID"": 0,
                        ""ClickDate"": ""2024-02-01T12:30:00Z"",
                        ""SendID"": 100,
                        ""CampaignID"": 200
                    },
                    {
                        ""ID"": 2,
                        ""ClickDate"": null,
                        ""SendID"": 100,
                        ""CampaignID"": 200
                    },
                    {
                        ""ID"": 3,
                        ""ClickDate"": ""2024-02-01T13:30:00Z"",
                        ""SendID"": 100,
                        ""CampaignID"": 200
                    }
                ]
            }";

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(jsonResponse);

            // Act
            var result = (await _sut.GetClickedLinkContactsForSendAsync(100, Enumerable.Empty<UserAgentInfo>())).ToList();

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].ID, Is.EqualTo(3));
        }

        [Test]
        public async Task GetClickedLinkContactsForSendAsync_WithNoMatchingUserAgent_LeavesUserAgentFieldsNull()
        {
            // Arrange
            var jsonResponse = @"{
                ""value"": [
                    {
                        ""ID"": 1,
                        ""ClickDate"": ""2024-02-01T12:30:00Z"",
                        ""SendID"": 100,
                        ""CampaignID"": 200,
                        ""UserAgentID"": 999
                    }
                ]
            }";

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(jsonResponse);

            // Act
            var result = (await _sut.GetClickedLinkContactsForSendAsync(100, Enumerable.Empty<UserAgentInfo>())).ToList();

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Device, Is.Null);
            Assert.That(result[0].ClientName, Is.Null);
        }

        [Test]
        public void GetClickedLinkContactsForSendAsync_ApiThrowsException_PropagatesAndLogsError()
        {
            // Arrange
            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException("API error"));

            // Act & Assert
            Assert.ThrowsAsync<HttpRequestException>(
                async () => await _sut.GetClickedLinkContactsForSendAsync(100, Enumerable.Empty<UserAgentInfo>()));

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to retrieve clicked link contacts for Send 100")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Test]
        public async Task GetClickedLinkContactsForSendAsync_MultiplePages_ReturnsCombinedResults()
        {
            // Arrange
            int pageSize = 1;
            var firstPage = @"{
                ""value"": [
                    { ""ID"": 1, ""ClickDate"": ""2024-02-01T12:30:00Z"", ""SendID"": 100, ""CampaignID"": 200 }
                ]
            }";
            var secondPage = @"{ ""value"": [] }";

            _externalApiServiceMock
                .SetupSequence(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(firstPage)
                .ReturnsAsync(secondPage);

            var apiConfigSmall = new Mock<IOptions<EmailMarketingApi>>();
            apiConfigSmall.Setup(x => x.Value).Returns(new EmailMarketingApi
            {
                PageSize = pageSize,
                ApiBaseUrl = "https://api.eshot.com/api/v1.0",
                ApiKey = "test-api",
                ApiRetryCount = 3,
                ChunkSizeKB = 100,
            });

            var service = new CampaignService(
                _externalApiServiceMock.Object,
                _loggerMock.Object,
                apiConfigSmall.Object);

            // Act
            var result = (await service.GetClickedLinkContactsForSendAsync(100, Enumerable.Empty<UserAgentInfo>())).ToList();

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            _externalApiServiceMock.Verify(x => x.GetDataAsync(It.IsAny<string>()), Times.Exactly(2));
        }

        [Test]
        public async Task GetClickedLinkContactsForSendAsync_CorrectEndpointFormat()
        {
            // Arrange
            int sendId = 555;
            var jsonResponse = @"{ ""value"": [] }";

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(jsonResponse);

            // Act
            await _sut.GetClickedLinkContactsForSendAsync(sendId, Enumerable.Empty<UserAgentInfo>());

            // Assert
            _externalApiServiceMock.Verify(
                x => x.GetDataAsync(It.Is<string>(url =>
                    url.Contains("ClickedContacts") &&
                    url.Contains($"SendID%20eq%20{sendId}") &&
                    url.Contains("$skip=0") &&
                    url.Contains($"$top={_apiConfig.Object.Value.PageSize}"))),
                Times.Once);
        }

        #endregion

        #region GetBouncedEmailContactsForSendAsync Tests

        [Test]
        public async Task GetBouncedEmailContactsForSendAsync_WithValidResponse_ReturnsBouncedContacts()
        {
            // Arrange
            int sendId = 100;
            var jsonResponse = @"{
                ""value"": [
                    {
                        ""ID"": 1,
                        ""BounceReason"": ""Mailbox full"",
                        ""BounceType"": ""Soft"",
                        ""BounceDate"": ""2024-02-01T12:00:00Z"",
                        ""Contact"": { ""Email"": ""bounced@test.com"" },
                        ""SendID"": 100,
                        ""CampaignID"": 200,
                        ""ResponseText"": ""452 4.2.2 Mailbox full""
                    }
                ]
            }";

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(jsonResponse);

            // Act
            var result = (await _sut.GetBouncedEmailContactsForSendAsync(sendId)).ToList();

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].ID, Is.EqualTo(1));
            Assert.That(result[0].BounceReason, Is.EqualTo("Mailbox full"));
            Assert.That(result[0].BounceType, Is.EqualTo("Soft"));
            Assert.That(result[0].BounceDate, Is.EqualTo("2024-02-01T12:00:00Z"));
            Assert.That(result[0].ContactEmail, Is.EqualTo("bounced@test.com"));
            Assert.That(result[0].SendID, Is.EqualTo(100));
            Assert.That(result[0].CampaignID, Is.EqualTo(200));
            Assert.That(result[0].ResponseText, Is.EqualTo("452 4.2.2 Mailbox full"));
        }

        [Test]
        public async Task GetBouncedEmailContactsForSendAsync_EmptyResponse_ReturnsEmptyCollection()
        {
            // Arrange
            var jsonResponse = @"{ ""value"": [] }";

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(jsonResponse);

            // Act
            var result = await _sut.GetBouncedEmailContactsForSendAsync(100);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetBouncedEmailContactsForSendAsync_InvalidRecords_SkipsInvalidEntries()
        {
            // Arrange
            var jsonResponse = @"{
                ""value"": [
                    {
                        ""ID"": 0,
                        ""BounceDate"": ""2024-02-01T12:00:00Z"",
                        ""SendID"": 100,
                        ""CampaignID"": 200
                    },
                    {
                        ""ID"": 2,
                        ""BounceDate"": null,
                        ""SendID"": 100,
                        ""CampaignID"": 200
                    },
                    {
                        ""ID"": 3,
                        ""BounceDate"": ""2024-02-01T13:00:00Z"",
                        ""BounceReason"": ""Invalid address"",
                        ""BounceType"": ""Hard"",
                        ""SendID"": 100,
                        ""CampaignID"": 200
                    }
                ]
            }";

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(jsonResponse);

            // Act
            var result = (await _sut.GetBouncedEmailContactsForSendAsync(100)).ToList();

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].ID, Is.EqualTo(3));
            Assert.That(result[0].BounceType, Is.EqualTo("Hard"));
        }

        [Test]
        public void GetBouncedEmailContactsForSendAsync_ApiThrowsException_PropagatesAndLogsError()
        {
            // Arrange
            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException("API error"));

            // Act & Assert
            Assert.ThrowsAsync<HttpRequestException>(
                async () => await _sut.GetBouncedEmailContactsForSendAsync(100));

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to retrieve bounced email contacts for Send 100")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Test]
        public async Task GetBouncedEmailContactsForSendAsync_MultiplePages_ReturnsCombinedResults()
        {
            // Arrange
            int pageSize = 1;
            var firstPage = @"{
                ""value"": [
                    { ""ID"": 1, ""BounceDate"": ""2024-02-01T12:00:00Z"", ""SendID"": 100, ""CampaignID"": 200 }
                ]
            }";
            var secondPage = @"{ ""value"": [] }";

            _externalApiServiceMock
                .SetupSequence(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(firstPage)
                .ReturnsAsync(secondPage);

            var apiConfigSmall = new Mock<IOptions<EmailMarketingApi>>();
            apiConfigSmall.Setup(x => x.Value).Returns(new EmailMarketingApi
            {
                PageSize = pageSize,
                ApiBaseUrl = "https://api.eshot.com/api/v1.0",
                ApiKey = "test-api",
                ApiRetryCount = 3,
                ChunkSizeKB = 100,
            });

            var service = new CampaignService(
                _externalApiServiceMock.Object,
                _loggerMock.Object,
                apiConfigSmall.Object);

            // Act
            var result = (await service.GetBouncedEmailContactsForSendAsync(100)).ToList();

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            _externalApiServiceMock.Verify(x => x.GetDataAsync(It.IsAny<string>()), Times.Exactly(2));
        }

        [Test]
        public async Task GetBouncedEmailContactsForSendAsync_CorrectEndpointFormat()
        {
            // Arrange
            int sendId = 555;
            var jsonResponse = @"{ ""value"": [] }";

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(jsonResponse);

            // Act
            await _sut.GetBouncedEmailContactsForSendAsync(sendId);

            // Assert
            _externalApiServiceMock.Verify(
                x => x.GetDataAsync(It.Is<string>(url =>
                    url.Contains("BouncedContacts") &&
                    url.Contains($"SendID%20eq%20{sendId}") &&
                    url.Contains("$skip=0") &&
                    url.Contains($"$top={_apiConfig.Object.Value.PageSize}"))),
                Times.Once);
        }

        #endregion

        #region GetUnsubscribedContactsForSendAsync Tests

        [Test]
        public async Task GetUnsubscribedContactsForSendAsync_WithValidResponse_ReturnsUnsubscribedContacts()
        {
            // Arrange
            int sendId = 100;
            var jsonResponse = @"{
                ""value"": [
                    {
                        ""ID"": 1,
                        ""UnsubscribedDate"": ""2024-02-01T15:00:00Z"",
                        ""Contact"": { ""Email"": ""unsub@test.com"" },
                        ""SendID"": 100,
                        ""CampaignID"": 200,
                        ""IsGlobalUnsubscribe"": true,
                        ""IsComplaint"": false
                    }
                ]
            }";

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(jsonResponse);

            // Act
            var result = (await _sut.GetUnsubscribedContactsForSendAsync(sendId)).ToList();

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].ID, Is.EqualTo(1));
            Assert.That(result[0].UnsubscribedDate, Is.EqualTo("2024-02-01T15:00:00Z"));
            Assert.That(result[0].ContactEmail, Is.EqualTo("unsub@test.com"));
            Assert.That(result[0].SendID, Is.EqualTo(100));
            Assert.That(result[0].CampaignID, Is.EqualTo(200));
            Assert.That(result[0].IsGlobalUnsubscribe, Is.True);
            Assert.That(result[0].IsComplaint, Is.False);
        }

        [Test]
        public async Task GetUnsubscribedContactsForSendAsync_EmptyResponse_ReturnsEmptyCollection()
        {
            // Arrange
            var jsonResponse = @"{ ""value"": [] }";

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(jsonResponse);

            // Act
            var result = await _sut.GetUnsubscribedContactsForSendAsync(100);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetUnsubscribedContactsForSendAsync_InvalidRecords_SkipsInvalidEntries()
        {
            // Arrange
            var jsonResponse = @"{
                ""value"": [
                    {
                        ""ID"": 0,
                        ""UnsubscribedDate"": ""2024-02-01T15:00:00Z"",
                        ""SendID"": 100,
                        ""CampaignID"": 200
                    },
                    {
                        ""ID"": 2,
                        ""UnsubscribedDate"": null,
                        ""SendID"": 100,
                        ""CampaignID"": 200
                    },
                    {
                        ""ID"": 3,
                        ""UnsubscribedDate"": ""2024-02-01T16:00:00Z"",
                        ""SendID"": 100,
                        ""CampaignID"": 200,
                        ""IsGlobalUnsubscribe"": false,
                        ""IsComplaint"": true
                    }
                ]
            }";

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(jsonResponse);

            // Act
            var result = (await _sut.GetUnsubscribedContactsForSendAsync(100)).ToList();

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].ID, Is.EqualTo(3));
            Assert.That(result[0].IsComplaint, Is.True);
        }

        [Test]
        public void GetUnsubscribedContactsForSendAsync_ApiThrowsException_PropagatesAndLogsError()
        {
            // Arrange
            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException("API error"));

            // Act & Assert
            Assert.ThrowsAsync<HttpRequestException>(
                async () => await _sut.GetUnsubscribedContactsForSendAsync(100));

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to retrieve unsubscribed email contacts for Send 100")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Test]
        public async Task GetUnsubscribedContactsForSendAsync_MultiplePages_ReturnsCombinedResults()
        {
            // Arrange
            int pageSize = 1;
            var firstPage = @"{
                ""value"": [
                    { ""ID"": 1, ""UnsubscribedDate"": ""2024-02-01T15:00:00Z"", ""SendID"": 100, ""CampaignID"": 200 }
                ]
            }";
            var secondPage = @"{ ""value"": [] }";

            _externalApiServiceMock
                .SetupSequence(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(firstPage)
                .ReturnsAsync(secondPage);

            var apiConfigSmall = new Mock<IOptions<EmailMarketingApi>>();
            apiConfigSmall.Setup(x => x.Value).Returns(new EmailMarketingApi
            {
                PageSize = pageSize,
                ApiBaseUrl = "https://api.eshot.com/api/v1.0",
                ApiKey = "test-api",
                ApiRetryCount = 3,
                ChunkSizeKB = 100,
            });

            var service = new CampaignService(
                _externalApiServiceMock.Object,
                _loggerMock.Object,
                apiConfigSmall.Object);

            // Act
            var result = (await service.GetUnsubscribedContactsForSendAsync(100)).ToList();

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            _externalApiServiceMock.Verify(x => x.GetDataAsync(It.IsAny<string>()), Times.Exactly(2));
        }

        [Test]
        public async Task GetUnsubscribedContactsForSendAsync_CorrectEndpointFormat()
        {
            // Arrange
            int sendId = 555;
            var jsonResponse = @"{ ""value"": [] }";

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(jsonResponse);

            // Act
            await _sut.GetUnsubscribedContactsForSendAsync(sendId);

            // Assert
            _externalApiServiceMock.Verify(
                x => x.GetDataAsync(It.Is<string>(url =>
                    url.Contains("UnsubscribedContacts") &&
                    url.Contains($"SendID%20eq%20{sendId}") &&
                    url.Contains("$skip=0") &&
                    url.Contains($"$top={_apiConfig.Object.Value.PageSize}"))),
                Times.Once);
        }

        #endregion
    }
}