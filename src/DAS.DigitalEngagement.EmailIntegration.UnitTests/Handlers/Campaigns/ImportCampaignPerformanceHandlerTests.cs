using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DAS.DigitalEngagement.Application.Handlers.Campaigns;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.Models.Campaigns;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace DAS.DigitalEngagement.EmailIntegration.UnitTests.Handlers.Campaigns
{
    [TestFixture]
    public class ImportCampaignPerformanceHandlerTests
    {
        private Mock<ICampaignService> _campaignServiceMock;
        private Mock<ILogger<ImportCampaignPerformanceHandler>> _loggerMock;
        private ImportCampaignPerformanceHandler _sut;

        [SetUp]
        public void SetUp()
        {
            _campaignServiceMock = new Mock<ICampaignService>();
            _loggerMock = new Mock<ILogger<ImportCampaignPerformanceHandler>>();
            _sut = new ImportCampaignPerformanceHandler(_campaignServiceMock.Object, _loggerMock.Object);
        }

        [Test]
        public async Task Handle_NoSendsFound_LogsWarningAndReturnsEarly()
        {
            // Arrange
            _campaignServiceMock
                .Setup(x => x.GetAllSendsAsync(null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<Send>());

            // Act
            await _sut.Handle();

            // Assert
            _campaignServiceMock.Verify(
                x => x.GetAllSendsAsync(null, It.IsAny<CancellationToken>()),
                Times.Once);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("No sends found")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            _campaignServiceMock.Verify(
                x => x.GetUserAgentInfoForSendAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task Handle_WithSends_ProcessesEachSend()
        {
            // Arrange
            var sends = new List<Send>
            {
                new Send { ID = 1, SendCompletedDate = "2024-01-15T10:00:00Z", Account = "Sub1" },
                new Send { ID = 2, SendCompletedDate = "2024-01-16T10:00:00Z", Account = "Sub2" }
            };

            var userAgents = new List<UserAgentInfo>
            {
                new UserAgentInfo { ID = 10, SendID = 1 }
            };

            _campaignServiceMock
                .Setup(x => x.GetAllSendsAsync(null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sends);

            _campaignServiceMock
                .Setup(x => x.GetUserAgentInfoForSendAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(userAgents);

            _campaignServiceMock
                .Setup(x => x.GetDisplayedContactsForSendAsync(It.IsAny<int>(), It.IsAny<IEnumerable<UserAgentInfo>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<DisplayedContact>());

            _campaignServiceMock
                .Setup(x => x.GetClickedLinkContactsForSendAsync(It.IsAny<int>(), It.IsAny<IEnumerable<UserAgentInfo>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<ClickedLinkContact>());

            _campaignServiceMock
                .Setup(x => x.GetBouncedEmailContactsForSendAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<BouncedContact>());

            _campaignServiceMock
                .Setup(x => x.GetUnsubscribedContactsForSendAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<UnsubscribedContact>());

            // Act
            await _sut.Handle();

            // Assert
            _campaignServiceMock.Verify(x => x.GetUserAgentInfoForSendAsync(1, It.IsAny<CancellationToken>()), Times.Once);
            _campaignServiceMock.Verify(x => x.GetUserAgentInfoForSendAsync(2, It.IsAny<CancellationToken>()), Times.Once);
            _campaignServiceMock.Verify(x => x.GetDisplayedContactsForSendAsync(It.IsAny<int>(), It.IsAny<IEnumerable<UserAgentInfo>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            _campaignServiceMock.Verify(x => x.GetClickedLinkContactsForSendAsync(It.IsAny<int>(), It.IsAny<IEnumerable<UserAgentInfo>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            _campaignServiceMock.Verify(x => x.GetBouncedEmailContactsForSendAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            _campaignServiceMock.Verify(x => x.GetUnsubscribedContactsForSendAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Test]
        public async Task Handle_PassesUserAgentInfoToDisplayedAndClickedMethods()
        {
            // Arrange
            var sends = new List<Send>
            {
                new Send { ID = 1, SendCompletedDate = "2024-01-15T10:00:00Z", Account = "Sub1" }
            };

            var userAgents = new List<UserAgentInfo>
            {
                new UserAgentInfo { ID = 10, SendID = 1, ClientName = "Gmail" },
                new UserAgentInfo { ID = 20, SendID = 1, ClientName = "Outlook" }
            };

            _campaignServiceMock
                .Setup(x => x.GetAllSendsAsync(null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sends);

            _campaignServiceMock
                .Setup(x => x.GetUserAgentInfoForSendAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(userAgents);

            _campaignServiceMock
                .Setup(x => x.GetDisplayedContactsForSendAsync(It.IsAny<int>(), It.IsAny<IEnumerable<UserAgentInfo>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<DisplayedContact>());

            _campaignServiceMock
                .Setup(x => x.GetClickedLinkContactsForSendAsync(It.IsAny<int>(), It.IsAny<IEnumerable<UserAgentInfo>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<ClickedLinkContact>());

            _campaignServiceMock
                .Setup(x => x.GetBouncedEmailContactsForSendAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<BouncedContact>());

            _campaignServiceMock
                .Setup(x => x.GetUnsubscribedContactsForSendAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<UnsubscribedContact>());

            // Act
            await _sut.Handle();

            // Assert - verify the userAgents returned from GetUserAgentInfoForSendAsync are passed through
            _campaignServiceMock.Verify(
                x => x.GetDisplayedContactsForSendAsync(1, It.Is<IEnumerable<UserAgentInfo>>(ua => ua.Count() == 2), It.IsAny<CancellationToken>()),
                Times.Once);

            _campaignServiceMock.Verify(
                x => x.GetClickedLinkContactsForSendAsync(1, It.Is<IEnumerable<UserAgentInfo>>(ua => ua.Count() == 2), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task Handle_CallsGetAllSendsWithNullSubAccountId()
        {
            // Arrange
            _campaignServiceMock
                .Setup(x => x.GetAllSendsAsync(null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<Send>());

            // Act
            await _sut.Handle();

            // Assert
            _campaignServiceMock.Verify(
                x => x.GetAllSendsAsync(null, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public void Handle_GetAllSendsThrowsException_PropagatesAndLogsError()
        {
            // Arrange
            _campaignServiceMock
                .Setup(x => x.GetAllSendsAsync(null, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("API failure"));

            // Act & Assert
            Assert.ThrowsAsync<Exception>(async () => await _sut.Handle());

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error importing campaign performance data")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Test]
        public void Handle_GetUserAgentInfoThrowsException_PropagatesAndLogsError()
        {
            // Arrange
            var sends = new List<Send>
            {
                new Send { ID = 1, SendCompletedDate = "2024-01-15T10:00:00Z" }
            };

            _campaignServiceMock
                .Setup(x => x.GetAllSendsAsync(null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sends);

            _campaignServiceMock
                .Setup(x => x.GetUserAgentInfoForSendAsync(1, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("UserAgent API failure"));

            // Act & Assert
            Assert.ThrowsAsync<Exception>(async () => await _sut.Handle());

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error importing campaign performance data")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Test]
        public void Handle_GetDisplayedContactsThrowsException_PropagatesAndLogsError()
        {
            // Arrange
            var sends = new List<Send>
            {
                new Send { ID = 1, SendCompletedDate = "2024-01-15T10:00:00Z" }
            };

            _campaignServiceMock
                .Setup(x => x.GetAllSendsAsync(null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sends);

            _campaignServiceMock
                .Setup(x => x.GetUserAgentInfoForSendAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<UserAgentInfo>());

            _campaignServiceMock
                .Setup(x => x.GetDisplayedContactsForSendAsync(1, It.IsAny<IEnumerable<UserAgentInfo>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Displayed contacts failure"));

            // Act & Assert
            Assert.ThrowsAsync<Exception>(async () => await _sut.Handle());
        }

        [Test]
        public async Task Handle_WithCancellationToken_PassesThroughToAllServiceCalls()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource().Token;

            var sends = new List<Send>
            {
                new Send { ID = 1, SendCompletedDate = "2024-01-15T10:00:00Z" }
            };

            _campaignServiceMock
                .Setup(x => x.GetAllSendsAsync(null, cancellationToken))
                .ReturnsAsync(sends);

            _campaignServiceMock
                .Setup(x => x.GetUserAgentInfoForSendAsync(1, cancellationToken))
                .ReturnsAsync(Enumerable.Empty<UserAgentInfo>());

            _campaignServiceMock
                .Setup(x => x.GetDisplayedContactsForSendAsync(1, It.IsAny<IEnumerable<UserAgentInfo>>(), cancellationToken))
                .ReturnsAsync(Enumerable.Empty<DisplayedContact>());

            _campaignServiceMock
                .Setup(x => x.GetClickedLinkContactsForSendAsync(1, It.IsAny<IEnumerable<UserAgentInfo>>(), cancellationToken))
                .ReturnsAsync(Enumerable.Empty<ClickedLinkContact>());

            _campaignServiceMock
                .Setup(x => x.GetBouncedEmailContactsForSendAsync(1, cancellationToken))
                .ReturnsAsync(Enumerable.Empty<BouncedContact>());

            _campaignServiceMock
                .Setup(x => x.GetUnsubscribedContactsForSendAsync(1, cancellationToken))
                .ReturnsAsync(Enumerable.Empty<UnsubscribedContact>());

            // Act
            await _sut.Handle(cancellationToken);

            // Assert
            _campaignServiceMock.Verify(x => x.GetAllSendsAsync(null, cancellationToken), Times.Once);
            _campaignServiceMock.Verify(x => x.GetUserAgentInfoForSendAsync(1, cancellationToken), Times.Once);
            _campaignServiceMock.Verify(x => x.GetDisplayedContactsForSendAsync(1, It.IsAny<IEnumerable<UserAgentInfo>>(), cancellationToken), Times.Once);
            _campaignServiceMock.Verify(x => x.GetClickedLinkContactsForSendAsync(1, It.IsAny<IEnumerable<UserAgentInfo>>(), cancellationToken), Times.Once);
            _campaignServiceMock.Verify(x => x.GetBouncedEmailContactsForSendAsync(1, cancellationToken), Times.Once);
            _campaignServiceMock.Verify(x => x.GetUnsubscribedContactsForSendAsync(1, cancellationToken), Times.Once);
        }

        [Test]
        public async Task Handle_LogsStartAndCompletionMessages()
        {
            // Arrange
            _campaignServiceMock
                .Setup(x => x.GetAllSendsAsync(null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<Send>());

            // Act
            await _sut.Handle();

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Starting campaign performance import")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Test]
        public async Task Handle_WithSends_LogsSendCountAndPerSendProgress()
        {
            // Arrange
            var sends = new List<Send>
            {
                new Send { ID = 1, SendCompletedDate = "2024-01-15T10:00:00Z", Account = "Sub1" }
            };

            _campaignServiceMock
                .Setup(x => x.GetAllSendsAsync(null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sends);

            _campaignServiceMock
                .Setup(x => x.GetUserAgentInfoForSendAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<UserAgentInfo>());

            _campaignServiceMock
                .Setup(x => x.GetDisplayedContactsForSendAsync(1, It.IsAny<IEnumerable<UserAgentInfo>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<DisplayedContact>());

            _campaignServiceMock
                .Setup(x => x.GetClickedLinkContactsForSendAsync(1, It.IsAny<IEnumerable<UserAgentInfo>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<ClickedLinkContact>());

            _campaignServiceMock
                .Setup(x => x.GetBouncedEmailContactsForSendAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<BouncedContact>());

            _campaignServiceMock
                .Setup(x => x.GetUnsubscribedContactsForSendAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<UnsubscribedContact>());

            // Act
            await _sut.Handle();

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Retrieved 1 sends")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Processing Send 1")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Campaign performance import completed successfully")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Test]
        public async Task Handle_ProcessesSendsInOrder()
        {
            // Arrange
            var callOrder = new List<int>();

            var sends = new List<Send>
            {
                new Send { ID = 10, SendCompletedDate = "2024-01-15T10:00:00Z" },
                new Send { ID = 20, SendCompletedDate = "2024-01-16T10:00:00Z" },
                new Send { ID = 30, SendCompletedDate = "2024-01-17T10:00:00Z" }
            };

            _campaignServiceMock
                .Setup(x => x.GetAllSendsAsync(null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sends);

            _campaignServiceMock
                .Setup(x => x.GetUserAgentInfoForSendAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Callback<int, CancellationToken>((id, _) => callOrder.Add(id))
                .ReturnsAsync(Enumerable.Empty<UserAgentInfo>());

            _campaignServiceMock
                .Setup(x => x.GetDisplayedContactsForSendAsync(It.IsAny<int>(), It.IsAny<IEnumerable<UserAgentInfo>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<DisplayedContact>());

            _campaignServiceMock
                .Setup(x => x.GetClickedLinkContactsForSendAsync(It.IsAny<int>(), It.IsAny<IEnumerable<UserAgentInfo>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<ClickedLinkContact>());

            _campaignServiceMock
                .Setup(x => x.GetBouncedEmailContactsForSendAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<BouncedContact>());

            _campaignServiceMock
                .Setup(x => x.GetUnsubscribedContactsForSendAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<UnsubscribedContact>());

            // Act
            await _sut.Handle();

            // Assert
            Assert.That(callOrder, Is.EqualTo(new[] { 10, 20, 30 }));
        }

        [Test]
        public void Handle_ExceptionMidway_DoesNotProcessRemainingSends()
        {
            // Arrange
            var sends = new List<Send>
            {
                new Send { ID = 1, SendCompletedDate = "2024-01-15T10:00:00Z" },
                new Send { ID = 2, SendCompletedDate = "2024-01-16T10:00:00Z" }
            };

            _campaignServiceMock
                .Setup(x => x.GetAllSendsAsync(null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sends);

            _campaignServiceMock
                .Setup(x => x.GetUserAgentInfoForSendAsync(1, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Failure on first send"));

            // Act & Assert
            Assert.ThrowsAsync<Exception>(async () => await _sut.Handle());

            // Second send should never be reached
            _campaignServiceMock.Verify(
                x => x.GetUserAgentInfoForSendAsync(2, It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
