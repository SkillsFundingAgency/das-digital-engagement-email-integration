using DAS.DigitalEngagement.Application.Services;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.CampaignInterest.Data.Helpers;
using DAS.DigitalEngagement.CampaignInterest.Data.Models;
using DAS.DigitalEngagement.CampaignInterest.Data.Repositories;
using DAS.DigitalEngagement.Models.Campaigns;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DAS.DigitalEngagement.EmailIntegration.UnitTests.Services;

[TestFixture]
public class CampaignServiceTests
{
    private Mock<IExternalApiService> _externalApiServiceMock;
    private Mock<IUnitOfWork> _unitOfWorkMock;
    private Mock<ILogger<CampaignService>> _loggerMock;
    private Mock<IOptions<EmailMarketingApi>> _apiConfig;
    private CampaignService _sut;

    private Mock<ICampaignImportMetadataRepository> _metadataRepositoryMock;
    private Mock<ICampaignsRepository> _campaignsRepositoryMock;
    private Mock<IBouncedEmailsRepository> _bouncedEmailsRepositoryMock;
    private Mock<IClickedLinksRepository> _clickedLinksRepositoryMock;
    private Mock<IDisplayedEmailsRepository> _displayedEmailsRepositoryMock;
    private Mock<IUnsubscribedContactsRepository> _unsubscribedContactsRepositoryMock;

    private Mock<IDbConnectionFactory> _mockConnectionFactory;
    private Mock<IDbConnection> _mockConnection;
    private Campaigns campaign;
    private CampaignImportMetadata campaignImportMetadata;
    private List<BouncedEmails> bouncedEmails;
    private List<ClickedLinks> clickedLinks;
    private List<DisplayedEmails> displayedEmails;
    private List<UnsubscribedContacts> unsubscribedContacts;

    [SetUp]
    public void SetUp()
    {
        _mockConnection = new Mock<IDbConnection>();
        _mockConnectionFactory = new Mock<IDbConnectionFactory>();
        _mockConnectionFactory.Setup(f => f.CreateConnectionAsync()).ReturnsAsync(_mockConnection.Object);

        _externalApiServiceMock = new Mock<IExternalApiService>();
        _metadataRepositoryMock = new Mock<ICampaignImportMetadataRepository>();
        _campaignsRepositoryMock = new Mock<ICampaignsRepository>();
        _bouncedEmailsRepositoryMock = new Mock<IBouncedEmailsRepository>();
        _clickedLinksRepositoryMock = new Mock<IClickedLinksRepository>();
        _displayedEmailsRepositoryMock = new Mock<IDisplayedEmailsRepository>();
        _unsubscribedContactsRepositoryMock = new Mock<IUnsubscribedContactsRepository>();

        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(x => x.CampaignImportMetadata).Returns(_metadataRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.Campaigns).Returns(_campaignsRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.BouncedEmails).Returns(_bouncedEmailsRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.ClickedLinks).Returns(_clickedLinksRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.DisplayedEmails).Returns(_displayedEmailsRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.UnsubscribedContacts).Returns(_unsubscribedContactsRepositoryMock.Object);

        _loggerMock = new Mock<ILogger<CampaignService>>();
        _apiConfig = new Mock<IOptions<EmailMarketingApi>>();
        _apiConfig.Setup(x => x.Value).Returns(new EmailMarketingApi
        {
            PageSize = 5000,
            ApiBaseUrl = "https://api.eshot.com/api/v1.0",
            ApiKey = "test-api",
            ApiRetryCount = 3,
            ChunkSizeKB = 100,
            ImportWindowDays = 7,
        });
        _sut = new CampaignService(_externalApiServiceMock.Object, _unitOfWorkMock.Object, _loggerMock.Object, _apiConfig.Object);

        campaign = new Campaigns
        {
            Id = 12345,
            ExternalId = 100,
            Name = "Test Campaign",
            Type = "Email",
            CreatedBy = "TestUser",
            CreatedOn = DateTime.UtcNow,
            ModifiedBy = "TestUser",
            ModifiedOn = DateTime.UtcNow,
            FirstSendDate = DateTime.UtcNow,
            LastSendDate = DateTime.UtcNow,
            FromEmailAddress = "test@example.com",
            FromName = "Test Sender",
            ReplyEmailAddress = "reply@example.com",
            Subject = "Test Subject",
            SubStatus = "Active",
            ContactCount = 100,
            Account = "TestAccount"
        };

        campaignImportMetadata = new CampaignImportMetadata
        {
            Id = 1,
            CampaignId = campaign.Id,
            IsImportComplete = true,
            ImportStartDate = DateTime.UtcNow.AddDays(-1)
        };

        bouncedEmails =
        [
            new BouncedEmails { Id = 1, ContactEmail = "Test1@test.com" },
            new BouncedEmails { Id = 2, ContactEmail = "Test2@test.com" }
        ];

        clickedLinks =
        [
            new ClickedLinks { Id = 1, ContactEmail = "Test1@test.com" },
            new ClickedLinks { Id = 2, ContactEmail = "Test2@test.com" }
        ];

        displayedEmails =
        [
            new DisplayedEmails { Id = 1, ContactEmail = "Test1@test.com" },
            new DisplayedEmails { Id = 2, ContactEmail = "Test2@test.com" }
        ];

        unsubscribedContacts =
        [
            new UnsubscribedContacts { Id = 1, ContactEmail = "Test1@test.com" },
            new UnsubscribedContacts { Id = 2, ContactEmail = "Test2@test.com" }
        ];
    }

    #region Sends Tests

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
    public async Task GetAllSendsAsync_WithMissingValueKey_ReturnsEmptyCollection()
    {
        // Arrange
        var jsonResponse = @"{ ""other"": 123 }";

        _externalApiServiceMock
            .Setup(x => x.GetDataAsync(It.IsAny<string>()))
            .ReturnsAsync(jsonResponse);

        // Act
        var result = await _sut.GetAllSendsAsync();

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
    public async Task GetAllSendsAsync_WithMinimalFields_DefaultsMissingProperties()
    {
        // Arrange
        var jsonResponse = @"{
                ""value"": [
                    {
                        ""ID"": 5,
                        ""SendCompletedDate"": ""2024-01-15T10:00:00Z""
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
        Assert.That(result[0].ID, Is.EqualTo(5));
        Assert.That(result[0].SendCompletedDate, Is.EqualTo("2024-01-15T10:00:00Z"));
        Assert.That(result[0].Name, Is.Null);
        Assert.That(result[0].CampaignID, Is.EqualTo(0));
        Assert.That(result[0].Status, Is.Null);
        Assert.That(result[0].SubStatus, Is.Null);
        Assert.That(result[0].SendDate, Is.Null);
        Assert.That(result[0].CampaignType, Is.Null);
        Assert.That(result[0].ContactCount, Is.EqualTo(0));
        Assert.That(result[0].CreatedBy, Is.Null);
        Assert.That(result[0].CreatedDate, Is.Null);
        Assert.That(result[0].FirstSendDate, Is.Null);
        Assert.That(result[0].LastSendDate, Is.Null);
        Assert.That(result[0].FromEmail, Is.Null);
        Assert.That(result[0].FromName, Is.Null);
        Assert.That(result[0].ReplyEmail, Is.Null);
        Assert.That(result[0].SubjectLine, Is.Null);
        Assert.That(result[0].Account, Is.Null);
    }

    [Test]
    public void GetSendsForSubAccountAsync_WithNullResponse_ThrowsException()
    {
        // Arrange
        const int subAccountId = 123;

        _externalApiServiceMock
            .Setup(x => x.GetDataAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("API Error"));

        // Act & Assert
        Assert.ThrowsAsync<Exception>(() => _sut.GetAllSendsAsync(subAccountId));
    }

    [Test]
    public async Task GetSendsForSubAccountAsync_WithValidSubAccountId_CallsGetDataAsyncWithCorrectEndpoint()
    {
        // Arrange
        const int subAccountId = 456;
        var jsonResponse = @"{ ""value"": [] }";

        _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ReturnsAsync(jsonResponse);

        // Act
        await _sut.GetAllSendsAsync(subAccountId);

        // Assert
        _externalApiServiceMock.Verify(x => x.GetDataAsync(It.Is<string>(endpoint => endpoint.Contains("Sends"))), Times.Once);
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

        _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ReturnsAsync(jsonResponse);

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

    #endregion

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

        _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ReturnsAsync(jsonResponse);

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
            _unitOfWorkMock.Object,
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

        _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ReturnsAsync(emptyResponse);

        // Act
        var result = await _sut.GetUserAgentInfoForSendAsync(sendId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(0));

        _externalApiServiceMock.Verify(x => x.GetDataAsync(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task GetUserAgentInfoForSendAsync_WithMissingValueKey_ReturnsEmptyCollection()
    {
        // Arrange
        var jsonResponse = @"{ ""other"": 123 }";

        _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ReturnsAsync(jsonResponse);

        // Act
        var result = await _sut.GetUserAgentInfoForSendAsync(123);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
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

        _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ReturnsAsync(jsonResponse);

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

        _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ReturnsAsync(jsonResponse);

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

        _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ThrowsAsync(new HttpRequestException(exceptionMessage));

        // Act & Assert
        var ex = Assert.ThrowsAsync<HttpRequestException>(async () => await _sut.GetUserAgentInfoForSendAsync(sendId));

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

        _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ReturnsAsync(jsonResponse);

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
        using (var cancellationTokenSource = new CancellationTokenSource())
        {
            cancellationTokenSource.Cancel();

            _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            Assert.ThrowsAsync<OperationCanceledException>(async () => await _sut.GetUserAgentInfoForSendAsync(sendId, cancellationTokenSource.Token));
        }
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

        _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ReturnsAsync(jsonResponse);

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

        _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ReturnsAsync(invalidJsonResponse);

        // Act & Assert
        Assert.That(async () => await _sut.GetUserAgentInfoForSendAsync(sendId), Throws.InstanceOf<JsonException>());
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

        _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ReturnsAsync(jsonResponse);

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
            new() { ID = 50, SendID = 100, CampaignID = 200, Device = "Desktop", ClientName = "Gmail", OperatingSystem = "Windows 10", OperatingSystemFamily = "Windows", IPAddress = "10.0.0.1", ClientType = "Webmail", ClientFamily = "Gmail" }
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

        _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ReturnsAsync(jsonResponse);

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

        _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ReturnsAsync(jsonResponse);

        // Act
        var result = await _sut.GetDisplayedContactsForSendAsync(100, Enumerable.Empty<UserAgentInfo>());

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetDisplayedContactsForSendAsync_WithMissingValueKey_ReturnsEmptyCollection()
    {
        // Arrange
        var jsonResponse = @"{ ""other"": 123 }";

        _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ReturnsAsync(jsonResponse);

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

        _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ReturnsAsync(jsonResponse);

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
            new() { ID = 999, Device = "Mobile" }
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

        _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ReturnsAsync(jsonResponse);

        // Act
        var result = (await _sut.GetDisplayedContactsForSendAsync(100, userAgentInfos)).ToList();

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Device, Is.Null);
        Assert.That(result[0].ClientName, Is.Null);
    }

    [Test]
    public async Task GetDisplayedContactsForSendAsync_WithMissingUserAgentIdKey_LeavesUserAgentFieldsNull()
    {
        // Arrange
        var jsonResponse = @"{
                ""value"": [
                    {
                        ""ID"": 1,
                        ""DisplayDate"": ""2024-02-01T12:00:00Z"",
                        ""SendID"": 100,
                        ""CampaignID"": 200
                    }
                ]
            }";

        _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ReturnsAsync(jsonResponse);

        // Act
        var result = (await _sut.GetDisplayedContactsForSendAsync(100, Enumerable.Empty<UserAgentInfo>())).ToList();

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

        _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ReturnsAsync(jsonResponse);

        // Act
        var result = (await _sut.GetDisplayedContactsForSendAsync(100, Enumerable.Empty<UserAgentInfo>())).ToList();

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Device, Is.Null);
    }

    [Test]
    public void GetDisplayedContactsForSendAsync_ApiThrowsException_Propagates()
    {
        // Arrange
        _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ThrowsAsync(new HttpRequestException("API error"));

        // Act & Assert
        Assert.ThrowsAsync<HttpRequestException>(async () => await _sut.GetDisplayedContactsForSendAsync(100, Enumerable.Empty<UserAgentInfo>()));
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

        var service = new CampaignService(_externalApiServiceMock.Object, _unitOfWorkMock.Object, _loggerMock.Object, apiConfigSmall.Object);

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

        _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ReturnsAsync(jsonResponse);

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
            new() { ID = 50, Device = "Desktop", ClientName = "Chrome", OperatingSystem = "Windows 11", OperatingSystemFamily = "Windows", IPAddress = "10.0.0.1", ClientType = "Browser", ClientFamily = "Chrome" }
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

        _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ReturnsAsync(jsonResponse);

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
        Assert.That(result[0].OperatingSystem, Is.EqualTo("Windows 11"));
        Assert.That(result[0].IPAddress, Is.EqualTo("10.0.0.1"));
    }

    [Test]
    public async Task GetClickedLinkContactsForSendAsync_EmptyResponse_ReturnsEmptyCollection()
    {
        // Arrange
        var jsonResponse = @"{ ""value"": [] }";

        _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ReturnsAsync(jsonResponse);

        // Act
        var result = await _sut.GetClickedLinkContactsForSendAsync(100, Enumerable.Empty<UserAgentInfo>());

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetClickedLinkContactsForSendAsync_WithMissingValueKey_ReturnsEmptyCollection()
    {
        // Arrange
        var jsonResponse = @"{ ""other"": 123 }";

        _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ReturnsAsync(jsonResponse);

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

        _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ReturnsAsync(jsonResponse);

        // Act
        var result = (await _sut.GetClickedLinkContactsForSendAsync(100, Enumerable.Empty<UserAgentInfo>())).ToList();

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].ID, Is.EqualTo(3));
    }

    [Test]
    public async Task GetClickedLinkContactsForSendAsync_WithMissingUserAgentIdKey_LeavesUserAgentFieldsNull()
    {
        // Arrange
        var jsonResponse = @"{
                ""value"": [
                    {
                        ""ID"": 1,
                        ""ClickDate"": ""2024-02-01T12:30:00Z"",
                        ""SendID"": 100,
                        ""CampaignID"": 200
                    }
                ]
            }";

        _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ReturnsAsync(jsonResponse);

        // Act
        var result = (await _sut.GetClickedLinkContactsForSendAsync(100, Enumerable.Empty<UserAgentInfo>())).ToList();

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Device, Is.Null);
        Assert.That(result[0].ClientName, Is.Null);
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

        _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ReturnsAsync(jsonResponse);

        // Act
        var result = (await _sut.GetClickedLinkContactsForSendAsync(100, Enumerable.Empty<UserAgentInfo>())).ToList();

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Device, Is.Null);
        Assert.That(result[0].ClientName, Is.Null);
    }

    [Test]
    public void GetClickedLinkContactsForSendAsync_ApiThrowsException_Propagates()
    {
        // Arrange
        _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ThrowsAsync(new HttpRequestException("API error"));

        // Act & Assert
        Assert.ThrowsAsync<HttpRequestException>(async () => await _sut.GetClickedLinkContactsForSendAsync(100, Enumerable.Empty<UserAgentInfo>()));
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

        var service = new CampaignService(_externalApiServiceMock.Object, _unitOfWorkMock.Object, _loggerMock.Object, apiConfigSmall.Object);

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

        _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ReturnsAsync(jsonResponse);

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
    public async Task GetBouncedEmailContactsForSendAsync_WithMissingValueKey_ReturnsEmptyCollection()
    {
        // Arrange
        var jsonResponse = @"{ ""other"": 123 }";

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

        _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ReturnsAsync(jsonResponse);

        // Act
        var result = (await _sut.GetBouncedEmailContactsForSendAsync(100)).ToList();

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].ID, Is.EqualTo(3));
        Assert.That(result[0].BounceType, Is.EqualTo("Hard"));
    }

    [Test]
    public async Task GetBouncedEmailContactsForSendAsync_WithPartiallyMissingFields_HandlesGracefully()
    {
        // Arrange
        var jsonResponse = @"{
                ""value"": [
                    {
                        ""ID"": 1,
                        ""BounceDate"": ""2024-02-01T12:00:00Z""
                    }
                ]
            }";

        _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ReturnsAsync(jsonResponse);

        // Act
        var result = (await _sut.GetBouncedEmailContactsForSendAsync(100)).ToList();

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].ID, Is.EqualTo(1));
        Assert.That(result[0].BounceDate, Is.EqualTo("2024-02-01T12:00:00Z"));
        Assert.That(result[0].BounceReason, Is.Null);
        Assert.That(result[0].BounceType, Is.Null);
        Assert.That(result[0].ContactEmail, Is.Null);
        Assert.That(result[0].ResponseText, Is.Null);
        Assert.That(result[0].SendID, Is.EqualTo(0));
        Assert.That(result[0].CampaignID, Is.EqualTo(0));
    }

    [Test]
    public void GetBouncedEmailContactsForSendAsync_ApiThrowsException_Propagates()
    {
        // Arrange
        _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ThrowsAsync(new HttpRequestException("API error"));

        // Act & Assert
        Assert.ThrowsAsync<HttpRequestException>(async () => await _sut.GetBouncedEmailContactsForSendAsync(100));
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

        _externalApiServiceMock.SetupSequence(x => x.GetDataAsync(It.IsAny<string>())).ReturnsAsync(firstPage).ReturnsAsync(secondPage);

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
            _unitOfWorkMock.Object,
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
    public async Task GetUnsubscribedContactsForSendAsync_WithMissingValueKey_ReturnsEmptyCollection()
    {
        // Arrange
        var jsonResponse = @"{ ""other"": 123 }";

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
    public async Task GetUnsubscribedContactsForSendAsync_WithPartiallyMissingFields_HandlesGracefully()
    {
        // Arrange
        var jsonResponse = @"{
                ""value"": [
                    {
                        ""ID"": 1,
                        ""UnsubscribedDate"": ""2024-02-01T15:00:00Z""
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
        Assert.That(result[0].ID, Is.EqualTo(1));
        Assert.That(result[0].UnsubscribedDate, Is.EqualTo("2024-02-01T15:00:00Z"));
        Assert.That(result[0].ContactEmail, Is.Null);
        Assert.That(result[0].SendID, Is.EqualTo(0));
        Assert.That(result[0].CampaignID, Is.EqualTo(0));
        Assert.That(result[0].IsGlobalUnsubscribe, Is.False);
        Assert.That(result[0].IsComplaint, Is.False);
    }

    [Test]
    public void GetUnsubscribedContactsForSendAsync_ApiThrowsException_Propagates()
    {
        // Arrange
        _externalApiServiceMock
            .Setup(x => x.GetDataAsync(It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("API error"));

        // Act & Assert
        Assert.ThrowsAsync<HttpRequestException>(
            async () => await _sut.GetUnsubscribedContactsForSendAsync(100));
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
            _unitOfWorkMock.Object,
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

    #region GetEligibleSendsAsync Tests

    [Test]
    public async Task GetEligibleSendsAsync_AllSendsEligible_ReturnsAll()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var jsonResponse = @"{
                ""value"": [
                    { ""ID"": 1, ""Name"": ""Send 1"", ""SendCompletedDate"": """ + now.AddDays(-8).ToString("O") + @""", ""ContactCount"": 100 },
                    { ""ID"": 2, ""Name"": ""Send 2"", ""SendCompletedDate"": """ + now.AddDays(-11).ToString("O") + @""", ""ContactCount"": 200 }
                ]
            }";

        _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ReturnsAsync(jsonResponse);
        _metadataRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync([]);

        // Act
        var result = (await _sut.GetEligibleSendsAsync()).ToList();

        // Assert
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result[0].ID, Is.EqualTo(1));
        Assert.That(result[1].ID, Is.EqualTo(2));
    }

    [Test]
    public async Task GetEligibleSendsAsync_FullyImportedSends_AreExcluded()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var jsonResponse = @"{
                ""value"": [
                    { ""ID"": 1, ""Name"": ""Send 1"", ""SendCompletedDate"": """ + now.AddDays(-10).ToString("O") + @""", ""ContactCount"": 100 },
                    { ""ID"": 2, ""Name"": ""Send 2"", ""SendCompletedDate"": """ + now.AddDays(-20).ToString("O") + @""", ""ContactCount"": 200 },
                    { ""ID"": 3, ""Name"": ""Send 3"", ""SendCompletedDate"": """ + now.AddDays(-30).ToString("O") + @""", ""ContactCount"": 300 }
                ]
            }";

        _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ReturnsAsync(jsonResponse);

        _metadataRepositoryMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync([
                new() { CampaignId = 1, IsImportComplete = true, ImportStartDate = now.AddDays(-1) },
                new() { CampaignId = 3, IsImportComplete = true, ImportStartDate = now.AddDays(-3) }
            ]);

        // Act
        var result = (await _sut.GetEligibleSendsAsync()).ToList();

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].ID, Is.EqualTo(2));
    }

    [Test]
    public async Task GetEligibleSendsAsync_PartiallyImportedSends_AreIncluded()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var jsonResponse = @"{
                ""value"": [
                    { ""ID"": 1, ""Name"": ""Send 1"", ""SendCompletedDate"": """ + now.AddDays(-10).ToString("O") + @""", ""ContactCount"": 100 }
                ]
            }";

        _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ReturnsAsync(jsonResponse);

        _metadataRepositoryMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync([
                new CampaignImportMetadata { CampaignId = 1, IsImportComplete = false, ImportStartDate = now.AddDays(-1) }
            ]);

        // Act
        var result = (await _sut.GetEligibleSendsAsync()).ToList();

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].ID, Is.EqualTo(1));
    }

    [Test]
    public async Task GetEligibleSendsAsync_SendsOutsideTimeWindow_AreExcluded()
    {
        // Arrange - ImportWindowDays is 7 in SetUp
        var now = DateTime.UtcNow;
        var jsonResponse = @"{
                ""value"": [
                    { ""ID"": 1, ""Name"": ""Recent"", ""SendCompletedDate"": """ + now.AddDays(-3).ToString("O") + @""", ""ContactCount"": 100 },
                    { ""ID"": 2, ""Name"": ""Old"", ""SendCompletedDate"": """ + now.AddDays(-10).ToString("O") + @""", ""ContactCount"": 200 },
                    { ""ID"": 3, ""Name"": ""Very Old"", ""SendCompletedDate"": """ + now.AddDays(-30).ToString("O") + @""", ""ContactCount"": 300 }
                ]
            }";

        _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ReturnsAsync(jsonResponse);
        _metadataRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(Enumerable.Empty<CampaignImportMetadata>());

        // Act
        var result = (await _sut.GetEligibleSendsAsync()).ToList();

        // Assert
        Assert.That(result.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task GetEligibleSendsAsync_MixOfConditions_ReturnsOnlyEligible()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var jsonResponse = @"{
                ""value"": [
                    { ""ID"": 1, ""Name"": ""Eligible"", ""SendCompletedDate"": """ + now.AddDays(-2).ToString("O") + @""", ""ContactCount"": 100 },
                    { ""ID"": 2, ""Name"": ""Already Imported"", ""SendCompletedDate"": """ + now.AddDays(-1).ToString("O") + @""", ""ContactCount"": 200 },
                    { ""ID"": 3, ""Name"": ""Too Old"", ""SendCompletedDate"": """ + now.AddDays(-15).ToString("O") + @""", ""ContactCount"": 300 },
                    { ""ID"": 4, ""Name"": ""Partial Import"", ""SendCompletedDate"": """ + now.AddDays(-3).ToString("O") + @""", ""ContactCount"": 400 }
                ]
            }";

        _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ReturnsAsync(jsonResponse);

        _metadataRepositoryMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync([
                new CampaignImportMetadata { CampaignId = 2, IsImportComplete = true, ImportStartDate = now.AddDays(-1) },
                new CampaignImportMetadata { CampaignId = 4, IsImportComplete = false, ImportStartDate = now.AddDays(-3) }
            ]);

        // Act
        var result = (await _sut.GetEligibleSendsAsync()).ToList();

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.Select(s => s.ID), Does.Contain(3));
    }

    [Test]
    public async Task GetEligibleSendsAsync_NoSendsFromApi_ReturnsEmpty()
    {
        // Arrange
        var jsonResponse = @"{ ""value"": [] }";

        _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ReturnsAsync(jsonResponse);

        // Act
        var result = await _sut.GetEligibleSendsAsync();

        // Assert
        Assert.That(result, Is.Empty);
        _metadataRepositoryMock.Verify(x => x.GetAllAsync(), Times.Never);
    }

    [Test]
    public async Task GetEligibleSendsAsync_EmptyMetadata_AllWithinWindowAreEligible()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var jsonResponse = @"{
                ""value"": [
                    { ""ID"": 1, ""Name"": ""Send 1"", ""SendCompletedDate"": """ + now.AddDays(-10).ToString("O") + @""", ""ContactCount"": 100 }
                ]
            }";

        _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ReturnsAsync(jsonResponse);
        _metadataRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(Enumerable.Empty<CampaignImportMetadata>());

        // Act
        var result = (await _sut.GetEligibleSendsAsync()).ToList();

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
    }

    [Test]
    public void GetEligibleSendsAsync_ApiThrowsException_Propagates()
    {
        // Arrange
        _externalApiServiceMock.Setup(x => x.GetDataAsync(It.IsAny<string>())).ThrowsAsync(new HttpRequestException("API error"));

        // Act & Assert
        Assert.ThrowsAsync<HttpRequestException>(async () => await _sut.GetEligibleSendsAsync());
    }

    #endregion

    #region Data Access Tests

    #region GetCampaignDetailsAsync Tests

    [Test]
    public async Task GetCampaignDetailsAsync_WithValidResponse_ReturnsDetails()
    {
        // Arrange
        _campaignsRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<long>())).ReturnsAsync(campaign);

        // Act
        var result = await _sut.GetCampaignDetailsAsync(campaign.Id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(campaign.Id));
        Assert.That(result.Name, Is.EqualTo("Test Campaign"));
        Assert.That(result.Subject, Is.EqualTo("Test Subject"));
        Assert.That(result.FromEmailAddress, Is.EqualTo("test@example.com"));
        Assert.That(result.Account, Is.EqualTo("TestAccount"));
        Assert.That(result.SubStatus, Is.EqualTo("Active"));
        Assert.That(result.FromName, Is.EqualTo("Test Sender"));
        Assert.That(result.ReplyEmailAddress, Is.EqualTo("reply@example.com"));
        Assert.That(result.ContactCount, Is.EqualTo(100));

        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Successfully retrieved campaign details for CampaignID {campaign.Id}")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetCampaignDetailsAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = await _sut.GetCampaignDetailsAsync(0);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetCampaignDetailsAsync_WithIdNotFound_ReturnsNull()
    {
        // Act
        var result = await _sut.GetCampaignDetailsAsync(9999999);

        // Assert
        Assert.That(result, Is.Null);

        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No campaign details found in database for CampaignID 9999999")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetCampaignDetailsAsync_WhenRepositoryThrowsException_ReturnsNullAndLogsError()
    {
        // Arrange
        var exception = new Exception("Database error");
        _campaignsRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<long>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _sut.GetCampaignDetailsAsync(campaign.Id);

        // Assert
        Assert.That(result, Is.Null);
        _loggerMock.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Error retrieving campaign details for CampaignID {campaign.Id} from database")),
            It.Is<Exception>(e => e == exception),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Test]
    public async Task SaveCampaignDetailsAsync_WithTrueResponse()
    {
        // Arrange
        _campaignsRepositoryMock.Setup(x => x.UpsertAsync(It.IsAny<Campaigns>())).ReturnsAsync(1);

        // Act
        var result = await _sut.SaveCampaignDetailsAsync(campaign);

        // Assert
        Assert.That(result, Is.EqualTo(true));

        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Saving campaign details for CampaignID {campaign.Id} to database")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Successfully saved campaign details for CampaignID {campaign.Id} to database")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task SaveCampaignDetailsAsync_WithFalseResponse()
    {
        // Arrange
        _campaignsRepositoryMock.Setup(x => x.UpsertAsync(It.IsAny<Campaigns>())).ReturnsAsync(0);

        // Act
        var result = await _sut.SaveCampaignDetailsAsync(campaign);

        // Assert
        Assert.That(result, Is.EqualTo(false));

        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Saving campaign details for CampaignID {campaign.Id} to database")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"No rows were inserted or updated when saving campaign details for CampaignID {campaign.Id}")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task SaveCampaignDetailsAsync_ThrowsException()
    {
        // Arrange
        _campaignsRepositoryMock.Setup(x => x.UpsertAsync(It.IsAny<Campaigns>())).ThrowsAsync(new Exception("Test exception"));

        // Act & Assert
        Assert.ThrowsAsync<NullReferenceException>(async () => await _sut.SaveCampaignDetailsAsync(null));
    }

    [Test]
    public async Task SaveCampaignDetailsAsync_WhenUpsertThrowsException_ReturnsFalseAndLogsError()
    {
        // Arrange
        var exception = new Exception("Database error");
        _campaignsRepositoryMock
            .Setup(x => x.UpsertAsync(It.IsAny<Campaigns>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _sut.SaveCampaignDetailsAsync(campaign);

        // Assert
        Assert.That(result, Is.False);
        _loggerMock.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Error saving campaign details for CampaignID {campaign.Id} to database")),
            It.Is<Exception>(e => e == exception),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    #endregion

    #region GetCampaignImportMetadataAsync Tests

    [Test]
    public async Task GetCampaignImportMetadataAsync_WithValidResponse_ReturnsDetails()
    {
        // Arrange
        _metadataRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<long>())).ReturnsAsync(campaignImportMetadata);

        // Act
        var result = await _sut.GetCampaignImportMetadataAsync(campaign.Id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.CampaignId, Is.EqualTo(campaignImportMetadata.CampaignId));

        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Successfully retrieved campaign import metadata for CampaignID {campaign.Id}")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetCampaignImportMetadataAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = await _sut.GetCampaignImportMetadataAsync(0);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetCampaignImportMetadataAsync_WithIdNotFound_ReturnsNull()
    {
        // Act
        var result = await _sut.GetCampaignImportMetadataAsync(9999999);

        // Assert
        Assert.That(result, Is.Null);

        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No campaign import metadata found in database for CampaignID 9999999")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetCampaignImportMetadataAsync_WhenRepositoryThrowsException_ReturnsNullAndLogsError()
    {
        // Arrange
        var exception = new Exception("Database error");
        _metadataRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<long>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _sut.GetCampaignImportMetadataAsync(campaign.Id);

        // Assert
        Assert.That(result, Is.Null);

        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Error retrieving campaign import metadata for CampaignID {campaign.Id} from database")),
                It.Is<Exception>(e => e == exception),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task UpsertCampaignImportMetadataAsync_WithTrueResponse()
    {
        // Arrange
        _metadataRepositoryMock.Setup(x => x.UpsertAsync(It.IsAny<CampaignImportMetadata>())).ReturnsAsync(1);

        // Act
        var result = await _sut.UpsertCampaignImportMetadataAsync(campaignImportMetadata);

        // Assert
        Assert.That(result, Is.EqualTo(true));

        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Upserting campaign import metadata for CampaignID {campaign.Id} to database")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Successfully upserted campaign import metadata for CampaignID {campaign.Id} to database")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task UpsertCampaignImportMetadataAsync_WithFalseResponse()
    {
        // Arrange
        _metadataRepositoryMock.Setup(x => x.UpsertAsync(It.IsAny<CampaignImportMetadata>())).ReturnsAsync(0);

        // Act
        var result = await _sut.UpsertCampaignImportMetadataAsync(campaignImportMetadata);

        // Assert
        Assert.That(result, Is.EqualTo(false));

        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Upserting campaign import metadata for CampaignID {campaign.Id} to database")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"No rows were inserted or updated when upserting campaign import metadata for CampaignID {campaign.Id}")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task UpsertCampaignImportMetadataAsync_ThrowsException()
    {
        // Arrange
        _metadataRepositoryMock.Setup(x => x.UpsertAsync(It.IsAny<CampaignImportMetadata>())).ThrowsAsync(new Exception("Test exception"));

        // Act & Assert
        Assert.ThrowsAsync<NullReferenceException>(async () => await _sut.UpsertCampaignImportMetadataAsync(null));
    }

    [Test]
    public async Task UpsertCampaignImportMetadataAsync_WhenUpsertThrowsException_ReturnsFalseAndLogsError()
    {
        // Arrange
        var exception = new Exception("Database error");
        _metadataRepositoryMock
            .Setup(x => x.UpsertAsync(It.IsAny<CampaignImportMetadata>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _sut.UpsertCampaignImportMetadataAsync(campaignImportMetadata);

        // Assert
        Assert.That(result, Is.False);

        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Error upserting campaign import metadata for CampaignID {campaignImportMetadata.CampaignId} to database")),
                It.Is<Exception>(e => e == exception),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Bounced Contacts Bulk Insert Tests

    [Test]
    public async Task BulkInsertAsyncAsync_BouncedContacts_WithTrueResponse()
    {
        // Act
        var result = await _sut.BulkInsertBouncedContactsAsync(bouncedEmails);

        // Assert
        Assert.That(result, Is.EqualTo(true));

        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Bulk inserting {bouncedEmails.Count} bounced contacts into database")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Successfully bulk inserted {bouncedEmails.Count} bounced contacts into database")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task BulkInsertAsyncAsync_BouncedContacts_WhenNothingToInsert_ReturnsTrue()
    {
        // Act
        var result = await _sut.BulkInsertBouncedContactsAsync([]);

        // Assert
        Assert.That(result, Is.EqualTo(true));

        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Bulk inserting 0 bounced contacts into database")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No bounced contacts to insert into database")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task BulkInsertBouncedContactsAsync_WhenBulkInsertThrowsException_ReturnsFalseAndLogsError()
    {
        // Arrange
        var exception = new Exception("Database error");
        _bouncedEmailsRepositoryMock
            .Setup(x => x.BulkInsertAsync(It.IsAny<IEnumerable<BouncedEmails>>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _sut.BulkInsertBouncedContactsAsync(bouncedEmails);

        // Assert
        Assert.That(result, Is.False);

        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error bulk inserting bounced contacts into database")),
                It.Is<Exception>(e => e == exception),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Clicked Links Bulk Insert Tests

    [Test]
    public async Task BulkInsertAsyncAsync_ClickedLinks_WithTrueResponse()
    {
        // Act
        var result = await _sut.BulkInsertClickedLinksAsync(clickedLinks);

        // Assert
        Assert.That(result, Is.EqualTo(true));

        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Bulk inserting {clickedLinks.Count} clicked link contacts into database")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Successfully bulk inserted {clickedLinks.Count} clicked link contacts into database")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task BulkInsertAsyncAsync_ClickedLinks_WhenNothingToInsert_ReturnsTrue()
    {
        // Act
        var result = await _sut.BulkInsertClickedLinksAsync([]);

        // Assert
        Assert.That(result, Is.EqualTo(true));

        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Bulk inserting 0 clicked link contacts into database")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No clicked link contacts to insert into database")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task BulkInsertClickedLinksAsync_WhenBulkInsertThrowsException_ReturnsFalseAndLogsError()
    {
        // Arrange
        var exception = new Exception("Database error");
        _clickedLinksRepositoryMock
            .Setup(x => x.BulkInsertAsync(It.IsAny<IEnumerable<ClickedLinks>>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _sut.BulkInsertClickedLinksAsync(clickedLinks);

        // Assert
        Assert.That(result, Is.False);

        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error bulk inserting clicked link contacts into database")),
                It.Is<Exception>(e => e == exception),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Displayed Emails Bulk Insert Tests

    [Test]
    public async Task BulkInsertAsyncAsync_DisplayedEmails_WithTrueResponse()
    {
        // Act
        var result = await _sut.BulkInsertDisplayedContactsAsync(displayedEmails);

        // Assert
        Assert.That(result, Is.EqualTo(true));

        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Bulk inserting {displayedEmails.Count} displayed email contacts into database")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Successfully bulk inserted {displayedEmails.Count} displayed email contacts into database")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task BulkInsertAsyncAsync_DisplayedEmails_WhenNothingToInsert_ReturnsTrue()
    {
        // Act
        var result = await _sut.BulkInsertDisplayedContactsAsync([]);

        // Assert
        Assert.That(result, Is.EqualTo(true));

        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Bulk inserting 0 displayed email contacts into database")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No displayed email contacts to insert into database")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task BulkInsertDisplayedContactsAsync_WhenBulkInsertThrowsException_ReturnsFalseAndLogsError()
    {
        // Arrange
        var exception = new Exception("Database error");
        _displayedEmailsRepositoryMock
            .Setup(x => x.BulkInsertAsync(It.IsAny<IEnumerable<DisplayedEmails>>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _sut.BulkInsertDisplayedContactsAsync(displayedEmails);

        // Assert
        Assert.That(result, Is.False);

        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error bulk inserting displayed email contacts into database")),
                It.Is<Exception>(e => e == exception),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Unsubscribed Contacts Bulk Insert Tests

    [Test]
    public async Task BulkInsertAsyncAsync_UnsubscribedContacts_WithTrueResponse()
    {
        // Act
        var result = await _sut.BulkInsertUnsubscribedContactsAsync(unsubscribedContacts);

        // Assert
        Assert.That(result, Is.EqualTo(true));

        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Bulk inserting {unsubscribedContacts.Count} unsubscribed contacts into database")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Successfully bulk inserted {unsubscribedContacts.Count} unsubscribed contacts into database")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task BulkInsertAsyncAsync_UnsubscribedContacts_WhenNothingToInsert_ReturnsTrue()
    {
        // Act
        var result = await _sut.BulkInsertUnsubscribedContactsAsync([]);

        // Assert
        Assert.That(result, Is.EqualTo(true));

        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Bulk inserting 0 unsubscribed contacts into database")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No unsubscribed contacts to insert into database")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task BulkInsertUnsubscribedContactsAsync_WhenBulkInsertThrowsException_ReturnsFalseAndLogsError()
    {
        // Arrange
        var exception = new Exception("Database error");
        _unsubscribedContactsRepositoryMock
            .Setup(x => x.BulkInsertAsync(It.IsAny<IEnumerable<UnsubscribedContacts>>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _sut.BulkInsertUnsubscribedContactsAsync(unsubscribedContacts);

        // Assert
        Assert.That(result, Is.False);

        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error bulk inserting unsubscribed contacts into database")),
                It.Is<Exception>(e => e == exception),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #endregion
}
