using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.Models.Import;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json.Nodes;

namespace DAS.DigitalEngagement.Application.Services
{
    public class ImportService : IImportService
    {
        private readonly ILogger<ImportService> _logger;
        private readonly IExternalApiService _externalApiService;
        private readonly IPayLoadMapper _payLoadMapper;
        private readonly IChunkingService _chunkingService;
        private readonly ICsvService _csvService;
        private readonly IReadOnlyList<DataMartSettings> _dataMartSettings;


        public ImportService(IExternalApiService externalApiService,
            ILogger<ImportService> logger,
            IPayLoadMapper payLoadMapper,
            IChunkingService chunkingService,
            ICsvService csvService,
            IList<DataMartSettings> dataMartSettings)
        {
            _externalApiService = externalApiService;
            _logger = logger;
            _payLoadMapper = payLoadMapper;
            _chunkingService = chunkingService;
            _csvService = csvService;
            _dataMartSettings = dataMartSettings.ToList().AsReadOnly();
        }

        public async Task<bool> IsContactImportTemplatesExist()
        {
            DataMartSettings empRegistrationSettings = GetDataMartConfig("Lead");
            
            // Check if all template IDs exist
            foreach (var templateId in empRegistrationSettings.TemplatedUploadId)
            {
                var filter = WebUtility.UrlEncode($"ID eq {templateId}");
                var importResult = await _externalApiService.GetDataAsync($"ContactImportTemplates/?$filter={filter}");
                int count = JsonNode.Parse(importResult)?["value"]?.AsArray()?.Count ?? 0;
                
                if (count < 1)
                {
                    _logger.LogWarning($"Template ID {templateId} not found");
                    return false;
                }
            }

            return true;
        }

        public async Task<ImportSummaryResult> ImportEmployeeRegistration<T>(IList<T> leads)
        {
            DataMartSettings empRegistrationSettings = GetDataMartConfig("Lead");

            var summary = new ImportSummaryResult
            {
                StartTime = DateTime.UtcNow,
                Messages = new List<string>(),
                TotalRecordsFromDb = leads?.Count ?? 0,
                Status = "Partial",
                FieldMapping = empRegistrationSettings.FieldMapping
            };

            try
            {
                var safeLeads = leads ?? new List<T>();
                var byteCount = _csvService.GetByteCount(safeLeads);
                var contactsChunks = _chunkingService.GetChunks(byteCount, safeLeads).ToList();
                int batchIndex = 0;
                
                // Process each template ID
                foreach (var templateId in empRegistrationSettings.TemplatedUploadId)
                {
                    foreach (var contactsList in contactsChunks)
                    {   
                        batchIndex++;
                        var payLoad = _payLoadMapper.MapToPayload(contactsList, empRegistrationSettings.ObjectName);
                        var csvString = _csvService.ToCsv(payLoad.ToList());

                        var importResult = await _externalApiService.PostDataAsync(
                            $"ContactImports/TemplatedUpload({templateId})", csvString);

                        importResult.BatchId = $"Template {templateId} - Batch: {batchIndex}";

                        await VerifyContactImport(importResult);

                        summary.BatchResults.Add(importResult);

                        _logger.LogInformation($"Called External API to import employee registrations for Template ID {templateId}.");
                    }
                }
            }
            catch (Exception ex)
            {
                summary.Status = "Failed";
                summary.Messages.Add($"Import failed: {ex.Message}");
                _logger.LogError(ex, "Error during employee registration import.");
            }
            finally
            {
                summary.EndTime = DateTime.UtcNow;
                if (summary.Status != "Failed")
                {
                    summary.Status = summary.BatchResults.All(b => b.Status == "Completed") ? "Completed" : "Partial";
                }
                summary.Messages.Add("Import completed.");
            }

            return summary;
        }

        private async Task VerifyContactImport(BatchResultDetail importResult)
        {
            ExtractToken(importResult);

            // Wait for 10 sec before checking the import status otherwise we get the "Waiting" response
            await Task.Delay(TimeSpan.FromSeconds(10));

            // The token value should be wrapped in single quotes for OData filter
            var filter = WebUtility.UrlEncode($"Token eq '{importResult.TokenFromEshot}'");
            var response = await _externalApiService.GetDataAsync($"ContactImports?$filter={filter}");

            var node = JsonNode.Parse(response)?["value"]?.AsArray()?.FirstOrDefault();
            if (node != null)
            {
                importResult.RecordsProcessed = node["ContactsImported"]?.GetValue<int?>() ?? 0;
                var error = node["ImportStatus"]?.GetValue<string>() ?? importResult.Status;
                importResult.Status = error == "Error" ? "Failed" : "Completed";
                importResult.Error = node["AdditionalInfo"]?.GetValue<string>();

                if (importResult.Status == "Failed")
                {
                    var errorInfo = $"Import error: {importResult.Error}";
                    _logger.LogError(errorInfo);
                }
            }
        }

        private void ExtractToken(BatchResultDetail importResult)
        {
            // Parse the token if it's a JSON string like {"Token":"..."}
            if (!string.IsNullOrEmpty(importResult.TokenFromEshot) && importResult.TokenFromEshot.TrimStart().StartsWith("{"))
            {
                try
                {
                    var tokenNode = JsonNode.Parse(importResult.TokenFromEshot);
                    var tokenValue = tokenNode?["Token"]?.GetValue<string>();
                    if (!string.IsNullOrEmpty(tokenValue))
                    {
                        importResult.TokenFromEshot = tokenValue;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse TokenFromEshot JSON.");
                }
            }
        }

        private DataMartSettings GetDataMartConfig(string objectName)
        {
            var config = _dataMartSettings.FirstOrDefault(x => x.ObjectName == objectName);
            if (config is null)
            {
                throw new InvalidOperationException("Employee registration config is missing");
            }
            return config;
        }
    }
}
