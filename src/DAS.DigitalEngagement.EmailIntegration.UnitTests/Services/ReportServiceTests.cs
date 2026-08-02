using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using DAS.DigitalEngagement.Application.Services;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.Models.Import;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace DAS.DigitalEngagement.Application.Services.UnitTests
{
    [TestFixture]
    public class ReportServiceTests
    {
        [Test]
        public void Constructor_WithValidDependencies_DoesNotThrow()
        {
            var mockBlobServiceClient = new Mock<BlobServiceClient>();
            var mockLogger = new Mock<ILogger<ReportService>>();
            var mockEmailNotificationService = new Mock<IEmailNotificationService>();

            Assert.DoesNotThrow(() => new ReportService(mockBlobServiceClient.Object, mockLogger.Object, mockEmailNotificationService.Object));
        }

        [Test]
        public void Constructor_WithNullBlobServiceClient_DoesNotThrow()
        {
            var mockLogger = new Mock<ILogger<ReportService>>();
            var mockEmailNotificationService = new Mock<IEmailNotificationService>();

            Assert.DoesNotThrow(() => new ReportService(null, mockLogger.Object, mockEmailNotificationService.Object));
        }

        [Test]
        public void Constructor_WithNullLogger_DoesNotThrow()
        {
            var mockBlobServiceClient = new Mock<BlobServiceClient>();
            var mockEmailNotificationService = new Mock<IEmailNotificationService>();

            Assert.DoesNotThrow(() => new ReportService(mockBlobServiceClient.Object, null, mockEmailNotificationService.Object));
        }

        [Test]
        public void Constructor_WithBothParametersNull_DoesNotThrow()
        {
            var mockEmailNotificationService = new Mock<IEmailNotificationService>();

            Assert.DoesNotThrow(() => new ReportService(null, null, mockEmailNotificationService.Object));
        }

        [Test]
        public async Task SaveReportToBlob_ValidInputs_SavesSuccessfullyAndLogs()
        {
            var reportContent = "Test report content";
            var fileName = "test-report";
            var expectedBlobPath = "Report/test-report.report.txt";
            var expectedUrl = "https://test.blob.core.windows.net/container/Report/test-report.report.txt";

            var mockBlobServiceClient = new Mock<BlobServiceClient>();
            var mockLogger = new Mock<ILogger<ReportService>>();
            var mockEmailNotificationService = new Mock<IEmailNotificationService>();
            var mockBlobContainerClient = new Mock<BlobContainerClient>();
            var mockBlobClient = new Mock<BlobClient>();

            mockBlobServiceClient
                .Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
                .Returns(mockBlobContainerClient.Object);

            mockBlobContainerClient
                .Setup(x => x.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), It.IsAny<System.Collections.Generic.IDictionary<string, string>>(), It.IsAny<BlobContainerEncryptionScopeOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<Response<BlobContainerInfo>>());

            mockBlobContainerClient
                .Setup(x => x.GetBlobClient(expectedBlobPath))
                .Returns(mockBlobClient.Object);

            mockBlobClient
               .Setup(x => x.Uri)
               .Returns(new Uri(expectedUrl));

            mockBlobClient
                .Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

            var service = new ReportService(mockBlobServiceClient.Object, mockLogger.Object, mockEmailNotificationService.Object);

            var result = await service.SaveReportToBlobInternalAsync(reportContent, fileName);

            mockBlobServiceClient.Verify(x => x.GetBlobContainerClient("email-integration-to-marketing-tool"), Times.Once);
            mockBlobContainerClient.Verify(x => x.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), It.IsAny<System.Collections.Generic.IDictionary<string, string>>(), It.IsAny<BlobContainerEncryptionScopeOptions>(), It.IsAny<CancellationToken>()), Times.Once);
            mockBlobContainerClient.Verify(x => x.GetBlobClient(expectedBlobPath), Times.Once);
            mockBlobClient.Verify(x => x.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()), Times.Once);

            Assert.That(result, Is.EqualTo(expectedUrl));
        }

        [Test]
        public async Task SaveReportToBlob_EmptyReportContent_SavesSuccessfully()
        {
            var reportContent = string.Empty;
            var fileName = "empty-report";

            var mockBlobServiceClient = new Mock<BlobServiceClient>();
            var mockLogger = new Mock<ILogger<ReportService>>();
            var mockEmailNotificationService = new Mock<IEmailNotificationService>();
            var mockBlobContainerClient = new Mock<BlobContainerClient>();
            var mockBlobClient = new Mock<BlobClient>();

            mockBlobServiceClient
                .Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
                .Returns(mockBlobContainerClient.Object);

            mockBlobContainerClient
                .Setup(x => x.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), It.IsAny<System.Collections.Generic.IDictionary<string, string>>(), It.IsAny<BlobContainerEncryptionScopeOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<Response<BlobContainerInfo>>());

            mockBlobContainerClient
                .Setup(x => x.GetBlobClient(It.IsAny<string>()))
                .Returns(mockBlobClient.Object);

            mockBlobClient
                .Setup(x => x.Uri)
                .Returns(new Uri("https://test.blob.core.windows.net/container/Report/test-report.report.txt"));

            mockBlobClient
                .Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

            var service = new ReportService(mockBlobServiceClient.Object, mockLogger.Object, mockEmailNotificationService.Object);

            await service.SaveReportToBlobInternalAsync(reportContent, fileName);

            mockBlobClient.Verify(x => x.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void SaveReportToBlob_CreateContainerThrowsException_LogsErrorAndRethrows()
        {
            var reportContent = "Test content";
            var fileName = "test-report";
            var expectedException = new RequestFailedException("Container creation failed");

            var mockBlobServiceClient = new Mock<BlobServiceClient>();
            var mockLogger = new Mock<ILogger<ReportService>>();
            var mockEmailNotificationService = new Mock<IEmailNotificationService>();
            var mockBlobContainerClient = new Mock<BlobContainerClient>();

            mockBlobServiceClient
                .Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
                .Returns(mockBlobContainerClient.Object);

            mockBlobContainerClient
                .Setup(x => x.CreateIfNotExistsAsync(
                    It.IsAny<PublicAccessType>(),
                    It.IsAny<IDictionary<string, string>>(),
                    It.IsAny<BlobContainerEncryptionScopeOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            var service = new ReportService(
                mockBlobServiceClient.Object,
                mockLogger.Object,
                mockEmailNotificationService.Object);

            var thrownException = Assert.ThrowsAsync<InvalidOperationException>(
                async () => await service.SaveReportToBlobInternalAsync(reportContent, fileName));

            Assert.That(thrownException, Is.Not.Null);

            Assert.That(
                thrownException!.Message,
                Is.EqualTo($"Failed to save report file '{fileName}' to blob storage."));

            Assert.That(
                thrownException.InnerException,
                Is.EqualTo(expectedException));

            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString()!.Contains($"Failed to save report file '{fileName}' to blob storage container")),
                    It.Is<Exception>(ex => ex == expectedException),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Test]
        public async Task SaveReportToBlob_WhitespaceReportContent_SavesSuccessfully()
        {
            var reportContent = "   \t\n  ";
            var fileName = "whitespace-report";

            var mockBlobServiceClient = new Mock<BlobServiceClient>();
            var mockLogger = new Mock<ILogger<ReportService>>();
            var mockEmailNotificationService = new Mock<IEmailNotificationService>();
            var mockBlobContainerClient = new Mock<BlobContainerClient>();
            var mockBlobClient = new Mock<BlobClient>();

            mockBlobServiceClient
                .Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
                .Returns(mockBlobContainerClient.Object);

            mockBlobContainerClient
                .Setup(x => x.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), It.IsAny<System.Collections.Generic.IDictionary<string, string>>(), It.IsAny<BlobContainerEncryptionScopeOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<Response<BlobContainerInfo>>());

            mockBlobContainerClient
                .Setup(x => x.GetBlobClient(It.IsAny<string>()))
                .Returns(mockBlobClient.Object);

            mockBlobClient
                .Setup(x => x.Uri)
                .Returns(new Uri("https://test.blob.core.windows.net/container/Report/test-report.report.txt"));

            mockBlobClient
                .Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

            var service = new ReportService(mockBlobServiceClient.Object, mockLogger.Object, mockEmailNotificationService.Object);

            await service.SaveReportToBlobInternalAsync(reportContent, fileName);

            mockBlobClient.Verify(x => x.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task SaveReportToBlob_EmptyFileName_SavesWithCorrectPath()
        {
            var reportContent = "Test content";
            var fileName = string.Empty;
            var expectedBlobPath = "Report/.report.txt";

            var mockBlobServiceClient = new Mock<BlobServiceClient>();
            var mockLogger = new Mock<ILogger<ReportService>>();
            var mockEmailNotificationService = new Mock<IEmailNotificationService>();
            var mockBlobContainerClient = new Mock<BlobContainerClient>();
            var mockBlobClient = new Mock<BlobClient>();

            mockBlobServiceClient
                .Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
                .Returns(mockBlobContainerClient.Object);

            mockBlobContainerClient
                .Setup(x => x.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(),
                            It.IsAny<System.Collections.Generic.IDictionary<string, string>>(),
                            It.IsAny<BlobContainerEncryptionScopeOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<Response<BlobContainerInfo>>());

            mockBlobContainerClient
                .Setup(x => x.GetBlobClient(expectedBlobPath))
                .Returns(mockBlobClient.Object);

            mockBlobClient
             .Setup(x => x.Uri)
             .Returns(new Uri("https://test.blob.core.windows.net/container/Report/test-report.report.txt"));

            mockBlobClient
                .Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

            var service = new ReportService(mockBlobServiceClient.Object, mockLogger.Object, mockEmailNotificationService.Object);

            await service.SaveReportToBlobInternalAsync(reportContent, fileName);

            mockBlobContainerClient.Verify(x => x.GetBlobClient(expectedBlobPath), Times.Once);
        }
        private Mock<Azure.Storage.Blobs.BlobServiceClient> _blobServiceClientMock;
        private Mock<ILogger<ReportService>> _loggerMock;
        private ReportService _reportService;

        [SetUp]
        public void SetUp()
        {
            _blobServiceClientMock = new Mock<Azure.Storage.Blobs.BlobServiceClient>();
            _loggerMock = new Mock<ILogger<ReportService>>();
            var mockEmailNotificationService = new Mock<IEmailNotificationService>();
            _reportService = new ReportService(_blobServiceClientMock.Object, _loggerMock.Object, mockEmailNotificationService.Object);
        }

        [Test]
        public void CreateImportSummaryReport_WithBatchResultsAndMessages_GeneratesCompleteReport()
        {
            var summary = new ImportSummaryResult
            {
                Status = BatchStatus.Completed,
                StartTime = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2024, 1, 15, 10, 45, 0, DateTimeKind.Utc),
                TotalRecordsFromDb = 100,
                BatchResults = new List<BatchResultDetail>
                {
                    new BatchResultDetail
                    {
                        BatchId = "batch-001",
                        Status = BatchStatus.Completed,
                        RecordsProcessed = 50,
                        TokenFromEshot = "token-123",
                        Error = null
                    },
                    new BatchResultDetail
                    {
                        BatchId = "batch-002",
                        Status = BatchStatus.Completed,
                        RecordsProcessed = 50,
                        TokenFromEshot = "token-456",
                        Error = null
                    }
                },
                Messages = new List<string> { "Import started", "Import completed successfully" }
            };

            var result = _reportService.CreateImportSummaryReport(summary);

            Assert.That(result, Does.Contain("Status: Completed"));
            Assert.That(result, Does.Contain("Start Time: 2024-01-15T10:30:00.0000000"));
            Assert.That(result, Does.Contain("End Time: 2024-01-15T10:45:00.0000000"));
            Assert.That(result, Does.Contain("Total Records From DB: 100"));
            Assert.That(result, Does.Contain("Total Records Processed: 100"));
            Assert.That(result, Does.Contain("Batch Results (2):"));
            Assert.That(result, Does.Contain("Batch 1:"));
            Assert.That(result, Does.Contain("BatchId: batch-001"));
            Assert.That(result, Does.Contain("Status: Completed"));
            Assert.That(result, Does.Contain("Records Processed: 50"));
            Assert.That(result, Does.Contain("Token: token-123"));
            Assert.That(result, Does.Contain("Batch 2:"));
            Assert.That(result, Does.Contain("BatchId: batch-002"));
            Assert.That(result, Does.Contain("Token: token-456"));
            Assert.That(result, Does.Contain("Messages:"));
            Assert.That(result, Does.Contain("- Import started"));
            Assert.That(result, Does.Contain("- Import completed successfully"));
        }

        [Test]
        public void CreateImportSummaryReport_WithNullBatchResults_DisplaysNoBatchResultsMessage()
        {
            var summary = new ImportSummaryResult
            {
                Status = BatchStatus.Completed,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                TotalRecordsFromDb = 0,
                BatchResults = new List<BatchResultDetail>(),
                Messages = new List<string>()
            };

            var result = _reportService.CreateImportSummaryReport(summary);

            Assert.That(result, Does.Contain("No batch results available."));
            Assert.That(result, Does.Not.Contain("Batch Results ("));
        }

        [Test]
        public void CreateImportSummaryReport_WithEmptyBatchResults_DisplaysNoBatchResultsMessage()
        {
            var summary = new ImportSummaryResult
            {
                Status = BatchStatus.Failed,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                TotalRecordsFromDb = 0,
                BatchResults = new List<BatchResultDetail>(),
                Messages = new List<string>()
            };

            var result = _reportService.CreateImportSummaryReport(summary);

            Assert.That(result, Does.Contain("No batch results available."));
            Assert.That(result, Does.Not.Contain("Batch Results ("));
        }

        [Test]
        public void CreateImportSummaryReport_WithNullMessages_DisplaysNoMessagesMessage()
        {
            var summary = new ImportSummaryResult
            {
                Status = BatchStatus.Partial,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                TotalRecordsFromDb = 5,
                BatchResults = new List<BatchResultDetail>(),
                Messages = null
            };

            var result = _reportService.CreateImportSummaryReport(summary);

            Assert.That(result, Does.Contain("No messages."));
            Assert.That(result, Does.Not.Contain("Messages:"));
        }

        [Test]
        public void CreateImportSummaryReport_WithEmptyMessages_DisplaysNoMessagesMessage()
        {
            var summary = new ImportSummaryResult
            {
                Status = BatchStatus.Completed,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                TotalRecordsFromDb = 20,
                BatchResults = new List<BatchResultDetail>(),
                Messages = new List<string>()
            };

            var result = _reportService.CreateImportSummaryReport(summary);

            Assert.That(result, Does.Contain("No messages."));
        }

        [Test]
        public void CreateImportSummaryReport_WithNullTokenFromEshot_ExcludesTokenFromReport()
        {
            var summary = new ImportSummaryResult
            {
                Status = BatchStatus.Completed,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                TotalRecordsFromDb = 50,
                BatchResults = new List<BatchResultDetail>
                {
                    new BatchResultDetail
                    {
                        BatchId = "batch-001",
                        Status = BatchStatus.Completed,
                        RecordsProcessed = 50,
                        TokenFromEshot = null,
                        Error = null
                    }
                },
                Messages = new List<string>()
            };

            var result = _reportService.CreateImportSummaryReport(summary);

            Assert.That(result, Does.Not.Contain("Token:"));
            Assert.That(result, Does.Contain("BatchId: batch-001"));
            Assert.That(result, Does.Contain("Records Processed: 50"));
        }

        [Test]
        public void CreateImportSummaryReport_WithEmptyTokenFromEshot_ExcludesTokenFromReport()
        {
            var summary = new ImportSummaryResult
            {
                Status = BatchStatus.Completed,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                TotalRecordsFromDb = 30,
                BatchResults = new List<BatchResultDetail>
                {
                    new BatchResultDetail
                    {
                        BatchId = "batch-002",
                        Status = BatchStatus.Completed,
                        RecordsProcessed = 30,
                        TokenFromEshot = "",
                        Error = null
                    }
                },
                Messages = new List<string>()
            };

            var result = _reportService.CreateImportSummaryReport(summary);

            Assert.That(result, Does.Not.Contain("TokenFromEshot:"));
        }

        [Test]
        public void CreateImportSummaryReport_WithNullError_ExcludesErrorFromReport()
        {
            var summary = new ImportSummaryResult
            {
                Status = BatchStatus.Completed,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                TotalRecordsFromDb = 25,
                BatchResults = new List<BatchResultDetail>
                {
                    new BatchResultDetail
                    {
                        BatchId = "batch-003",
                        Status = BatchStatus.Completed,
                        RecordsProcessed = 25,
                        TokenFromEshot = "token-789",
                        Error = null
                    }
                },
                Messages = new List<string>()
            };

            var result = _reportService.CreateImportSummaryReport(summary);

            Assert.That(result, Does.Not.Contain("Error:"));
            Assert.That(result, Does.Contain("Token: token-789"));
        }

        [Test]
        public void CreateImportSummaryReport_WithEmptyError_ExcludesErrorFromReport()
        {
            var summary = new ImportSummaryResult
            {
                Status = BatchStatus.Failed,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                TotalRecordsFromDb = 15,
                BatchResults = new List<BatchResultDetail>
                {
                    new BatchResultDetail
                    {
                        BatchId = "batch-004",
                        Status = BatchStatus.Failed,
                        RecordsProcessed = 0,
                        TokenFromEshot = null,
                        Error = ""
                    }
                },
                Messages = new List<string>()
            };

            var result = _reportService.CreateImportSummaryReport(summary);

            Assert.That(result, Does.Not.Contain("Error:"));
        }

        [Test]
        public void CreateImportSummaryReport_WithTokenAndError_IncludesBothInReport()
        {
            var summary = new ImportSummaryResult
            {
                Status = BatchStatus.Failed,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                TotalRecordsFromDb = 75,
                BatchResults = new List<BatchResultDetail>
                {
                    new BatchResultDetail
                    {
                        BatchId = "batch-005",
                        Status = BatchStatus.Failed,
                        RecordsProcessed = 0,
                        TokenFromEshot = "token-error",
                        Error = "API connection timeout"
                    }
                },
                Messages = new List<string> { "Connection failed" }
            };

            var result = _reportService.CreateImportSummaryReport(summary);

            Assert.That(result, Does.Contain("Token: token-error"));
            Assert.That(result, Does.Contain("Error: API connection timeout"));
        }

        [Test]
        public void CreateImportSummaryReport_WithSingleMessage_IncludesMessageInReport()
        {
            var summary = new ImportSummaryResult
            {
                Status = BatchStatus.Completed,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                TotalRecordsFromDb = 10,
                BatchResults = new List<BatchResultDetail>(),
                Messages = new List<string> { "Single message" }
            };

            var result = _reportService.CreateImportSummaryReport(summary);

            Assert.That(result, Does.Contain("Messages:"));
            Assert.That(result, Does.Contain("- Single message"));
        }

        [Test]
        public void CreateImportSummaryReport_WithMultipleMessages_IncludesAllMessagesInReport()
        {
            var summary = new ImportSummaryResult
            {
                Status = BatchStatus.Partial,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                TotalRecordsFromDb = 200,
                BatchResults = new List<BatchResultDetail>(),
                Messages = new List<string> { "Message 1", "Message 2", "Message 3" }
            };

            var result = _reportService.CreateImportSummaryReport(summary);

            Assert.That(result, Does.Contain("Messages:"));
            Assert.That(result, Does.Contain("- Message 1"));
            Assert.That(result, Does.Contain("- Message 2"));
            Assert.That(result, Does.Contain("- Message 3"));
        }

        [Test]
        public void CreateImportSummaryReport_WithZeroTotalRecords_IncludesZeroInReport()
        {
            var summary = new ImportSummaryResult
            {
                Status = BatchStatus.Completed,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                TotalRecordsFromDb = 0,
                BatchResults = new List<BatchResultDetail>(),
                Messages = new List<string>()
            };

            var result = _reportService.CreateImportSummaryReport(summary);

            Assert.That(result, Does.Contain("Total Records From DB: 0"));
            Assert.That(result, Does.Contain("Total Records Processed: 0"));
        }

        [Test]
        public void CreateImportSummaryReport_WithMaxIntTotalRecords_IncludesValueInReport()
        {
            var summary = new ImportSummaryResult
            {
                Status = BatchStatus.Completed,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                TotalRecordsFromDb = int.MaxValue,
                BatchResults = new List<BatchResultDetail>(),
                Messages = new List<string>()
            };

            var result = _reportService.CreateImportSummaryReport(summary);

            Assert.That(result, Does.Contain($"Total Records From DB: {int.MaxValue}"));
        }

        [Test]
        public void CreateImportSummaryReport_WithNullStatus_IncludesEmptyStatusInReport()
        {
            var summary = new ImportSummaryResult
            {
                Status = null,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                TotalRecordsFromDb = 5,
                BatchResults = new List<BatchResultDetail>(),
                Messages = new List<string>()
            };

            var result = _reportService.CreateImportSummaryReport(summary);

            Assert.That(result, Does.Contain("Status: "));
        }

        [Test]
        public void CreateImportSummaryReport_WithMinDateTimeValues_FormatsCorrectly()
        {
            var summary = new ImportSummaryResult
            {
                Status = BatchStatus.Completed,
                StartTime = DateTime.MinValue,
                EndTime = DateTime.MinValue,
                TotalRecordsFromDb = 10,
                BatchResults = new List<BatchResultDetail>(),
                Messages = new List<string>()
            };

            var result = _reportService.CreateImportSummaryReport(summary);

            Assert.That(result, Does.Contain("Start Time:"));
            Assert.That(result, Does.Contain("End Time:"));
        }

        [Test]
        public void CreateImportSummaryReport_WithMaxDateTimeValues_FormatsCorrectly()
        {
            var summary = new ImportSummaryResult
            {
                Status = BatchStatus.Completed,
                StartTime = DateTime.MaxValue,
                EndTime = DateTime.MaxValue,
                TotalRecordsFromDb = 10,
                BatchResults = new List<BatchResultDetail>(),
                Messages = new List<string>()
            };

            var result = _reportService.CreateImportSummaryReport(summary);

            Assert.That(result, Does.Contain("Start Time:"));
            Assert.That(result, Does.Contain("End Time:"));
        }

        [Test]
        public void CreateImportSummaryReport_WithMultipleBatches_NumbersThemSequentially()
        {
            var summary = new ImportSummaryResult
            {
                Status = BatchStatus.Completed,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                TotalRecordsFromDb = 150,
                BatchResults = new List<BatchResultDetail>
                {
                    new BatchResultDetail { BatchId = "batch-1", Status = BatchStatus.Completed, RecordsProcessed = 50 },
                    new BatchResultDetail { BatchId = "batch-2", Status = BatchStatus.Completed, RecordsProcessed = 50 },
                    new BatchResultDetail { BatchId = "batch-3", Status = BatchStatus.Completed, RecordsProcessed = 50 }
                },
                Messages = new List<string>()
            };

            var result = _reportService.CreateImportSummaryReport(summary);

            Assert.That(result, Does.Contain("Batch 1:"));
            Assert.That(result, Does.Contain("Batch 2:"));
            Assert.That(result, Does.Contain("Batch 3:"));
            Assert.That(result, Does.Contain("Batch Results (3):"));
        }

        [Test]
        public void CreateImportSummaryReport_Always_IncludesHeaderAndFooter()
        {
            var summary = new ImportSummaryResult
            {
                Status = BatchStatus.Completed,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                TotalRecordsFromDb = 10,
                BatchResults = new List<BatchResultDetail>(),
                Messages = new List<string>()
            };

            var result = _reportService.CreateImportSummaryReport(summary);

            Assert.That(result, Does.Contain("################################################################################"));
            Assert.That(result, Does.Contain("Import Summary Report"));
        }

        [Test]
        public void CreateImportSummaryReport_WithSpecialCharactersInMessages_IncludesThemInReport()
        {
            var summary = new ImportSummaryResult
            {
                Status = BatchStatus.Completed,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                TotalRecordsFromDb = 10,
                BatchResults = new List<BatchResultDetail>(),
                Messages = new List<string> { "Message with <special> & \"characters\"", "Line\nBreak", "Tab\there" }
            };

            var result = _reportService.CreateImportSummaryReport(summary);

            Assert.That(result, Does.Contain("Message with <special> & \"characters\""));
            Assert.That(result, Does.Contain("Line\nBreak"));
            Assert.That(result, Does.Contain("Tab\there"));
        }

        [Test]
        public void CreateImportSummaryReport_WithSpecialCharactersInError_IncludesThemInReport()
        {
            var summary = new ImportSummaryResult
            {
                Status = BatchStatus.Failed,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                TotalRecordsFromDb = 10,
                BatchResults = new List<BatchResultDetail>
                {
                    new BatchResultDetail
                    {
                        BatchId = "batch-001",
                        Status = BatchStatus.Failed,
                        RecordsProcessed = 0,
                        Error = "Error: <XML> parsing failed with \"quotes\" & ampersand"
                    }
                },
                Messages = new List<string>()
            };

            var result = _reportService.CreateImportSummaryReport(summary);

            Assert.That(result, Does.Contain("Error: <XML> parsing failed with \"quotes\" & ampersand"));
        }

        [Test]
        public void CreateImportSummaryReport_IncludesRecordsReceivedAndFailed()
        {
            var summary = new ImportSummaryResult
            {
                Status = BatchStatus.Completed,
                StartTime = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2024, 1, 15, 10, 45, 0, DateTimeKind.Utc),
                TotalRecordsFromDb = 100,
                BatchResults = new List<BatchResultDetail>
                {
                    new BatchResultDetail
                    {
                        BatchId = "batch-001",
                        Status = BatchStatus.Completed,
                        RecordsProcessed = 48,
                        RecordsReceived = 50,
                        RecordsFailed = 2,
                        IsPartiallyImported = true,
                        TokenFromEshot = "token-123",
                        AdditionalInfo = "2 records had validation errors"
                    }
                },
                Messages = new List<string> { "Import completed" }
            };

            var result = _reportService.CreateImportSummaryReport(summary);

            Assert.That(result, Does.Contain("Total Records Received: 50"));
            Assert.That(result, Does.Contain("Total Records Processed: 48"));
            Assert.That(result, Does.Contain("Total Records Failed: 2"));
            Assert.That(result, Does.Contain("Records Received: 50"));
            Assert.That(result, Does.Contain("Records Failed: 2"));
            Assert.That(result, Does.Contain("Is Partially Imported: True"));
            Assert.That(result, Does.Contain("Additional Info: 2 records had validation errors"));
        }

        [Test]
        public void CreateImportSummaryReport_ShowsCorrectSummaryTotals()
        {
            var summary = new ImportSummaryResult
            {
                Status = BatchStatus.Partial,
                StartTime = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2024, 1, 15, 10, 45, 0, DateTimeKind.Utc),
                TotalRecordsFromDb = 200,
                BatchResults = new List<BatchResultDetail>
                {
                    new BatchResultDetail
                    {
                        BatchId = "batch-001",
                        Status = BatchStatus.Completed,
                        RecordsProcessed = 98,
                        RecordsReceived = 100,
                        RecordsFailed = 2,
                        IsPartiallyImported = true
                    },
                    new BatchResultDetail
                    {
                        BatchId = "batch-002",
                        Status = BatchStatus.Completed,
                        RecordsProcessed = 95,
                        RecordsReceived = 100,
                        RecordsFailed = 5,
                        IsPartiallyImported = true
                    }
                },
                Messages = new List<string>()
            };

            var result = _reportService.CreateImportSummaryReport(summary);

            Assert.That(result, Does.Contain("Total Records Received: 200"));
            Assert.That(result, Does.Contain("Total Records Processed: 193"));
            Assert.That(result, Does.Contain("Total Records Failed: 7"));
            Assert.That(result, Does.Contain("Batches with Partial Imports: 2"));
        }

        [Test]
        public async Task SaveReportToBlobAndNotifyAsync_SavesReportAndSendsEmail()
        {
            var reportContent = "Test report content";
            var fileName = "test-report";
            var integrationName = "Email Integration";

            var mockBlobServiceClient = new Mock<BlobServiceClient>();
            var mockLogger = new Mock<ILogger<ReportService>>();
            var mockEmailNotificationService = new Mock<IEmailNotificationService>();
            var mockBlobContainerClient = new Mock<BlobContainerClient>();
            var mockBlobClient = new Mock<BlobClient>();

            mockBlobServiceClient
                .Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
                .Returns(mockBlobContainerClient.Object);

            mockBlobContainerClient
               .Setup(x => x.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), It.IsAny<System.Collections.Generic.IDictionary<string, string>>(), It.IsAny<BlobContainerEncryptionScopeOptions>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(Mock.Of<Response<BlobContainerInfo>>());

            mockBlobContainerClient
                .Setup(x => x.GetBlobClient(It.IsAny<string>()))
                .Returns(mockBlobClient.Object);

            mockBlobClient
                .Setup(x => x.Uri)
                .Returns(new Uri("https://test.blob.core.windows.net/container/Report/test-report.report.txt"));

            mockBlobClient
                .Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

            var service = new ReportService(mockBlobServiceClient.Object, mockLogger.Object, mockEmailNotificationService.Object);

            await service.SaveReportToBlobAndNotifyAsync(reportContent, fileName, integrationName);

            mockBlobClient.Verify(x => x.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()), Times.Once);
            mockEmailNotificationService.Verify(
                x => x.SendMonitoringReportAsync(
                    integrationName,
                    reportContent,
                    It.Is<string>(url => url.Contains("test-report.report.txt")),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Monitoring report email sent")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Test]
        public void CreateImportSummaryReport_WhenBatchIsPartiallyImportedAndStatusCompleted_SetsStatusToPartialAndIncludesInReport()
        {
            var service = CreateService();

            var summary = new ImportSummaryResult
            {
                Status = BatchStatus.Completed,
                BatchResults = new List<BatchResultDetail>
                {
                    new() {
                        BatchId = "batch-1",
                        IsPartiallyImported = true,
                        RecordsReceived = 10,
                        RecordsProcessed = 5,
                        RecordsFailed = 5,
                        Status = BatchStatus.InProgress
                    }
                }
            };

            var report = service.CreateImportSummaryReport(summary);

            Assert.That(summary.Status, Is.EqualTo(BatchStatus.Partial));
            Assert.That(report, Does.Contain("Status: Partial"));
        }

        [Test]
        public void CreateImportSummaryReport_WhenNoPartialBatchesAndStatusCompleted_KeepsStatusCompletedAndIncludesInReport()
        {
            var service = CreateService();

            var summary = new ImportSummaryResult
            {
                Status = BatchStatus.Completed,
                BatchResults = new List<BatchResultDetail>
                {
                    new BatchResultDetail
                    {
                        BatchId = "batch-1",
                        IsPartiallyImported = false,
                        RecordsReceived = 10,
                        RecordsProcessed = 10,
                        RecordsFailed = 0,
                        Status = BatchStatus.InProgress
                    },
                    new BatchResultDetail
                    {
                        BatchId = "batch-2",
                        IsPartiallyImported = false,
                        RecordsReceived = 5,
                        RecordsProcessed = 5,
                        RecordsFailed = 0,
                        Status = BatchStatus.InProgress
                    }
                }
            };

            var report = service.CreateImportSummaryReport(summary);

            Assert.That(summary.Status, Is.EqualTo(BatchStatus.Completed));
            Assert.That(report, Does.Contain("Status: Completed"));
        }

        private ReportService CreateService()
        {
            var blobClientMock = new Mock<BlobServiceClient>();
            var loggerMock = new Mock<ILogger<ReportService>>();
            var emailMock = new Mock<IEmailNotificationService>();

            return new ReportService(blobClientMock.Object, loggerMock.Object, emailMock.Object);
        }

        [Test]
        public void CreateImportSummaryReport_WithValidFieldMapping_IncludesMappings()
        {
            var summary = new ImportSummaryResult
            {
                Status = BatchStatus.Completed,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                TotalRecordsFromDb = 0,
                BatchResults = new List<BatchResultDetail>(),
                Messages = new List<string>(),
                FieldMapping = "[{\"Source\":\"FirstName\",\"Target\":\"first_name\"},{\"Source\":\"LastName\",\"Target\":\"last_name\"}]"
            };

            var result = _reportService.CreateImportSummaryReport(summary);

            Assert.That(result, Does.Contain("Field Mapping:"));
            Assert.That(result, Does.Contain("Source: FirstName -> Target: first_name"));
            Assert.That(result, Does.Contain("Source: LastName -> Target: last_name"));
        }

        [Test]
        public void CreateImportSummaryReport_WithEmptyJsonArrayFieldMapping_IncludesNone()
        {
            var summary = new ImportSummaryResult
            {
                Status = BatchStatus.Completed,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                TotalRecordsFromDb = 0,
                BatchResults = new List<BatchResultDetail>(),
                Messages = new List<string>(),
                FieldMapping = "[]"
            };

            var result = _reportService.CreateImportSummaryReport(summary);

            Assert.That(result, Does.Contain("Field Mapping: None"));
        }

        [Test]
        public void CreateImportSummaryReport_WithInvalidJsonFieldMapping_IncludesRaw()
        {
            var raw = "{not: valid json}";
            var summary = new ImportSummaryResult
            {
                Status = BatchStatus.Completed,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                TotalRecordsFromDb = 0,
                BatchResults = new List<BatchResultDetail>(),
                Messages = new List<string>(),
                FieldMapping = raw
            };

            var result = _reportService.CreateImportSummaryReport(summary);

            Assert.That(result, Does.Contain($"Field Mapping (raw): {raw}"));
        }

        [Test]
        public void SaveReportToBlob_UploadThrows_LogsErrorAndRethrows()
        {
            var reportContent = "Test content";
            var fileName = "upload-fail-report";
            var expectedException = new RequestFailedException("Upload failed");

            var mockBlobServiceClient = new Mock<BlobServiceClient>();
            var mockLogger = new Mock<ILogger<ReportService>>();
            var mockEmailNotificationService = new Mock<IEmailNotificationService>();
            var mockBlobContainerClient = new Mock<BlobContainerClient>();
            var mockBlobClient = new Mock<BlobClient>();

            mockBlobServiceClient
                .Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
                .Returns(mockBlobContainerClient.Object);

            mockBlobContainerClient
                .Setup(x => x.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<BlobContainerEncryptionScopeOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<Response<BlobContainerInfo>>());

            mockBlobContainerClient
                .Setup(x => x.GetBlobClient(It.IsAny<string>()))
                .Returns(mockBlobClient.Object);

            mockBlobClient
                .Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            var service = new ReportService(mockBlobServiceClient.Object, mockLogger.Object, mockEmailNotificationService.Object);

            var thrown = Assert.ThrowsAsync<InvalidOperationException>(async () => await service.SaveReportToBlobInternalAsync(reportContent, fileName));
            Assert.That(thrown, Is.Not.Null);
            Assert.That(thrown!.Message, Is.EqualTo($"Failed to save report file '{fileName}' to blob storage."));
            Assert.That(thrown.InnerException, Is.EqualTo(expectedException));

            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Failed to save report file '{fileName}' to blob storage container")),
                    It.Is<Exception>(ex => ex == expectedException),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Test]
        public void SaveReportToBlobAndNotifyAsync_WhenEmailNotificationThrows_LogsErrorAndRethrows()
        {
            var reportContent = "Test report content";
            var fileName = "test-report";
            var integrationName = "Email Integration Fail";

            var mockBlobServiceClient = new Mock<BlobServiceClient>();
            var mockLogger = new Mock<ILogger<ReportService>>();
            var mockEmailNotificationService = new Mock<IEmailNotificationService>();
            var mockBlobContainerClient = new Mock<BlobContainerClient>();
            var mockBlobClient = new Mock<BlobClient>();

            mockBlobServiceClient
                .Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
                .Returns(mockBlobContainerClient.Object);

            mockBlobContainerClient
                .Setup(x => x.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<BlobContainerEncryptionScopeOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<Response<BlobContainerInfo>>());

            mockBlobContainerClient
                .Setup(x => x.GetBlobClient(It.IsAny<string>()))
                .Returns(mockBlobClient.Object);

            mockBlobClient
                .Setup(x => x.Uri)
                .Returns(new Uri("https://test.blob.core.windows.net/container/Report/test-report.report.txt"));

            mockBlobClient
                .Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

            var notifyException = new Exception("notify failed");
            mockEmailNotificationService
                .Setup(x => x.SendMonitoringReportAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(notifyException);

            var service = new ReportService(mockBlobServiceClient.Object, mockLogger.Object, mockEmailNotificationService.Object);

            var thrown = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await service.SaveReportToBlobAndNotifyAsync(reportContent, fileName, integrationName));

            Assert.That(thrown, Is.Not.Null);
            Assert.That(thrown!.Message, Is.EqualTo($"Failed to send email notification for integration '{integrationName}'."));
            Assert.That(thrown.InnerException, Is.EqualTo(notifyException));

            mockBlobClient.Verify(x => x.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()), Times.Once);

            mockEmailNotificationService.Verify(x => x.SendMonitoringReportAsync(
                integrationName,
                reportContent,
                It.Is<string>(s => s.Contains("test-report.report.txt")),
                It.IsAny<CancellationToken>()), Times.Once);

            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to send email notification for integration")),
                    It.Is<Exception>(ex => ex == notifyException),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}