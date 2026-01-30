using DAS.DigitalEngagement.Application.Repositories;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.Models.Import;
using Microsoft.Extensions.Logging;
using System.Dynamic;

namespace DAS.DigitalEngagement.Application.Services
{
    public class ImportService : IImportService
    {
        private readonly ILogger<ImportService> _logger;
        private readonly IExternalApiService _externalApiService;
        private readonly IPayLoadMapper _payLoadMapper;
        private readonly IChunkingService _chunkingService;
        private readonly ICsvService _csvService;

        public ImportService(IExternalApiService externalApiService,
            ILogger<ImportService> logger,
            IPayLoadMapper payLoadMapper,
            IChunkingService chunkingService,ICsvService csvService)
        {
            _externalApiService = externalApiService;
            _logger = logger;
            _payLoadMapper = payLoadMapper;
            _chunkingService = chunkingService;
            _csvService = csvService;
        }

        public async Task<BulkImportStatus> ImportEmployeeRegistration<T>(IList<T> leads)
        {
            var fileStatus = new BulkImportStatus()
            {
                BulkImportJobStatus = new List<BulkImportJobStatus>(),
                Container = "Test",
                Id = "1",
                Name = "Test",
                ValidationError = "Test",
               
            };

            var contactsChunks = _chunkingService.GetChunks(_csvService.GetByteCount(leads), leads).ToList();

            // ToDo : Call the API and return the result
            // await _externalApiService.GetDataAsync("Contacts/Export/?$filter=ID eq 182");
            foreach (var contactsList in contactsChunks) {


                var payLoad = _payLoadMapper.MapToPayload(contactsList);

                var csvString1 = _csvService.ToCsv(contactsList);

                var csvString = _csvService.ToCsv(payLoad);
                
                var csvStreamBody = _csvService.GenerateStreamFromString(csvString);

                // await _externalApiService.PostDataAsync("Contacts/Save", body);
                 var importResult = await _externalApiService.PostDataAsync("ContactImports/TemplatedUpload(1)", csvString);

                _logger.LogInformation("Called External API to import employee registrations.");

                fileStatus.BulkImportJobs.Add(new BulkImportJob() { batchId = 1, ImportId = "1", Status = "Failed" });
            }
            return await Task.FromResult(fileStatus);
        }
    }
}
