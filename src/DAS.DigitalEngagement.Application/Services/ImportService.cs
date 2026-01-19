using DAS.DigitalEngagement.Application.Repositories;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.Models.Import;
using Microsoft.Extensions.Logging;

namespace DAS.DigitalEngagement.Application.Services
{
    public class ImportService : IImportService
    {
        private readonly ILogger<DataMartRepository> _logger;
        private readonly IExternalApiService _externalApiService;

        public ImportService(IExternalApiService externalApiService,
            ILogger<DataMartRepository> logger)
        {
           _externalApiService = externalApiService;
            _logger = logger;
        }

        public Task<string> GetFailures(int jobId)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetWarnings(int jobId)
        {
            throw new NotImplementedException();
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
            var result = await _externalApiService.GetDataAsync("Contacts/Export/?$filter=ID eq 182");

            fileStatus.BulkImportJobs.Add(new BulkImportJob (){ batchId=1,ImportId="1",Status="Failed" });

            return await Task.FromResult(fileStatus);
        }
    }
}
