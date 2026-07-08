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

        public async Task<ImportSummaryResult> Handle(IList<DataMartSettings> config)
        {
            var summary = new ImportSummaryResult
            {
                StartTime = DateTime.UtcNow,
                Messages = new List<string>()
            };

            if (!config.Any(x => x.ObjectName == "Lead"))
            {
                summary.Status = BatchStatus.Failed;
                summary.EndTime = DateTime.UtcNow;
                summary.Messages.Add("Expected Object name is configured in the Configuration");
                return summary;
            }

            if (!await _importService.IsContactImportTemplatesExist())
            {
                _logger.LogWarning("Contact import template is not availabel in E-shot.");
                summary.Status = BatchStatus.Failed;
                summary.EndTime = DateTime.UtcNow;
                summary.Messages.Add("Contact import template is not available in E-shot.");
                return summary;
            }

            _logger.LogInformation("DataMart Handler is about to handle employer lead import");

            var data = await _dataMartRepository.RetrieveEmployeeRegistrationData();

            if (data != null && data.Count > 0)
            {
                _logger.LogInformation("DataMart Handler retrieved {RecordCount} records for employer lead import", data.Count);
                return await _importService.ImportEmployeeRegistration(data);
            }

            _logger.LogInformation("DataMart Handler did not retrieve any records for employer lead import");
            summary.Status = BatchStatus.Completed;
            summary.EndTime = DateTime.UtcNow;
            summary.Messages.Add("No records to import.");
            return summary;
        }
    }
}
