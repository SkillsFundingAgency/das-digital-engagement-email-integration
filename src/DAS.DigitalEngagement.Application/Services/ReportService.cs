using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.Models.Import;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs;

namespace DAS.DigitalEngagement.Application.Services
{
    public class ReportService : IReportService
    {
        private readonly string _container = "email-integration-to-marketing-tool";
        private readonly BlobServiceClient _blobServiceClient;
        protected readonly ILogger<ReportService> _logger;
        private readonly IEmailNotificationService _emailNotificationService;

        public ReportService(BlobServiceClient blobServiceClient, ILogger<ReportService> logger, IEmailNotificationService emailNotificationService)
        {
            _blobServiceClient = blobServiceClient;
            _logger = logger;
            _emailNotificationService = emailNotificationService;
        }

        public string CreateImportSummaryReport(ImportSummaryResult summary)
        {
            var sb = new StringBuilder();
            sb.AppendLine("################################################################################");
            sb.AppendLine("#################### Import Summary Report ###############################");
            sb.AppendLine("################################################################################");
            sb.AppendLine($"Status: {summary.Status}");
            sb.AppendLine($"Start Time: {summary.StartTime:O}");
            sb.AppendLine($"End Time: {summary.EndTime:O}");
            sb.AppendLine($"Total Records From DB: {summary.TotalRecordsFromDb}");
            sb.AppendLine($"Total Records Processed: {summary.TotalRecordsProcessed}");

            AppendFieldMapping(sb, summary.FieldMapping);

            sb.AppendLine();

            AppendBatchResults(sb, summary.BatchResults);

            AppendMessages(sb, summary.Messages);

            sb.AppendLine("################################################################################");
            return sb.ToString();
        }

        private static void AppendFieldMapping(StringBuilder sb, string? fieldMapping)
        {
            if (!string.IsNullOrWhiteSpace(fieldMapping))
            {
                try
                {
                    var mappings = JsonSerializer.Deserialize<List<FieldMappingItem>>(fieldMapping);
                    if (mappings != null && mappings.Count > 0)
                    {
                        sb.AppendLine("Field Mapping:");
                        foreach (var map in mappings)
                        {
                            sb.AppendLine($"  Source: {map.Source} -> Target: {map.Target}");
                        }
                    }
                    else
                    {
                        sb.AppendLine("Field Mapping: None");
                    }
                }
                catch
                {
                    sb.AppendLine($"Field Mapping (raw): {fieldMapping}");
                }
            }
            else
            {
                sb.AppendLine("Field Mapping: None");
            }
        }

        private static void AppendBatchResults(StringBuilder sb, List<BatchResultDetail>? batchResults)
        {
            if (batchResults != null && batchResults.Count > 0)
            {
                sb.AppendLine($"Batch Results ({batchResults.Count}):");
                for (int i = 0; i < batchResults.Count; i++)
                {
                    var batch = batchResults[i];
                    sb.AppendLine($"  Batch {i + 1}:");
                    sb.AppendLine($"    BatchId: {batch.BatchId}");
                    sb.AppendLine($"    Status: {batch.Status}");
                    sb.AppendLine($"    RecordsProcessed: {batch.RecordsProcessed}");
                    if (!string.IsNullOrEmpty(batch.TokenFromEshot))
                        sb.AppendLine($"    TokenFromEshot: {batch.TokenFromEshot}");
                    if (!string.IsNullOrEmpty(batch.Error))
                        sb.AppendLine($"    Error: {batch.Error}");
                }
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine("No batch results available.");
            }
        }

        private static void AppendMessages(StringBuilder sb, List<string>? messages)
        {
            if (messages != null && messages.Count > 0)
            {
                sb.AppendLine("Messages:");
                foreach (var msg in messages)
                {
                    sb.AppendLine($"  - {msg}");
                }
            }
            else
            {
                sb.AppendLine("No messages.");
            }
        }

        public async Task SaveReportToBlob(string reportContent, string fileName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(_container);
                await containerClient.CreateIfNotExistsAsync();

                var reportBlobClient = containerClient.GetBlobClient($"Report/{fileName}.report.txt");

                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(reportContent));
                await reportBlobClient.UploadAsync(stream, overwrite: true);

                _logger.LogInformation("Report file saved: {FileName}.report.txt", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save report file.");
                throw;
            }
        }

        public async Task SaveReportToBlobAndNotifyAsync(string reportContent, string fileName, string integrationName)
        {
            try
            {
                // Save report to blob storage
                var containerClient = _blobServiceClient.GetBlobContainerClient(_container);
                await containerClient.CreateIfNotExistsAsync();

                var reportBlobClient = containerClient.GetBlobClient($"Report/{fileName}.report.txt");

                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(reportContent));
                await reportBlobClient.UploadAsync(stream, overwrite: true);

                _logger.LogInformation("Report file saved: {FileName}.report.txt", fileName);

                // Get blob URL
                var blobUrl = reportBlobClient.Uri.ToString();

                // Send email notification with both content and blob URL
                await _emailNotificationService.SendMonitoringReportAsync(integrationName, reportContent, blobUrl);

                _logger.LogInformation("Monitoring report email sent for integration: {IntegrationName}, Blob: {BlobUrl}", integrationName, blobUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save report file or send email notification.");
                throw;
            }
        }

        // Helper class for deserialization
        sealed class FieldMappingItem
        {
            public string? Source { get; set; }
            public string? Target { get; set; }
        }
    }
}
