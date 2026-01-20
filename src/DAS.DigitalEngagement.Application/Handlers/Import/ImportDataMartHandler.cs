using DAS.DigitalEngagement.Application.Handlers.Import.Interfaces;
using DAS.DigitalEngagement.Application.Repositories.Interfaces;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.Models.Import;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Extensions.Logging;

namespace DAS.DigitalEngagement.Application.Import.Handlers
{

    public class ImportDataMartHandler : IImportDataMartHandler
    {
        private readonly IDataMartRepository _dataMartRepository;
        protected readonly ILogger<ImportDataMartHandler> _logger;
        private readonly IImportService _importService;

        public ImportDataMartHandler(ILogger<ImportDataMartHandler> logger,
            IImportService importService,
            IDataMartRepository dataMartRepository)
        {
            _logger = logger;
            _dataMartRepository = dataMartRepository;
            _importService = importService;
        }

        public async Task<BulkImportStatus> Handle(DataMartSettings config)
        {
            _logger.LogInformation($"about to handle employer lead import");

            var data = await _dataMartRepository.RetrieveEmployeeRegistrationData(config.ViewName ?? "");

            if (config.ObjectName == "Lead")
            {
                var status = await _importService.ImportEmployeeRegistration(data);

                return status;
            }
            else
            {
                _logger.LogInformation($"No Object name is configured in the Configuration");
                // Return a default BulkImportStatus instance to satisfy non-nullable contract
                return new BulkImportStatus
                {
                    Container = string.Empty,
                    Name = string.Empty,
                    Id = string.Empty,
                    StartTime = DateTime.UtcNow,
                    BulkImportJobs = new List<BulkImportJob>(),
                    BulkImportJobStatus = new List<BulkImportJobStatus>(),
                    ImportFileIsValid = false,
                    ValidationError = "No Object name is configured in the Configuration",
                    HeaderErrors = Enumerable.Empty<string>()
                };
            }
        }

    }
}
