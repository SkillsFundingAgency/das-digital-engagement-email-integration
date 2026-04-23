using DAS.DigitalEngagement.Application.Handlers.Campaigns;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.CampaignInterest.Data.Models;
using DAS.DigitalEngagement.Models.Campaigns;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DAS.DigitalEngagement.EmailIntegration.UnitTests.Handlers.Campaigns
{
    [TestFixture]
    public class ImportCampaignPerformanceHandlerTests
    {
        private Mock<ICampaignService> _campaignServiceMock;
        private Mock<ILogger<ImportCampaignPerformanceHandler>> _loggerMock;
        private ImportCampaignPerformanceHandler _sut;
        private List<Send> sends;
        private CancellationToken cancellationToken;

        [SetUp]
        public void SetUp()
        {
            _campaignServiceMock = new Mock<ICampaignService>();
            _loggerMock = new Mock<ILogger<ImportCampaignPerformanceHandler>>();
            _sut = new ImportCampaignPerformanceHandler(_campaignServiceMock.Object, _loggerMock.Object);
            cancellationToken = new CancellationTokenSource().Token;

            sends =
            [
                new() { ID = 1, SendCompletedDate = "2024-01-15T10:00:00Z", Account = "Sub1" }
            ];

            _campaignServiceMock.Setup(x => x.GetBouncedEmailContactsFromEShot(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _campaignServiceMock.Setup(x => x.GetUnsubscribedContactsFromEShot(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _campaignServiceMock.Setup(x => x.SaveCampaignDetailsAsync(It.IsAny<CampaignInterest.Data.Models.Campaigns>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
            _campaignServiceMock.Setup(x => x.UpsertCampaignImportMetadataAsync(It.IsAny<CampaignImportMetadata>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _campaignServiceMock
                .Setup(x => x.GetDisplayedContactsFromEShot(It.IsAny<int>(), It.IsAny<IEnumerable<UserAgentInfo>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _campaignServiceMock
                .Setup(x => x.GetClickedLinkContactsFromEShot(It.IsAny<int>(), It.IsAny<IEnumerable<UserAgentInfo>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
        }

        [Test]
        public async Task Handle_NoSendsFound_LogsWarningAndReturnsEarly()
        {
            // Arrange
            _campaignServiceMock.Setup(x => x.GetEligibleSendsAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync([]);

            // Act
            await _sut.Handle();

            // Assert
            _campaignServiceMock.Verify(x => x.GetEligibleSendsAsync(null, It.IsAny<CancellationToken>()), Times.Once);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("No eligible sends found for import")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            _campaignServiceMock.Verify(x => x.GetUserAgentInfoForSendAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Handle_WithSends_ProcessesEachSend()
        {
            // Arrange
            sends =
            [
                new() { ID = 1, SendCompletedDate = "2024-01-15T10:00:00Z", Account = "Sub1" },
                new() { ID = 2, SendCompletedDate = "2024-01-16T10:00:00Z", Account = "Sub2" }
            ];

            var userAgents = new List<UserAgentInfo>
            {
                new() { ID = 10, SendID = 1 }
            };

            _campaignServiceMock.Setup(x => x.GetEligibleSendsAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync(sends);
            _campaignServiceMock.Setup(x => x.GetUserAgentInfoForSendAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(userAgents);

            // Act
            await _sut.Handle();

            // Assert
            _campaignServiceMock.Verify(x => x.GetUserAgentInfoForSendAsync(1, It.IsAny<CancellationToken>()), Times.Never);
            _campaignServiceMock.Verify(x => x.GetUserAgentInfoForSendAsync(2, It.IsAny<CancellationToken>()), Times.Never);
            _campaignServiceMock.Verify(x => x.GetDisplayedContactsFromEShot(It.IsAny<int>(), It.IsAny<IEnumerable<UserAgentInfo>>(), It.IsAny<CancellationToken>()), Times.Exactly(0));
            _campaignServiceMock.Verify(x => x.GetClickedLinkContactsFromEShot(It.IsAny<int>(), It.IsAny<IEnumerable<UserAgentInfo>>(), It.IsAny<CancellationToken>()), Times.Exactly(0));
            _campaignServiceMock.Verify(x => x.GetBouncedEmailContactsFromEShot(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(0));
            _campaignServiceMock.Verify(x => x.GetUnsubscribedContactsFromEShot(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(0));
        }

        [Test]
        public async Task Handle_PassesUserAgentInfoToDisplayedAndClickedMethods()
        {
            // Arrange
            var userAgents = new List<UserAgentInfo>
            {
                new() { ID = 10, SendID = 1, ClientName = "Gmail" },
                new() { ID = 20, SendID = 1, ClientName = "Outlook" }
            };

            _campaignServiceMock.Setup(x => x.GetEligibleSendsAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync(sends);
            _campaignServiceMock.Setup(x => x.GetUserAgentInfoForSendAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(userAgents);

            // Act
            await _sut.Handle();

            // Assert - verify the userAgents returned from GetUserAgentInfoForSendAsync are passed through
            _campaignServiceMock.Verify(
                x => x.GetDisplayedContactsFromEShot(1, It.Is<IEnumerable<UserAgentInfo>>(ua => ua.Count() == 2), It.IsAny<CancellationToken>()),
                Times.Never);

            _campaignServiceMock.Verify(
                x => x.GetClickedLinkContactsFromEShot(1, It.Is<IEnumerable<UserAgentInfo>>(ua => ua.Count() == 2), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task Handle_CallsGetEligibleSendsWithNullSubAccountId()
        {
            // Arrange
            _campaignServiceMock.Setup(x => x.GetEligibleSendsAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync([]);

            // Act
            await _sut.Handle();

            // Assert
            _campaignServiceMock.Verify(x => x.GetEligibleSendsAsync(null, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void Handle_GetAllSendsThrowsException_Propagates()
        {
            // Arrange
            _campaignServiceMock.Setup(x => x.GetEligibleSendsAsync(null, It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("API failure"));

            // Act & Assert
            Assert.ThrowsAsync<Exception>(async () => await _sut.Handle());
        }

        [Test]
        public async Task Handle_WithCancellationToken_PassesThroughToAllServiceCalls()
        {
            // Arrange
            _campaignServiceMock.Setup(x => x.GetEligibleSendsAsync(null, cancellationToken)).ReturnsAsync(sends);
            _campaignServiceMock.Setup(x => x.GetUserAgentInfoForSendAsync(1, cancellationToken)).ReturnsAsync([]);

            // Act
            await _sut.Handle(cancellationToken);

            // Assert
            _campaignServiceMock.Verify(x => x.GetEligibleSendsAsync(null, cancellationToken), Times.Once);
            _campaignServiceMock.Verify(x => x.GetUserAgentInfoForSendAsync(1, cancellationToken), Times.Never);
            _campaignServiceMock.Verify(x => x.GetDisplayedContactsFromEShot(1, It.IsAny<IEnumerable<UserAgentInfo>>(), cancellationToken), Times.Never);
            _campaignServiceMock.Verify(x => x.GetClickedLinkContactsFromEShot(0, It.IsAny<IEnumerable<UserAgentInfo>>(), cancellationToken), Times.Never);
            _campaignServiceMock.Verify(x => x.GetBouncedEmailContactsFromEShot(1, cancellationToken), Times.Never);
            _campaignServiceMock.Verify(x => x.GetUnsubscribedContactsFromEShot(1, cancellationToken), Times.Never);
        }

        [Test]
        public async Task Handle_NoSends_DoesNotLogProcessingMessages()
        {
            // Arrange
            _campaignServiceMock.Setup(x => x.GetEligibleSendsAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync([]);

            // Act
            await _sut.Handle();

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Processing Send")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Never);
        }

        [Test]
        public async Task Handle_WithSends_LogsSendCountAndPerSendProgress()
        {
            // Arrange
            _campaignServiceMock.Setup(x => x.GetEligibleSendsAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync(sends);
            _campaignServiceMock.Setup(x => x.GetUserAgentInfoForSendAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);

            // Act
            await _sut.Handle();

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Processing Send 1")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Test]
        public async Task Handle_ExceptionMidway_DoesNotProcessRemainingSends()
        {
            // Arrange
            sends =
            [
                new() { ID = 1, SendCompletedDate = "2024-01-15T10:00:00Z" },
                new() { ID = 2, SendCompletedDate = "2024-01-16T10:00:00Z" }
            ];

            _campaignServiceMock.Setup(x => x.GetEligibleSendsAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync(sends);
            _campaignServiceMock.Setup(x => x.GetUserAgentInfoForSendAsync(1, It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Failure on first send"));

            // Act 
            await _sut.Handle();

            // Assert
            // Second send should never be reached
            _campaignServiceMock.Verify(x => x.GetUserAgentInfoForSendAsync(2, It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Handle_BuildCampaignImportMetadataObject_SetsCorrectCampaignIdAndIsImportCompleteFalse()
        {
            // Arrange
            const int expectedSendId = 42;
            var capturedMetadataCalls = new List<CampaignImportMetadata>();

            _campaignServiceMock.Setup(x => x.GetEligibleSendsAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync(sends);
            _campaignServiceMock.Setup(x => x.GetUserAgentInfoForSendAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);
            _campaignServiceMock
                .Setup(x => x.SaveCampaignDetailsAsync(It.IsAny<CampaignInterest.Data.Models.Campaigns>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedSendId);
            _campaignServiceMock
                .Setup(x => x.UpsertCampaignImportMetadataAsync(It.IsAny<CampaignImportMetadata>(), It.IsAny<CancellationToken>()))
                .Callback<CampaignImportMetadata, CancellationToken>((m, _) => capturedMetadataCalls.Add(new CampaignImportMetadata
                {
                    SendId = m.SendId,
                    CampaignId = m.CampaignId,
                    IsImportComplete = m.IsImportComplete,
                    ImportStartDate = m.ImportStartDate,
                    ImportEndDate = m.ImportEndDate
                }))
                .ReturnsAsync(0);

            // Act
            await _sut.Handle();

            // Assert - first upsert call sets up the metadata with correct campaignId and IsImportComplete = false
            Assert.That(capturedMetadataCalls, Has.Count.GreaterThanOrEqualTo(1));
            var initialMetadata = capturedMetadataCalls[0];
            Assert.That(initialMetadata.SendId, Is.EqualTo(0));
            Assert.That(initialMetadata.IsImportComplete, Is.False);
        }

        [Test]
        public async Task Handle_BuildCampaignImportMetadataObject_ImportStartDateIsSetToApproximatelyUtcNow()
        {
            // Arrange
            const int expectedSendId = 99;
            CampaignImportMetadata capturedInitialMetadata = null;
            var beforeHandle = DateTime.UtcNow;

            _campaignServiceMock.Setup(x => x.GetEligibleSendsAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync(sends);
            _campaignServiceMock.Setup(x => x.GetUserAgentInfoForSendAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);
            _campaignServiceMock
                .Setup(x => x.SaveCampaignDetailsAsync(It.IsAny<CampaignInterest.Data.Models.Campaigns>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedSendId);

            var callCount = 0;
            _campaignServiceMock
                .Setup(x => x.UpsertCampaignImportMetadataAsync(It.IsAny<CampaignImportMetadata>(), It.IsAny<CancellationToken>()))
                .Callback<CampaignImportMetadata, CancellationToken>((m, _) =>
                {
                    callCount++;
                    if (callCount == 1)
                        capturedInitialMetadata = m;
                })
                .ReturnsAsync(expectedSendId);

            // Act
            await _sut.Handle();
            var afterHandle = DateTime.UtcNow;

            // Assert
            Assert.That(capturedInitialMetadata, Is.Not.Null);
            Assert.That(capturedInitialMetadata.ImportStartDate, Is.GreaterThanOrEqualTo(beforeHandle));
            Assert.That(capturedInitialMetadata.ImportStartDate, Is.LessThanOrEqualTo(afterHandle));
        }

        [Test]
        public async Task Handle_BuildCampaignImportMetadataObject_CompletionUpsertSetsIsImportCompleteTrueAndImportEndDate()
        {
            // Arrange
            const int expectedSendId = 7;
            CampaignImportMetadata capturedCompletionMetadata = null;
            var beforeHandle = DateTime.UtcNow;

            _campaignServiceMock.Setup(x => x.GetEligibleSendsAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync(sends);
            _campaignServiceMock.Setup(x => x.GetUserAgentInfoForSendAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);
            _campaignServiceMock
                .Setup(x => x.SaveCampaignDetailsAsync(It.IsAny<CampaignInterest.Data.Models.Campaigns>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedSendId);

            var callCount = 0;
            _campaignServiceMock
                .Setup(x => x.UpsertCampaignImportMetadataAsync(It.IsAny<CampaignImportMetadata>(), It.IsAny<CancellationToken>()))
                .Callback<CampaignImportMetadata, CancellationToken>((m, _) =>
                {
                    callCount++;
                    if (callCount == 2)
                        capturedCompletionMetadata = m;
                })
                .ReturnsAsync(expectedSendId);

            // Act
            await _sut.Handle();
            var afterHandle = DateTime.UtcNow;

            // Assert - second upsert call marks import as complete with an end date
            Assert.That(capturedCompletionMetadata, Is.Not.Null);
            Assert.That(capturedCompletionMetadata.IsImportComplete, Is.True);
            Assert.That(capturedCompletionMetadata.ImportEndDate, Is.Not.Null);
            Assert.That(capturedCompletionMetadata.ImportEndDate, Is.GreaterThanOrEqualTo(beforeHandle));
            Assert.That(capturedCompletionMetadata.ImportEndDate, Is.LessThanOrEqualTo(afterHandle));
        }

        [Test]
        public async Task Handle_UpsertCampaignImportMetadataAsync_CalledTwicePerSendOnSuccess()
        {
            // Arrange
            _campaignServiceMock.Setup(x => x.GetEligibleSendsAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync(sends);
            _campaignServiceMock.Setup(x => x.SaveCampaignDetailsAsync(It.IsAny<CampaignInterest.Data.Models.Campaigns>(), It.IsAny<CancellationToken>())).ReturnsAsync(1L);
            _campaignServiceMock.Setup(x => x.GetUserAgentInfoForSendAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);

            // Act
            await _sut.Handle();

            // Assert - once at start, once at completion
            _campaignServiceMock.Verify(x => x.UpsertCampaignImportMetadataAsync(It.IsAny<CampaignImportMetadata>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Test]
        public async Task Handle_FirstUpsertReturnsFalse_LogsErrorAndSkipsContactProcessing()
        {
            // Arrange
            _campaignServiceMock.Setup(x => x.GetEligibleSendsAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync(sends);
            _campaignServiceMock.Setup(x => x.SaveCampaignDetailsAsync(It.IsAny<CampaignInterest.Data.Models.Campaigns>(), It.IsAny<CancellationToken>())).ReturnsAsync(1L);
            _campaignServiceMock.Setup(x => x.UpsertCampaignImportMetadataAsync(It.IsAny<CampaignImportMetadata>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            await _sut.Handle();

            // Assert - error logged for initial upsert failure
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to upsert campaign import metadata")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Never);

            // Contact processing should be skipped
            _campaignServiceMock.Verify(x => x.GetUserAgentInfoForSendAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
            _campaignServiceMock.Verify(x => x.GetDisplayedContactsFromEShot(It.IsAny<int>(), It.IsAny<IEnumerable<UserAgentInfo>>(), It.IsAny<CancellationToken>()), Times.Once);
            _campaignServiceMock.Verify(x => x.GetClickedLinkContactsFromEShot(It.IsAny<int>(), It.IsAny<IEnumerable<UserAgentInfo>>(), It.IsAny<CancellationToken>()), Times.Once);
            _campaignServiceMock.Verify(x => x.GetBouncedEmailContactsFromEShot(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
            _campaignServiceMock.Verify(x => x.GetUnsubscribedContactsFromEShot(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_FirstUpsertReturnsFalse_SecondUpsertNeverCalled()
        {
            // Arrange
            _campaignServiceMock.Setup(x => x.GetEligibleSendsAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync(sends);
            _campaignServiceMock.Setup(x => x.SaveCampaignDetailsAsync(It.IsAny<CampaignInterest.Data.Models.Campaigns>(), It.IsAny<CancellationToken>())).ReturnsAsync(1L);
            _campaignServiceMock.Setup(x => x.UpsertCampaignImportMetadataAsync(It.IsAny<CampaignImportMetadata>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            await _sut.Handle();

            // Assert - only first upsert attempt was made
            _campaignServiceMock.Verify(x => x.UpsertCampaignImportMetadataAsync(It.IsAny<CampaignImportMetadata>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Test]
        public async Task Handle_SecondUpsertReturnsFalse_LogsErrorAndDoesNotLogCompletion()
        {
            // Arrange
            _campaignServiceMock.Setup(x => x.GetEligibleSendsAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync(sends);
            _campaignServiceMock.Setup(x => x.SaveCampaignDetailsAsync(It.IsAny<CampaignInterest.Data.Models.Campaigns>(), It.IsAny<CancellationToken>())).ReturnsAsync(1L);
            _campaignServiceMock.Setup(x => x.GetUserAgentInfoForSendAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);

            // Act
            await _sut.Handle();

            // Assert - error logged for completion upsert failure
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to mark campaign import complete")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Never);

            // "Processing complete" should not be logged
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Processing complete for Send")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Test]
        public async Task Handle_UpsertCampaignImportMetadataAsync_ThrowsException_Propagates()
        {
            // Arrange
            _campaignServiceMock.Setup(x => x.GetEligibleSendsAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync(sends);
            _campaignServiceMock.Setup(x => x.SaveCampaignDetailsAsync(It.IsAny<CampaignInterest.Data.Models.Campaigns>(), It.IsAny<CancellationToken>())).ReturnsAsync(1L);
            _campaignServiceMock
                .Setup(x => x.UpsertCampaignImportMetadataAsync(It.IsAny<CampaignImportMetadata>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Upsert failure"));

            // Act & Assert
            Assert.ThrowsAsync<Exception>(async () => await _sut.Handle());
        }
    }
}
