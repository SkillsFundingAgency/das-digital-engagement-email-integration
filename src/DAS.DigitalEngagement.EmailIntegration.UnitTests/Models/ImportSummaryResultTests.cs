using DAS.DigitalEngagement.Models.Import;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace DAS.DigitalEngagement.EmailIntegration.UnitTests.Models
{
    [TestFixture]
    public class ImportSummaryResultTests
    {
        [Test]
        public void TotalRecordsProcessed_OnlyCountsCompletedBatches()
        {
            // Arrange
            var summary = new ImportSummaryResult
            {
                BatchResults = new List<BatchResultDetail>
                {
                    new BatchResultDetail { Status = "Completed", RecordsProcessed = 50, RecordsReceived = 50, RecordsFailed = 0 },
                    new BatchResultDetail { Status = "Failed", RecordsProcessed = 0, RecordsReceived = 30, RecordsFailed = 30 },
                    new BatchResultDetail { Status = "Completed", RecordsProcessed = 75, RecordsReceived = 80, RecordsFailed = 5 }
                }
            };

            // Act
            var result = summary.TotalRecordsProcessed;

            // Assert - only completed batches should be counted (50 + 75 = 125)
            Assert.That(result, Is.EqualTo(125));
        }

        [Test]
        public void TotalRecordsReceived_SumsAllBatches()
        {
            // Arrange
            var summary = new ImportSummaryResult
            {
                BatchResults = new List<BatchResultDetail>
                {
                    new BatchResultDetail { Status = "Completed", RecordsProcessed = 50, RecordsReceived = 50, RecordsFailed = 0 },
                    new BatchResultDetail { Status = "Failed", RecordsProcessed = 0, RecordsReceived = 30, RecordsFailed = 30 },
                    new BatchResultDetail { Status = "Completed", RecordsProcessed = 75, RecordsReceived = 80, RecordsFailed = 5 }
                }
            };

            // Act
            var result = summary.TotalRecordsReceived;

            // Assert - all batches should be counted (50 + 30 + 80 = 160)
            Assert.That(result, Is.EqualTo(160));
        }

        [Test]
        public void TotalRecordsFailed_SumsAllBatches()
        {
            // Arrange
            var summary = new ImportSummaryResult
            {
                BatchResults = new List<BatchResultDetail>
                {
                    new BatchResultDetail { Status = "Completed", RecordsProcessed = 50, RecordsReceived = 50, RecordsFailed = 0 },
                    new BatchResultDetail { Status = "Failed", RecordsProcessed = 0, RecordsReceived = 30, RecordsFailed = 30 },
                    new BatchResultDetail { Status = "Completed", RecordsProcessed = 75, RecordsReceived = 80, RecordsFailed = 5 }
                }
            };

            // Act
            var result = summary.TotalRecordsFailed;

            // Assert - all failed records should be counted (0 + 30 + 5 = 35)
            Assert.That(result, Is.EqualTo(35));
        }

        [Test]
        public void IsPartiallyImported_ReturnsTrue_WhenAnyBatchIsPartiallyImported()
        {
            // Arrange
            var summary = new ImportSummaryResult
            {
                BatchResults = new List<BatchResultDetail>
                {
                    new BatchResultDetail { Status = "Completed", RecordsProcessed = 50, RecordsReceived = 50, RecordsFailed = 0, IsPartiallyImported = false },
                    new BatchResultDetail { Status = "Completed", RecordsProcessed = 75, RecordsReceived = 80, RecordsFailed = 5, IsPartiallyImported = true }
                }
            };

            // Act
            var result = summary.IsPartiallyImported;

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsPartiallyImported_ReturnsFalse_WhenNoBatchIsPartiallyImported()
        {
            // Arrange
            var summary = new ImportSummaryResult
            {
                BatchResults = new List<BatchResultDetail>
                {
                    new BatchResultDetail { Status = "Completed", RecordsProcessed = 50, RecordsReceived = 50, RecordsFailed = 0, IsPartiallyImported = false },
                    new BatchResultDetail { Status = "Completed", RecordsProcessed = 80, RecordsReceived = 80, RecordsFailed = 0, IsPartiallyImported = false }
                }
            };

            // Act
            var result = summary.IsPartiallyImported;

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void TotalRecordsProcessed_ReturnsZero_WhenNoBatches()
        {
            // Arrange
            var summary = new ImportSummaryResult
            {
                BatchResults = new List<BatchResultDetail>()
            };

            // Act
            var result = summary.TotalRecordsProcessed;

            // Assert
            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public void ToString_IncludesAllBatchDetails()
        {
            // Arrange
            var summary = new ImportSummaryResult
            {
                Status = "Completed",
                StartTime = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2024, 1, 15, 11, 0, 0, DateTimeKind.Utc),
                TotalRecordsFromDb = 100,
                BatchResults = new List<BatchResultDetail>
                {
                    new BatchResultDetail 
                    { 
                        Status = "Completed", 
                        RecordsProcessed = 50, 
                        RecordsReceived = 50, 
                        RecordsFailed = 0,
                        IsPartiallyImported = false,
                        AdditionalInfo = "Success"
                    }
                },
                Messages = new List<string> { "Import completed" }
            };

            // Act
            var result = summary.ToString();

            // Assert
            Assert.That(result, Does.Contain("Status: Completed"));
            Assert.That(result, Does.Contain("TotalRecordsProcessed: 50"));
            Assert.That(result, Does.Contain("RecordsReceived: 50"));
            Assert.That(result, Does.Contain("RecordsFailed: 0"));
            Assert.That(result, Does.Contain("IsPartiallyImported: False"));
            Assert.That(result, Does.Contain("AdditionalInfo: Success"));
        }
    }
}