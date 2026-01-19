
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
        private readonly ILogger<ImportDataMartHandler> _logger;
        private readonly IImportService _importService;

        public ImportDataMartHandler(ILoggerFactory loggerFactory,
            IImportService importService,
            IDataMartRepository dataMartRepository)
        {
            _logger = loggerFactory.CreateLogger<ImportDataMartHandler>();
            _dataMartRepository = dataMartRepository;
            _importService = importService;
        }

        public async Task<BulkImportStatus> Handle(DataMartSettings config)
        {
            _logger.LogInformation($"about to handle employer lead import");


            var data = await _dataMartRepository.RetrieveEmployeeRegistrationData(config.ViewName ?? "");

            if (config.ObjectName == "Main")
            {
                var status = await _importService.ImportEmployeeRegistration(data);

                return status;
            }
            else
            {
                //  var status = await _bulkImportService.ImportCustomObject(data, config.ObjectName);

                //  return status;
                // To Do
                return null;


            }


        }

    }
}
