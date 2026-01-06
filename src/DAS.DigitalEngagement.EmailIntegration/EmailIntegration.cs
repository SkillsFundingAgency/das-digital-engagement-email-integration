using DAS.DigitalEngagement.Application.Handlers.Import.Interfaces;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;

namespace DAS.DigitalEngagement.EmailIntegration;

public class EmailIntegration
{
    private readonly ILogger _logger;
    private readonly IImportDataMartHandler _importDataMartHandler;
    public EmailIntegration(ILoggerFactory loggerFactory, IImportDataMartHandler importDataMartHandler)
    {
        _logger = loggerFactory.CreateLogger<EmailIntegration>();
        _importDataMartHandler = importDataMartHandler;
    }

    [Function("EmailIntegration")]
    public async Task RunAsync([TimerTrigger("*/5 * * * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation("C# Timer trigger function executed at: {executionTime}", DateTime.Now);

        if (myTimer.ScheduleStatus is not null)
        {
            // var importJobsStatus = await _importDataMartHandler.Handle(dataMartConfig);
            var importJobsStatus = await _importDataMartHandler.Handle(new DataMartSettings() { ViewName = "Test" });

            _logger.LogInformation("Next timer schedule at: {nextSchedule}", myTimer.ScheduleStatus.Next);
        }
    }
}