using DAS.DigitalEngagement.Application.Handlers.Campaigns;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DAS.DigitalEngagement.EmailIntegration;

public class PerformanceDataImporter
{
    private readonly IImportCampaignPerformanceHandler _importCampaignPerformanceHandler;
    private readonly ILogger<PerformanceDataImporter> _logger;

    public PerformanceDataImporter(IImportCampaignPerformanceHandler importCampaignPerformanceHandler, ILogger<PerformanceDataImporter> logger)
    {
        _importCampaignPerformanceHandler = importCampaignPerformanceHandler;
        _logger = logger;
    }

    [Function("PerformanceDataImporter")]
    public async Task Run([TimerTrigger("%PerformanceDataImportSchedule%", RunOnStartup = true)] TimerInfo myTimer)
    {
        _logger.LogInformation("Performance Data Importer started at: {DateTime}", DateTime.Now);

        try
        {            
            await _importCampaignPerformanceHandler.Handle();
            _logger.LogInformation("Performance data import ran successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing performance data");
        }

        if (myTimer.IsPastDue)
        {
            _logger.LogWarning("Timer schedule status: overdue");
        }
    }
}