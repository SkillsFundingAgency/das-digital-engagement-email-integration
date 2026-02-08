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
            var filter = WebUtility.UrlEncode( $"ID eq {empRegistrationSettings.TemplatedUploadId}");
            var importResult = await _externalApiService.GetDataAsync($"ContactImportTemplates/?$filter={filter}");
            int count = JsonNode.Parse(importResult)?["value"]?.AsArray()?.Count ?? 0;

            return count >= 1;

        }

        public async Task<ImportSummaryResult> ImportEmployeeRegistration<T>(IList<T> leads)
        {
            DataMartSettings empRegistrationSettings = GetDataMartConfig("Lead");

            var summary = new ImportSummaryResult
            {
                StartTime = DateTime.UtcNow,
                Messages = new List<string>(),
                TotalRecordsFromDb = leads?.Count ?? 0,
                Status = "Partial"
            };

            try
            {
                var safeLeads = leads ?? new List<T>();
                var byteCount = _csvService.GetByteCount(safeLeads);
                var contactsChunks = _chunkingService.GetChunks(byteCount, safeLeads).ToList();
                int index = 0;
                foreach (var contactsList in contactsChunks)
                {   
                    index++;
                    var payLoad = _payLoadMapper.MapToPayload(contactsList, empRegistrationSettings.ObjectName);
                    var csvString = _csvService.ToCsv(payLoad.ToList());
              
                    var importResult = await _externalApiService.PostDataAsync(
                        $"ContactImports/TemplatedUpload({empRegistrationSettings.TemplatedUploadId})", csvString);
                    
                    importResult.BatchId = $"Batch : {index}";
                    importResult.RecordsProcessed = contactsList.Count;
                    summary.BatchResults.Add(importResult);

                    _logger.LogInformation("Called External API to import employee registrations.");
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
