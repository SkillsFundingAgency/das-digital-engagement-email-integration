using DAS.DigitalEngagement.Application.Repositories;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.Models.Import;
using Microsoft.Extensions.Logging;

namespace DAS.DigitalEngagement.Application.Services
{
    public class ImportService : IImportService
    {
        private readonly ILogger<ImportService> _logger;
        private readonly IExternalApiService _externalApiService;

        public ImportService(IExternalApiService externalApiService,
            ILogger<ImportService> logger)
        {
           _externalApiService = externalApiService;
            _logger = logger;
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

            // ToDo : Call the API and return the result
            await _externalApiService.GetDataAsync("Contacts/Export/?$filter=ID eq 182");
            _logger.LogInformation("Called External API to import employee registrations.");

            fileStatus.BulkImportJobs.Add(new BulkImportJob (){ batchId=1,ImportId="1",Status="Failed" });

            return await Task.FromResult(fileStatus);
        }
    }
}
