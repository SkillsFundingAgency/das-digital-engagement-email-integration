using Azure.Storage.Blobs;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.Models.Import;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace DAS.DigitalEngagement.Application.Services
{
    public class ReportService : IReportService
    {
        private readonly string _container = "email-integration-to-marketing-tool";
        private readonly BlobServiceClient _blobServiceClient;
        protected readonly ILogger<ReportService> _logger;

        public ReportService(BlobServiceClient blobServiceClient, ILogger<ReportService> logger)
        {
            _blobServiceClient = blobServiceClient;
            _logger = logger;
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

            // Format Field Mapping
            if (!string.IsNullOrWhiteSpace(summary.FieldMapping))
            {
                try
                {
                    var mappings = JsonSerializer.Deserialize<List<FieldMappingItem>>(summary.FieldMapping);
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
                    sb.AppendLine($"Field Mapping (raw): {summary.FieldMapping}");
                }
            }
            else
            {
                sb.AppendLine("Field Mapping: None");
            }

            sb.AppendLine();

            if (summary.BatchResults != null && summary.BatchResults.Count > 0)
            {
                sb.AppendLine($"Batch Results ({summary.BatchResults.Count}):");
                for (int i = 0; i < summary.BatchResults.Count; i++)
                {
                    var batch = summary.BatchResults[i];
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

            if (summary.Messages != null && summary.Messages.Count > 0)
            {
                sb.AppendLine("Messages:");
                foreach (var msg in summary.Messages)
                {
                    sb.AppendLine($"  - {msg}");
                }
            }
            else
            {
                sb.AppendLine("No messages.");
            }

            sb.AppendLine("################################################################################");
            return sb.ToString();
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

                _logger.LogInformation($"Report file saved: {fileName}.report.txt");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save report file.");
                throw;
            }
        }

        // Helper class for deserialization
        private class FieldMappingItem
        {
            public string? Source { get; set; }
            public string? Target { get; set; }
        }
    }
}
