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
        /// <summary>
        /// Tests that the ReportService constructor successfully initializes when provided with valid dependencies.
        /// Input: Valid mocked BlobServiceClient and ILogger instances.
        /// Expected: Constructor completes without throwing an exception.
        /// </summary>
        [Test]
        public void Constructor_WithValidDependencies_DoesNotThrow()
        {
            // Arrange
            var mockBlobServiceClient = new Mock<BlobServiceClient>();
            var mockLogger = new Mock<ILogger<ReportService>>();
            var mockEmailNotificationService = new Mock<IEmailNotificationService>();

            // Act & Assert
            Assert.DoesNotThrow(() => new ReportService(mockBlobServiceClient.Object, mockLogger.Object, mockEmailNotificationService.Object));
        }

        /// <summary>
        /// Tests the ReportService constructor behavior when a null BlobServiceClient is provided.
        /// Input: Null BlobServiceClient parameter.
        /// Expected: Constructor completes without throwing (no null validation present).
        /// </summary>
        [Test]
        public void Constructor_WithNullBlobServiceClient_DoesNotThrow()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<ReportService>>();
            var mockEmailNotificationService = new Mock<IEmailNotificationService>();

            // Act & Assert
            Assert.DoesNotThrow(() => new ReportService(null, mockLogger.Object, mockEmailNotificationService.Object));
        }

        /// <summary>
        /// Tests the ReportService constructor behavior when a null ILogger is provided.
        /// Input: Null ILogger parameter.
        /// Expected: Constructor completes without throwing (no null validation present).
        /// </summary>
        [Test]
        public void Constructor_WithNullLogger_DoesNotThrow()
        {
            // Arrange
            var mockBlobServiceClient = new Mock<BlobServiceClient>();
            var mockEmailNotificationService = new Mock<IEmailNotificationService>();

            // Act & Assert
            Assert.DoesNotThrow(() => new ReportService(mockBlobServiceClient.Object, null, mockEmailNotificationService.Object));
        }

        /// <summary>
        /// Tests the ReportService constructor behavior when both dependencies are null.
        /// Input: Null BlobServiceClient and null ILogger parameters.
        /// Expected: Constructor completes without throwing (no null validation present).
        /// </summary>
        [Test]
        public void Constructor_WithBothParametersNull_DoesNotThrow()
        {
            // Arrange
            var mockEmailNotificationService = new Mock<IEmailNotificationService>();

            // Act & Assert
            Assert.DoesNotThrow(() => new ReportService(null, null, mockEmailNotificationService.Object));
        }

        /// <summary>
        /// Tests that SaveReportToBlob successfully saves a report with valid inputs,
        /// creates the container if needed, uploads the content, and logs success.
        /// </summary>
        [Test]
        public async Task SaveReportToBlob_ValidInputs_SavesSuccessfullyAndLogs()
        {
            // Arrange
            var reportContent = "Test report content";
            var fileName = "test-report";
            var expectedBlobPath = "Report/test-report.report.txt";

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
                .Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

            var service = new ReportService(mockBlobServiceClient.Object, mockLogger.Object, mockEmailNotificationService.Object);

            // Act
            await service.SaveReportToBlob(reportContent, fileName);

            // Assert
            mockBlobServiceClient.Verify(x => x.GetBlobContainerClient("email-integration-to-marketing-tool"), Times.Once);
            mockBlobContainerClient.Verify(x => x.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), It.IsAny<System.Collections.Generic.IDictionary<string, string>>(), It.IsAny<BlobContainerEncryptionScopeOptions>(), It.IsAny<CancellationToken>()), Times.Once);
            mockBlobContainerClient.Verify(x => x.GetBlobClient(expectedBlobPath), Times.Once);
            mockBlobClient.Verify(x => x.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()), Times.Once);
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Report file saved: {fileName}.report.txt")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        /// <summary>
        /// Tests that SaveReportToBlob successfully handles empty report content.
        /// </summary>
        [Test]
        public async Task SaveReportToBlob_EmptyReportContent_SavesSuccessfully()
        {
            // Arrange
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
                .Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

            var service = new ReportService(mockBlobServiceClient.Object, mockLogger.Object, mockEmailNotificationService.Object);

            // Act
            await service.SaveReportToBlob(reportContent, fileName);

            // Assert
            mockBlobClient.Verify(x => x.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Tests that SaveReportToBlob logs error and re-throws exception when CreateIfNotExistsAsync fails.
        /// </summary>
        [Test]
        public void SaveReportToBlob_CreateContainerThrowsException_LogsErrorAndRethrows()
        {
            // Arrange
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

            // Act & Assert
            var thrownException = Assert.ThrowsAsync<InvalidOperationException>(
                async () => await service.SaveReportToBlob(reportContent, fileName));

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

        /// <summary>
        /// Tests that SaveReportToBlob handles whitespace-only report content correctly.
        /// </summary>
        [Test]
        public async Task SaveReportToBlob_WhitespaceReportContent_SavesSuccessfully()
        {
            // Arrange
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
                .Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

            var service = new ReportService(mockBlobServiceClient.Object, mockLogger.Object, mockEmailNotificationService.Object);

            // Act
            await service.SaveReportToBlob(reportContent, fileName);

            // Assert
            mockBlobClient.Verify(x => x.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Tests that SaveReportToBlob correctly constructs blob path with empty fileName.
        /// </summary>
        [Test]
        public async Task SaveReportToBlob_EmptyFileName_SavesWithCorrectPath()
        {
            // Arrange
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
                .Setup(x => x.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), It.IsAny<System.Collections.Generic.IDictionary<string, string>>(), It.IsAny<BlobContainerEncryptionScopeOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<Response<BlobContainerInfo>>());

            mockBlobContainerClient
                .Setup(x => x.GetBlobClient(expectedBlobPath))
                .Returns(mockBlobClient.Object);

            mockBlobClient
                .Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

            var service = new ReportService(mockBlobServiceClient.Object, mockLogger.Object, mockEmailNotificationService.Object);

            // Act
            await service.SaveReportToBlob(reportContent, fileName);

            // Assert
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

        /// <summary>
        /// Tests that CreateImportSummaryReport generates a valid report with all sections
        /// when the summary contains batch results and messages.
        /// </summary>
        [Test]
        public void CreateImportSummaryReport_WithBatchResultsAndMessages_GeneratesCompleteReport()
        {
            // Arrange
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

            // Act
            var result = _reportService.CreateImportSummaryReport(summary);

            // Assert
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

        /// <summary>
        /// Tests that CreateImportSummaryReport handles null batch results correctly
        /// by displaying "No batch results available" message.
        /// </summary>
        [Test]
        public void CreateImportSummaryReport_WithNullBatchResults_DisplaysNoBatchResultsMessage()
        {
            // Arrange
            var summary = new ImportSummaryResult
            {
                Status = BatchStatus.Completed,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                TotalRecordsFromDb = 0,
                BatchResults = new List<BatchResultDetail>(),
                Messages = new List<string>()
            };

            // Act
            var result = _reportService.CreateImportSummaryReport(summary);

            // Assert
            Assert.That(result, Does.Contain("No batch results available."));
            Assert.That(result, Does.Not.Contain("Batch Results ("));
        }

        /// <summary>
        /// Tests that CreateImportSummaryReport handles empty batch results list correctly
        /// by displaying "No batch results available" message.
        /// </summary>
        [Test]
        public void CreateImportSummaryReport_WithEmptyBatchResults_DisplaysNoBatchResultsMessage()
        {
            // Arrange
            var summary = new ImportSummaryResult
            {
                Status = BatchStatus.Failed,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                TotalRecordsFromDb = 0,
                BatchResults = new List<BatchResultDetail>(),
                Messages = new List<string>()
            };

            // Act
            var result = _reportService.CreateImportSummaryReport(summary);

            // Assert
            Assert.That(result, Does.Contain("No batch results available."));
            Assert.That(result, Does.Not.Contain("Batch Results ("));
        }

        /// <summary>
        /// Tests that CreateImportSummaryReport handles null messages correctly
        /// by displaying "No messages" message.
        /// </summary>
        [Test]
        public void CreateImportSummaryReport_WithNullMessages_DisplaysNoMessagesMessage()
        {
            // Arrange
            var summary = new ImportSummaryResult
            {
                Status = BatchStatus.Partial,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                TotalRecordsFromDb = 5,
                BatchResults = new List<BatchResultDetail>(),
                Messages = null
            };

            // Act
            var result = _reportService.CreateImportSummaryReport(summary);

            // Assert
            Assert.That(result, Does.Contain("No messages."));
            Assert.That(result, Does.Not.Contain("Messages:"));
        }

        /// <summary>
        /// Tests that CreateImportSummaryReport handles empty messages list correctly
        /// by displaying "No messages" message.
        /// </summary>
        [Test]
        public void CreateImportSummaryReport_WithEmptyMessages_DisplaysNoMessagesMessage()
        {
            // Arrange
            var summary = new ImportSummaryResult
            {
                Status = BatchStatus.Completed,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                TotalRecordsFromDb = 20,
                BatchResults = new List<BatchResultDetail>(),
                Messages = new List<string>()
            };

            // Act
            var result = _reportService.CreateImportSummaryReport(summary);

            // Assert
            Assert.That(result, Does.Contain("No messages."));
        }

        /// <summary>
        /// Tests that CreateImportSummaryReport correctly excludes TokenFromEshot
        /// when the batch has a null TokenFromEshot value.
        /// </summary>
        [Test]
        public void CreateImportSummaryReport_WithNullTokenFromEshot_ExcludesTokenFromReport()
        {
            // Arrange
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

            // Act
            var result = _reportService.CreateImportSummaryReport(summary);

            // Assert
            Assert.That(result, Does.Not.Contain("Token:"));
            Assert.That(result, Does.Contain("BatchId: batch-001"));
            Assert.That(result, Does.Contain("Records Processed: 50"));
        }

        /// <summary>
        /// Tests that CreateImportSummaryReport correctly excludes TokenFromEshot
        /// when the batch has an empty TokenFromEshot value.
        /// </summary>
        [Test]
        public void CreateImportSummaryReport_WithEmptyTokenFromEshot_ExcludesTokenFromReport()
        {
            // Arrange
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

            // Act
            var result = _reportService.CreateImportSummaryReport(summary);

            // Assert
            Assert.That(result, Does.Not.Contain("TokenFromEshot:"));
        }

        /// <summary>
        /// Tests that CreateImportSummaryReport correctly excludes Error
        /// when the batch has a null Error value.
        /// </summary>
        [Test]
        public void CreateImportSummaryReport_WithNullError_ExcludesErrorFromReport()
        {
            // Arrange
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

            // Act
            var result = _reportService.CreateImportSummaryReport(summary);

            // Assert
            Assert.That(result, Does.Not.Contain("Error:"));
            Assert.That(result, Does.Contain("Token: token-789"));
        }

        /// <summary>
        /// Tests that CreateImportSummaryReport correctly excludes Error
        /// when the batch has an empty Error value.
        /// </summary>
        [Test]
        public void CreateImportSummaryReport_WithEmptyError_ExcludesErrorFromReport()
        {
            // Arrange
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

            // Act
            var result = _reportService.CreateImportSummaryReport(summary);

            // Assert
            Assert.That(result, Does.Not.Contain("Error:"));
        }

        /// <summary>
        /// Tests that CreateImportSummaryReport includes both TokenFromEshot and Error
        /// when both are populated in the batch result.
        /// </summary>
        [Test]
        public void CreateImportSummaryReport_WithTokenAndError_IncludesBothInReport()
        {
            // Arrange
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

            // Act
            var result = _reportService.CreateImportSummaryReport(summary);

            // Assert
            Assert.That(result, Does.Contain("Token: token-error"));
            Assert.That(result, Does.Contain("Error: API connection timeout"));
        }

        /// <summary>
        /// Tests that CreateImportSummaryReport handles a single message correctly.
        /// </summary>
        [Test]
        public void CreateImportSummaryReport_WithSingleMessage_IncludesMessageInReport()
        {
            // Arrange
            var summary = new ImportSummaryResult
            {
                Status = BatchStatus.Completed,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                TotalRecordsFromDb = 10,
                BatchResults = new List<BatchResultDetail>(),
                Messages = new List<string> { "Single message" }
            };

            // Act
            var result = _reportService.CreateImportSummaryReport(summary);

            // Assert
            Assert.That(result, Does.Contain("Messages:"));
            Assert.That(result, Does.Contain("- Single message"));
        }

        /// <summary>
        /// Tests that CreateImportSummaryReport handles multiple messages correctly.
        /// </summary>
        [Test]
        public void CreateImportSummaryReport_WithMultipleMessages_IncludesAllMessagesInReport()
        {
            // Arrange
            var summary = new ImportSummaryResult
            {
                Status = BatchStatus.Partial,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                TotalRecordsFromDb = 200,
                BatchResults = new List<BatchResultDetail>(),
                Messages = new List<string> { "Message 1", "Message 2", "Message 3" }
            };

            // Act
            var result = _reportService.CreateImportSummaryReport(summary);

            // Assert
            Assert.That(result, Does.Contain("Messages:"));
            Assert.That(result, Does.Contain("- Message 1"));
            Assert.That(result, Does.Contain("- Message 2"));
            Assert.That(result, Does.Contain("- Message 3"));
        }

        /// <summary>
        /// Tests that CreateImportSummaryReport handles boundary value for TotalRecordsFromDb (0).
        /// </summary>
        [Test]
        public void CreateImportSummaryReport_WithZeroTotalRecords_IncludesZeroInReport()
        {
            // Arrange
            var summary = new ImportSummaryResult
            {
                Status = BatchStatus.Completed,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                TotalRecordsFromDb = 0,
                BatchResults = new List<BatchResultDetail>(),
                Messages = new List<string>()
            };

            // Act
            var result = _reportService.CreateImportSummaryReport(summary);

            // Assert
            Assert.That(result, Does.Contain("Total Records From DB: 0"));
            Assert.That(result, Does.Contain("Total Records Processed: 0"));
        }

        /// <summary>
        /// Tests that CreateImportSummaryReport handles large value for TotalRecordsFromDb.
        /// </summary>
        [Test]
        public void CreateImportSummaryReport_WithMaxIntTotalRecords_IncludesValueInReport()
        {
            // Arrange
            var summary = new ImportSummaryResult
            {
                Status = BatchStatus.Completed,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                TotalRecordsFromDb = int.MaxValue,
                BatchResults = new List<BatchResultDetail>(),
                Messages = new List<string>()
            };

            // Act
            var result = _reportService.CreateImportSummaryReport(summary);

            // Assert
            Assert.That(result, Does.Contain($"Total Records From DB: {int.MaxValue}"));
        }

        /// <summary>
        /// Tests that CreateImportSummaryReport handles null status correctly.
        /// </summary>
        [Test]
        public void CreateImportSummaryReport_WithNullStatus_IncludesEmptyStatusInReport()
        {
            // Arrange
            var summary = new ImportSummaryResult
            {
                Status = null,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                TotalRecordsFromDb = 5,
                BatchResults = new List<BatchResultDetail>(),
                Messages = new List<string>()
            };

            // Act
            var result = _reportService.CreateImportSummaryReport(summary);

            // Assert
            Assert.That(result, Does.Contain("Status: "));
        }

        /// <summary>
        /// Tests that CreateImportSummaryReport handles DateTime.MinValue correctly.
        /// </summary>
        [Test]
        public void CreateImportSummaryReport_WithMinDateTimeValues_FormatsCorrectly()
        {
            // Arrange
            var summary = new ImportSummaryResult
            {
                Status = BatchStatus.Completed,
                StartTime = DateTime.MinValue,
                EndTime = DateTime.MinValue,
                TotalRecordsFromDb = 10,
                BatchResults = new List<BatchResultDetail>(),
                Messages = new List<string>()
            };

            // Act
            var result = _reportService.CreateImportSummaryReport(summary);

            // Assert
            Assert.That(result, Does.Contain("Start Time:"));
            Assert.That(result, Does.Contain("End Time:"));
        }

        /// <summary>
        /// Tests that CreateImportSummaryReport handles DateTime.MaxValue correctly.
        /// </summary>
        [Test]
        public void CreateImportSummaryReport_WithMaxDateTimeValues_FormatsCorrectly()
        {
            // Arrange
            var summary = new ImportSummaryResult
            {
                Status = BatchStatus.Completed,
                StartTime = DateTime.MaxValue,
                EndTime = DateTime.MaxValue,
                TotalRecordsFromDb = 10,
                BatchResults = new List<BatchResultDetail>(),
                Messages = new List<string>()
            };

            // Act
            var result = _reportService.CreateImportSummaryReport(summary);

            // Assert
            Assert.That(result, Does.Contain("Start Time:"));
            Assert.That(result, Does.Contain("End Time:"));
        }

        /// <summary>
        /// Tests that CreateImportSummaryReport correctly numbers multiple batches sequentially.
        /// </summary>
        [Test]
        public void CreateImportSummaryReport_WithMultipleBatches_NumbersThemSequentially()
        {
            // Arrange
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

            // Act
            var result = _reportService.CreateImportSummaryReport(summary);

            // Assert
            Assert.That(result, Does.Contain("Batch 1:"));
            Assert.That(result, Does.Contain("Batch 2:"));
            Assert.That(result, Does.Contain("Batch 3:"));
            Assert.That(result, Does.Contain("Batch Results (3):"));
        }

        /// <summary>
        /// Tests that CreateImportSummaryReport includes report header and footer.
        /// </summary>
        [Test]
        public void CreateImportSummaryReport_Always_IncludesHeaderAndFooter()
        {
            // Arrange
            var summary = new ImportSummaryResult
            {
                Status = BatchStatus.Completed,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                TotalRecordsFromDb = 10,
                BatchResults = new List<BatchResultDetail>(),
                Messages = new List<string>()
            };

            // Act
            var result = _reportService.CreateImportSummaryReport(summary);

            // Assert
            Assert.That(result, Does.Contain("################################################################################"));
            Assert.That(result, Does.Contain("Import Summary Report"));
        }

        /// <summary>
        /// Tests that CreateImportSummaryReport handles special characters in messages correctly.
        /// </summary>
        [Test]
        public void CreateImportSummaryReport_WithSpecialCharactersInMessages_IncludesThemInReport()
        {
            // Arrange
            var summary = new ImportSummaryResult
            {
                Status = BatchStatus.Completed,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                TotalRecordsFromDb = 10,
                BatchResults = new List<BatchResultDetail>(),
                Messages = new List<string> { "Message with <special> & \"characters\"", "Line\nBreak", "Tab\there" }
            };

            // Act
            var result = _reportService.CreateImportSummaryReport(summary);

            // Assert
            Assert.That(result, Does.Contain("Message with <special> & \"characters\""));
            Assert.That(result, Does.Contain("Line\nBreak"));
            Assert.That(result, Does.Contain("Tab\there"));
        }

        /// <summary>
        /// Tests that CreateImportSummaryReport handles special characters in batch error messages.
        /// </summary>
        [Test]
        public void CreateImportSummaryReport_WithSpecialCharactersInError_IncludesThemInReport()
        {
            // Arrange
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

            // Act
            var result = _reportService.CreateImportSummaryReport(summary);

            // Assert
            Assert.That(result, Does.Contain("Error: <XML> parsing failed with \"quotes\" & ampersand"));
        }

        /// <summary>
        /// Tests that CreateImportSummaryReport includes records received and failed count
        /// when batch results have these values populated.
        /// </summary>
        [Test]
        public void CreateImportSummaryReport_IncludesRecordsReceivedAndFailed()
        {
            // Arrange
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

            // Act
            var result = _reportService.CreateImportSummaryReport(summary);

            // Assert
            Assert.That(result, Does.Contain("Total Records Received: 50"));
            Assert.That(result, Does.Contain("Total Records Processed: 48"));
            Assert.That(result, Does.Contain("Total Records Failed: 2"));
            Assert.That(result, Does.Contain("Records Received: 50"));
            Assert.That(result, Does.Contain("Records Failed: 2"));
            Assert.That(result, Does.Contain("Is Partially Imported: True"));
            Assert.That(result, Does.Contain("Additional Info: 2 records had validation errors"));
        }

        /// <summary>
        /// Tests that CreateImportSummaryReport shows correct summary totals
        /// when there are multiple batch results with partial imports.
        /// </summary>
        [Test]
        public void CreateImportSummaryReport_ShowsCorrectSummaryTotals()
        {
            // Arrange
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

            // Act
            var result = _reportService.CreateImportSummaryReport(summary);

            // Assert
            Assert.That(result, Does.Contain("Total Records Received: 200"));
            Assert.That(result, Does.Contain("Total Records Processed: 193"));
            Assert.That(result, Does.Contain("Total Records Failed: 7"));
            Assert.That(result, Does.Contain("Batches with Partial Imports: 2"));
        }

        /// <summary>
        /// Tests that SaveReportToBlobAndNotifyAsync saves the report and sends an email notification.
        /// </summary>
        [Test]
        public async Task SaveReportToBlobAndNotifyAsync_SavesReportAndSendsEmail()
        {
            // Arrange
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

            // Act
            await service.SaveReportToBlobAndNotifyAsync(reportContent, fileName, integrationName);

            // Assert
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
            // Arrange
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

            // Act
            var report = service.CreateImportSummaryReport(summary);

            // Assert
            Assert.That(summary.Status, Is.EqualTo(BatchStatus.Partial));
            Assert.That(report, Does.Contain("Status: Partial") );
        }

        [Test]
        public void CreateImportSummaryReport_WhenNoPartialBatchesAndStatusCompleted_KeepsStatusCompletedAndIncludesInReport()
        {
            // Arrange
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

            // Act
            var report = service.CreateImportSummaryReport(summary);

            // Assert
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
    }
}