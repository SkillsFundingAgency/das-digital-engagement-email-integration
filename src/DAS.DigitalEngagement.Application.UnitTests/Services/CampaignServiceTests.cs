using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DAS.DigitalEngagement.Application.Services;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.Models.Campaigns;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace DAS.DigitalEngagement.Application.UnitTests.Services
{
    [TestFixture]
    public class CampaignServiceTests
    {
        private Mock<IExternalApiService> _externalApiServiceMock;
        private Mock<ILogger<CampaignService>> _loggerMock;
        private Mock<IOptions<EmailMarketingApi>> _apiConfigMock;
        private CampaignService _sut;
        private const int PageSize = 10;

        [SetUp]
        public void SetUp()
        {
            _externalApiServiceMock = new Mock<IExternalApiService>();
            _loggerMock = new Mock<ILogger<CampaignService>>();
            _apiConfigMock = new Mock<IOptions<EmailMarketingApi>>();
            
            var apiConfig = new EmailMarketingApi { PageSize = PageSize };
            _apiConfigMock.Setup(o => o.Value).Returns(apiConfig);

            _sut = new CampaignService(
                _externalApiServiceMock.Object,
                _loggerMock.Object,
                _apiConfigMock.Object);
        }

        #region GetAllSendsAsync Tests

        [Test]
        public async Task GetAllSendsAsync_ReturnsSends_WhenDataExists()
        {
            // Arrange
            const int subAccountId = 123;
            var response = @"{
                ""value"": [
                    {
                        ""ID"": 1,
                        ""Name"": ""Test Send"",
                        ""CampaignID"": 456,
                        ""Status"": ""Completed"",
                        ""SubStatus"": ""Sent"",
                        ""SendDate"": ""2024-01-01T00:00:00Z"",
                        ""SendCompletedDate"": ""2024-01-01T01:00:00Z"",
                        ""CampaignType"": ""Newsletter"",
                        ""ContactCount"": 100,
                        ""CreatedBy"": ""TestUser"",
                        ""CreatedDate"": ""2024-01-01T00:00:00Z"",
                        ""FromEmail"": ""test@example.com"",
                        ""FromName"": ""Test Sender"",
                        ""ReplyEmail"": ""reply@example.com"",
                        ""SubjectLine"": ""Test Subject""
                    }
                ]
            }";

            _externalApiServiceMock
                .Setup(s => s.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(response);

            // Act
            var result = await _sut.GetAllSendsAsync(subAccountId);

            // Assert
            Assert.That(result, Is.Not.Null);
            var sends = result.ToList();
            Assert.That(sends, Has.Count.EqualTo(1));
            Assert.That(sends[0].ID, Is.EqualTo(1));
            Assert.That(sends[0].Name, Is.EqualTo("Test Send"));
            Assert.That(sends[0].SendCompletedDate, Is.EqualTo("2024-01-01T01:00:00Z"));
            Assert.That(sends[0].ContactCount, Is.EqualTo(100));
        }

        [Test]
        public async Task GetAllSendsAsync_ReturnsEmpty_WhenNoDataExists()
        {
            // Arrange
            const int subAccountId = 123;
            var response = @"{ ""value"": [] }";

            _externalApiServiceMock
                .Setup(s => s.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(response);

            // Act
            var result = await _sut.GetAllSendsAsync(subAccountId);

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetAllSendsAsync_SkipsInvalidRecords_WhenSendCompletedDateIsNull()
        {
            // Arrange
            const int subAccountId = 123;
            var response = @"{
                ""value"": [
                    {
                        ""ID"": 1,
                        ""Name"": ""Test Send"",
                        ""SendCompletedDate"": null
                    },
                    {
                        ""ID"": 2,
                        ""Name"": ""Valid Send"",
                        ""SendCompletedDate"": ""2024-01-01T01:00:00Z""
                    }
                ]
            }";

            _externalApiServiceMock
                .Setup(s => s.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(response);

            // Act
            var result = await _sut.GetAllSendsAsync(subAccountId);

            // Assert
            var sends = result.ToList();
            Assert.That(sends, Has.Count.EqualTo(1));
            Assert.That(sends[0].ID, Is.EqualTo(2));
        }

        [Test]
        public void GetAllSendsAsync_ThrowsException_WhenApiServiceThrows()
        {
            // Arrange
            const int subAccountId = 123;
            _externalApiServiceMock
                .Setup(s => s.GetDataAsync(It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException("API Error"));

            // Act & Assert
            Assert.ThrowsAsync<HttpRequestException>(async () => await _sut.GetAllSendsAsync(subAccountId));
        }

        #endregion

        #region GetUserAgentInfoForSendAsync Tests

        [Test]
        public async Task GetUserAgentInfoForSendAsync_ReturnsUserAgentInfo_WhenDataExists()
        {
            // Arrange
            const int sendId = 1;
            var response = @"{
                ""value"": [
                    {
                        ""ID"": 101,
                        ""SendID"": 1,
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
                .Setup(s => s.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(response);

            // Act
            var result = await _sut.GetUserAgentInfoForSendAsync(sendId);

            // Assert
            Assert.That(result, Is.Not.Null);
            var userAgents = result.ToList();
            Assert.That(userAgents, Has.Count.EqualTo(1));
            Assert.That(userAgents[0].ID, Is.EqualTo(101));
            Assert.That(userAgents[0].ClientName, Is.EqualTo("Gmail"));
            Assert.That(userAgents[0].Device, Is.EqualTo("Desktop"));
        }

        [Test]
        public async Task GetUserAgentInfoForSendAsync_DeduplicatesRecords_WhenDuplicatesExist()
        {
            // Arrange
            const int sendId = 1;
            var response = @"{
                ""value"": [
                    {
                        ""ID"": 101,
                        ""SendID"": 1,
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
                        ""ID"": 101,
                        ""SendID"": 1,
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
                .Setup(s => s.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(response);

            // Act
            var result = await _sut.GetUserAgentInfoForSendAsync(sendId);

            // Assert
            var userAgents = result.ToList();
            Assert.That(userAgents, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task GetUserAgentInfoForSendAsync_HandlesPagination_WhenMultiplePagesExist()
        {
            // Arrange
            const int sendId = 1;
            var firstPageResponse = @"{
                ""value"": [" + string.Join(",", Enumerable.Range(0, PageSize).Select(i => $@"
                    {{
                        ""ID"": {i},
                        ""SendID"": 1,
                        ""IPAddress"": ""192.168.1.{i}"",
                        ""ClientName"": ""Client{i}""
                    }}")) + @"]
            }";

            var secondPageResponse = @"{
                ""value"": [
                    {
                        ""ID"": 100,
                        ""SendID"": 1,
                        ""IPAddress"": ""192.168.1.100"",
                        ""ClientName"": ""Client100""
                    }
                ]
            }";

            _externalApiServiceMock
                .SetupSequence(s => s.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(firstPageResponse)
                .ReturnsAsync(secondPageResponse);

            // Act
            var result = await _sut.GetUserAgentInfoForSendAsync(sendId);

            // Assert
            var userAgents = result.ToList();
            Assert.That(userAgents, Has.Count.GreaterThanOrEqualTo(PageSize + 1));
            _externalApiServiceMock.Verify(s => s.GetDataAsync(It.IsAny<string>()), Times.Exactly(2));
        }

        [Test]
        public async Task GetUserAgentInfoForSendAsync_ReturnsEmpty_WhenNoDataExists()
        {
            // Arrange
            const int sendId = 1;
            var response = @"{ ""value"": [] }";

            _externalApiServiceMock
                .Setup(s => s.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(response);

            // Act
            var result = await _sut.GetUserAgentInfoForSendAsync(sendId);

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetUserAgentInfoForSendAsync_ThrowsException_WhenApiServiceThrows()
        {
            // Arrange
            const int sendId = 1;
            _externalApiServiceMock
                .Setup(s => s.GetDataAsync(It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException("API Error"));

            // Act & Assert
            Assert.ThrowsAsync<HttpRequestException>(async () => await _sut.GetUserAgentInfoForSendAsync(sendId));
        }

        #endregion

        #region GetDisplayedContactsForSendAsync Tests

        [Test]
        public async Task GetDisplayedContactsForSendAsync_ReturnsDisplayedContacts_WhenDataExists()
        {
            // Arrange
            const int sendId = 1;
            var userAgentInfo = new List<UserAgentInfo>
            {
                new UserAgentInfo { ID = 101, ClientName = "Gmail", Device = "Desktop", OperatingSystem = "Windows 10" }
            };

            var response = @"{
                ""value"": [
                    {
                        ""ID"": 1,
                        ""DisplayDate"": ""2024-01-01T10:00:00Z"",
                        ""SendID"": 1,
                        ""CampaignID"": 456,
                        ""UserAgentID"": 101,
                        ""Contact"": { ""Email"": ""test@example.com"" }
                    }
                ]
            }";

            _externalApiServiceMock
                .Setup(s => s.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(response);

            // Act
            var result = await _sut.GetDisplayedContactsForSendAsync(sendId, userAgentInfo);

            // Assert
            Assert.That(result, Is.Not.Null);
            var contacts = result.ToList();
            Assert.That(contacts, Has.Count.EqualTo(1));
            Assert.That(contacts[0].ID, Is.EqualTo(1));
            Assert.That(contacts[0].ContactEmail, Is.EqualTo("test@example.com"));
            Assert.That(contacts[0].ClientName, Is.EqualTo("Gmail"));
        }

        [Test]
        public async Task GetDisplayedContactsForSendAsync_UsesProvidedUserAgentInfo_WhenUserAgentMatches()
        {
            // Arrange
            const int sendId = 1;
            var userAgentInfo = new List<UserAgentInfo>
            {
                new UserAgentInfo
                {
                    ID = 101,
                    ClientName = "Outlook",
                    Device = "Mobile",
                    OperatingSystem = "iOS",
                    IPAddress = "10.0.0.1"
                }
            };

            var response = @"{
                ""value"": [
                    {
                        ""ID"": 1,
                        ""DisplayDate"": ""2024-01-01T10:00:00Z"",
                        ""SendID"": 1,
                        ""UserAgentID"": 101,
                        ""Contact"": { ""Email"": ""test@example.com"" }
                    }
                ]
            }";

            _externalApiServiceMock
                .Setup(s => s.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(response);

            // Act
            var result = await _sut.GetDisplayedContactsForSendAsync(sendId, userAgentInfo);

            // Assert
            var contacts = result.ToList();
            Assert.That(contacts[0].ClientName, Is.EqualTo("Outlook"));
            Assert.That(contacts[0].Device, Is.EqualTo("Mobile"));
            Assert.That(contacts[0].OperatingSystem, Is.EqualTo("iOS"));
            Assert.That(contacts[0].IPAddress, Is.EqualTo("10.0.0.1"));
        }

        [Test]
        public async Task GetDisplayedContactsForSendAsync_ReturnsNullUserAgentFields_WhenUserAgentNotFound()
        {
            // Arrange
            const int sendId = 1;
            var userAgentInfo = new List<UserAgentInfo>();

            var response = @"{
                ""value"": [
                    {
                        ""ID"": 1,
                        ""DisplayDate"": ""2024-01-01T10:00:00Z"",
                        ""SendID"": 1,
                        ""UserAgentID"": 999,
                        ""Contact"": { ""Email"": ""test@example.com"" }
                    }
                ]
            }";

            _externalApiServiceMock
                .Setup(s => s.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(response);

            // Act
            var result = await _sut.GetDisplayedContactsForSendAsync(sendId, userAgentInfo);

            // Assert
            var contacts = result.ToList();
            Assert.That(contacts[0].ClientName, Is.Null);
            Assert.That(contacts[0].Device, Is.Null);
        }

        [Test]
        public async Task GetDisplayedContactsForSendAsync_SkipsInvalidRecords_WhenDisplayDateIsNull()
        {
            // Arrange
            const int sendId = 1;
            var userAgentInfo = new List<UserAgentInfo>();

            var response = @"{
                ""value"": [
                    {
                        ""ID"": 1,
                        ""DisplayDate"": null,
                        ""SendID"": 1
                    },
                    {
                        ""ID"": 2,
                        ""DisplayDate"": ""2024-01-01T10:00:00Z"",
                        ""SendID"": 1
                    }
                ]
            }";

            _externalApiServiceMock
                .Setup(s => s.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(response);

            // Act
            var result = await _sut.GetDisplayedContactsForSendAsync(sendId, userAgentInfo);

            // Assert
            var contacts = result.ToList();
            Assert.That(contacts, Has.Count.EqualTo(1));
            Assert.That(contacts[0].ID, Is.EqualTo(2));
        }

        #endregion

        #region GetClickedLinkContactsForSendAsync Tests

        [Test]
        public async Task GetClickedLinkContactsForSendAsync_ReturnsClickedLinkContacts_WhenDataExists()
        {
            // Arrange
            const int sendId = 1;
            var userAgentInfo = new List<UserAgentInfo>
            {
                new UserAgentInfo { ID = 101, ClientName = "Gmail", Device = "Desktop" }
            };

            var response = @"{
                ""value"": [
                    {
                        ""ID"": 1,
                        ""ClickDate"": ""2024-01-01T11:00:00Z"",
                        ""SendID"": 1,
                        ""CampaignID"": 456,
                        ""UserAgentID"": 101,
                        ""Contact"": { ""Email"": ""test@example.com"" },
                        ""Link"": { ""URL"": ""https://example.com"" }
                    }
                ]
            }";

            _externalApiServiceMock
                .Setup(s => s.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(response);

            // Act
            var result = await _sut.GetClickedLinkContactsForSendAsync(sendId, userAgentInfo);

            // Assert
            var contacts = result.ToList();
            Assert.That(contacts, Has.Count.EqualTo(1));
            Assert.That(contacts[0].ID, Is.EqualTo(1));
            Assert.That(contacts[0].ContactEmail, Is.EqualTo("test@example.com"));
            Assert.That(contacts[0].URL, Is.EqualTo("https://example.com"));
        }

        [Test]
        public async Task GetClickedLinkContactsForSendAsync_SkipsInvalidRecords_WhenClickDateIsNull()
        {
            // Arrange
            const int sendId = 1;
            var userAgentInfo = new List<UserAgentInfo>();

            var response = @"{
                ""value"": [
                    {
                        ""ID"": 1,
                        ""ClickDate"": null,
                        ""SendID"": 1
                    },
                    {
                        ""ID"": 2,
                        ""ClickDate"": ""2024-01-01T11:00:00Z"",
                        ""SendID"": 1
                    }
                ]
            }";

            _externalApiServiceMock
                .Setup(s => s.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(response);

            // Act
            var result = await _sut.GetClickedLinkContactsForSendAsync(sendId, userAgentInfo);

            // Assert
            var contacts = result.ToList();
            Assert.That(contacts, Has.Count.EqualTo(1));
            Assert.That(contacts[0].ID, Is.EqualTo(2));
        }

        #endregion

        #region GetBouncedEmailContactsForSendAsync Tests

        [Test]
        public async Task GetBouncedEmailContactsForSendAsync_ReturnsBouncedContacts_WhenDataExists()
        {
            // Arrange
            const int sendId = 1;
            var response = @"{
                ""value"": [
                    {
                        ""ID"": 1,
                        ""BounceDate"": ""2024-01-01T12:00:00Z"",
                        ""BounceType"": ""Hard"",
                        ""BounceReason"": ""Invalid email"",
                        ""SendID"": 1,
                        ""CampaignID"": 456,
                        ""Contact"": { ""Email"": ""invalid@example.com"" }
                    }
                ]
            }";

            _externalApiServiceMock
                .Setup(s => s.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(response);

            // Act
            var result = await _sut.GetBouncedEmailContactsForSendAsync(sendId);

            // Assert
            var contacts = result.ToList();
            Assert.That(contacts, Has.Count.EqualTo(1));
            Assert.That(contacts[0].BounceType, Is.EqualTo("Hard"));
            Assert.That(contacts[0].BounceReason, Is.EqualTo("Invalid email"));
        }

        [Test]
        public async Task GetBouncedEmailContactsForSendAsync_SkipsInvalidRecords_WhenBounceDataIsNull()
        {
            // Arrange
            const int sendId = 1;
            var response = @"{
                ""value"": [
                    {
                        ""ID"": 1,
                        ""BounceDate"": null,
                        ""SendID"": 1
                    },
                    {
                        ""ID"": 2,
                        ""BounceDate"": ""2024-01-01T12:00:00Z"",
                        ""SendID"": 1
                    }
                ]
            }";

            _externalApiServiceMock
                .Setup(s => s.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(response);

            // Act
            var result = await _sut.GetBouncedEmailContactsForSendAsync(sendId);

            // Assert
            var contacts = result.ToList();
            Assert.That(contacts, Has.Count.EqualTo(1));
            Assert.That(contacts[0].ID, Is.EqualTo(2));
        }

        #endregion

        #region GetUnsubscribedContactsForSendAsync Tests

        [Test]
        public async Task GetUnsubscribedContactsForSendAsync_ReturnsUnsubscribedContacts_WhenDataExists()
        {
            // Arrange
            const int sendId = 1;
            var response = @"{
                ""value"": [
                    {
                        ""ID"": 1,
                        ""UnsubscribedDate"": ""2024-01-01T13:00:00Z"",
                        ""SendID"": 1,
                        ""CampaignID"": 456,
                        ""IsGlobalUnsubscribe"": true,
                        ""IsComplaint"": false,
                        ""Contact"": { ""Email"": ""unsubscribed@example.com"" }
                    }
                ]
            }";

            _externalApiServiceMock
                .Setup(s => s.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(response);

            // Act
            var result = await _sut.GetUnsubscribedContactsForSendAsync(sendId);

            // Assert
            var contacts = result.ToList();
            Assert.That(contacts, Has.Count.EqualTo(1));
            Assert.That(contacts[0].IsGlobalUnsubscribe, Is.True);
            Assert.That(contacts[0].IsComplaint, Is.False);
        }

        [Test]
        public async Task GetUnsubscribedContactsForSendAsync_HandlesPagination_WhenMultiplePagesExist()
        {
            // Arrange
            const int sendId = 1;
            var firstPageResponse = @"{
                ""value"": [" + string.Join(",", Enumerable.Range(0, PageSize).Select(i => $@"
                    {{
                        ""ID"": {i},
                        ""UnsubscribedDate"": ""2024-01-01T13:00:00Z"",
                        ""SendID"": 1
                    }}")) + @"]
            }";

            var secondPageResponse = @"{
                ""value"": [
                    {
                        ""ID"": 100,
                        ""UnsubscribedDate"": ""2024-01-01T13:00:00Z"",
                        ""SendID"": 1
                    }
                ]
            }";

            _externalApiServiceMock
                .SetupSequence(s => s.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(firstPageResponse)
                .ReturnsAsync(secondPageResponse);

            // Act
            var result = await _sut.GetUnsubscribedContactsForSendAsync(sendId);

            // Assert
            var contacts = result.ToList();
            Assert.That(contacts, Has.Count.GreaterThanOrEqualTo(PageSize + 1));
        }

        #endregion
    }
}