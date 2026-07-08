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
        private readonly EmailMarketingApi _emailMarketingApi;


        public ImportService(IExternalApiService externalApiService,
            ILogger<ImportService> logger,
            IPayLoadMapper payLoadMapper,
            IChunkingService chunkingService,
            ICsvService csvService,
            IList<DataMartSettings> dataMartSettings,
            EmailMarketingApi emailMarketingApi) 
        {
            _externalApiService = externalApiService;
            _logger = logger;
            _payLoadMapper = payLoadMapper;
            _chunkingService = chunkingService;
            _csvService = csvService;
            _dataMartSettings = dataMartSettings.ToList().AsReadOnly();
            _emailMarketingApi = emailMarketingApi;
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
                    _logger.LogWarning("Template ID {TemplateId} not found", templateId);
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
                BatchResults = new List<BatchResultDetail>(),
                TotalRecordsFromDb = leads?.Count ?? 0,
                Status = BatchStatus.Partial,
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

                        _logger.LogInformation("Batch {BatchId}: Sending {RecordCount} records to external API for Template ID {TemplateId}", batchIndex, contactsList.Count, templateId);

                        var importResult = await _externalApiService.PostDataAsync(
                            $"ContactImports/TemplatedUpload({templateId})", csvString);

                        importResult.BatchId = $"Template {templateId} - Batch: {batchIndex}";

                        await VerifyContactImport(importResult, batchIndex);

                        summary.BatchResults.Add(importResult);

                        _logger.LogInformation("Batch {BatchId} completed: Status={Status}, RecordsReceived={RecordsReceived}, RecordsProcessed={RecordsProcessed}, RecordsFailed={RecordsFailed}, IsPartiallyImported={IsPartiallyImported}", 
                            batchIndex, importResult.Status, importResult.RecordsReceived, importResult.RecordsProcessed, importResult.RecordsFailed, importResult.IsPartiallyImported);
                    }
                }
            }
            catch (Exception ex)
            {
                summary.Status = BatchStatus.Failed;
                summary.Messages.Add($"Import failed: {ex.Message}");
                _logger.LogError(ex, "Error during employee registration import.");
            }
            finally
            {
                summary.EndTime = DateTime.UtcNow;
                if (summary.Status != BatchStatus.Failed)
                {
                    summary.Status = summary.BatchResults.All(b => b.Status == BatchStatus.Completed) ? BatchStatus.Completed : BatchStatus.Partial;
                }
                summary.Messages.Add("Import completed.");
            }

            return summary;
        }

        private async Task VerifyContactImport(BatchResultDetail importResult, int batchIndex)
        {
            ExtractToken(importResult);

            if (string.IsNullOrEmpty(importResult.TokenFromEshot))
            {
                _logger.LogError("Batch {BatchId}: No token received from API", batchIndex);
                importResult.Status = BatchStatus.Failed;
                importResult.Error = "No token received from external API";
                return;
            }

            _logger.LogInformation("Batch {BatchId}: Verifying import with token: {Token}", batchIndex, importResult.TokenFromEshot);

            int maxRetries = _emailMarketingApi.ApiRetryCount > 0 
                ? _emailMarketingApi.ApiRetryCount 
                : 5;

            await RetryVerificationWithBackoff(importResult, batchIndex, maxRetries);
        }

        private async Task RetryVerificationWithBackoff(BatchResultDetail importResult, int batchIndex, int maxRetries)
        {
            const int baseDelaySeconds = 10;
            
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                await Task.Delay(TimeSpan.FromSeconds(baseDelaySeconds * attempt));

                var verificationResult = await TryVerifyImportStatus(importResult, batchIndex, attempt, maxRetries);
                
                if (verificationResult == VerificationResult.Success)
                {
                    return;
                }
                
                if (verificationResult == VerificationResult.Failed)
                {
                    return;
                }
                
                // Continue retrying for VerificationResult.Retry
            }

            // All retries exhausted
            _logger.LogError("Batch {BatchId}: Failed to verify import after {MaxRetries} attempts", batchIndex, maxRetries);
            importResult.Status = BatchStatus.Failed;
            importResult.Error = $"Failed to verify import status after {maxRetries} attempts";
        }

        private async Task<VerificationResult> TryVerifyImportStatus(BatchResultDetail importResult, int batchIndex, int attempt, int maxRetries)
        {
            try
            {
                var filter = WebUtility.UrlEncode($"Token eq '{importResult.TokenFromEshot}'");
                var response = await _externalApiService.GetDataAsync($"ContactImports?$filter={filter}");

                var node = JsonNode.Parse(response)?["value"]?.AsArray()?.FirstOrDefault();
                if (node == null)
                {
                    return HandleMissingNode(importResult, batchIndex, attempt, maxRetries);
                }

                var importStatus = node["ImportStatus"]?.GetValue<string>();
                
                if (IsStillProcessing(importStatus))
                {
                    return HandleProcessingStatus(batchIndex, attempt, maxRetries, importStatus);
                }

                PopulateImportResult(importResult, node);
                SetFinalStatus(importResult, batchIndex, importStatus);
                
                return VerificationResult.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Batch {BatchId}: Attempt {Attempt}/{MaxRetries} - Error verifying contact import", 
                    batchIndex, attempt, maxRetries);
                
                if (attempt >= maxRetries)
                {
                    importResult.Status = BatchStatus.Failed;
                    importResult.Error = "Failed to verify import status";
                    return VerificationResult.Failed;
                }
                
                return VerificationResult.Retry;
            }
        }

        private VerificationResult HandleMissingNode(BatchResultDetail importResult, int batchIndex, int attempt, int maxRetries)
        {
            _logger.LogWarning("Batch {BatchId}: Attempt {Attempt}/{MaxRetries} - No data found for token {Token}", 
                batchIndex, attempt, maxRetries, importResult.TokenFromEshot);
            
            if (attempt < maxRetries)
            {
                return VerificationResult.Retry;
            }
            
            importResult.Status = BatchStatus.Failed;
            importResult.Error = $"No import status found for token after {maxRetries} attempts";
            return VerificationResult.Failed;
        }

        private bool IsStillProcessing(string importStatus)
        {
            return importStatus == "Waiting" || importStatus == "Processing";
        }

        private VerificationResult HandleProcessingStatus(int batchIndex, int attempt, int maxRetries, string importStatus)
        {
            _logger.LogInformation("Batch {BatchId}: Attempt {Attempt}/{MaxRetries} - Import still processing (Status: {Status})", 
                batchIndex, attempt, maxRetries, importStatus);
            
            return attempt < maxRetries ? VerificationResult.Retry : VerificationResult.Success;
        }

        private void PopulateImportResult(BatchResultDetail importResult, JsonNode node)
        {
            importResult.RecordsReceived = node["ContactsReceived"]?.GetValue<int?>() ?? 0;
            importResult.RecordsProcessed = node["ContactsImported"]?.GetValue<int?>() ?? 0;
            importResult.RecordsFailed = importResult.RecordsReceived - importResult.RecordsProcessed;
            
            importResult.IsPartiallyImported = node["IsPartiallyImport"]?.GetValue<bool?>() 
                ?? node["IsPartiallyImported"]?.GetValue<bool?>() 
                ?? false;
            
            importResult.AdditionalInfo = node["AdditionalInfo"]?.GetValue<string>();
        }

        private void SetFinalStatus(BatchResultDetail importResult, int batchIndex, string importStatus)
        {
            if (importStatus == "Error")
            {
                if (importResult.IsPartiallyImported && importResult.RecordsProcessed > 0)
                {
                    importResult.Status = BatchStatus.Completed;
                    _logger.LogWarning("Batch {BatchId}: Partial import - {RecordsProcessed}/{RecordsReceived} records imported. {RecordsFailed} failed. Reason: {AdditionalInfo}", 
                        batchIndex, importResult.RecordsProcessed, importResult.RecordsReceived, importResult.RecordsFailed, importResult.AdditionalInfo);
                }
                else
                {
                    importResult.Status = BatchStatus.Failed;
                    importResult.Error = importResult.AdditionalInfo;
                    _logger.LogError("Batch {BatchId}: Import failed - {Error}", batchIndex, importResult.Error);
                }
            }
            else
            {
                importResult.Status = BatchStatus.Completed;
                _logger.LogInformation("Batch {BatchId}: Import successful - {RecordsProcessed}/{RecordsReceived} records imported", 
                    batchIndex, importResult.RecordsProcessed, importResult.RecordsReceived);
            }

            _logger.LogInformation("Batch {BatchId}: Import verification completed - ContactsReceived={ContactsReceived}, ContactsImported={ContactsImported}, RecordsFailed={RecordsFailed}, IsPartiallyImported={IsPartiallyImported}, ImportStatus={ImportStatus}", 
                batchIndex, importResult.RecordsReceived, importResult.RecordsProcessed, importResult.RecordsFailed, importResult.IsPartiallyImported, importStatus);
        }

        private enum VerificationResult
        {
            Success,
            Failed,
            Retry
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
