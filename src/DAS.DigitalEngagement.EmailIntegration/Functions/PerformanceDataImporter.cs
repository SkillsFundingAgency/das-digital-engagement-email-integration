using DAS.DigitalEngagement.Application.Handlers.Campaigns;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace DAS.DigitalEngagement.EmailIntegration.Functions;

public class PerformanceDataImporter(
    IImportCampaignPerformanceHandler importCampaignPerformanceHandler,
    ApplicationConfiguration configuration,
    ILogger<PerformanceDataImporter> logger)
{
    [Function("PerformanceDataImporter")]
    public async Task Run([TimerTrigger("%PerformanceDataImportSchedule%")] TimerInfo myTimer)
    {
        logger.LogInformation("Performance Data Importer started at: {DateTime}", DateTime.Now);
        logger.LogInformation(
            "Connection string: {ConnectionString}, API Base URL: {ApiBaseUrl}",
            configuration.ConnectionString.CampaignsDatabase,
            configuration.EmailMarketingApi?.ApiBaseUrl
        );

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await importCampaignPerformanceHandler.Handle();
            logger.LogInformation("Performance data import ran successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error importing performance data");
        }
        finally
        {
            stopwatch.Stop();

            logger.LogInformation(
                "Performance data import finished in {ElapsedMs} ms ({ElapsedSeconds} seconds).",
                stopwatch.ElapsedMilliseconds,
                stopwatch.Elapsed.TotalSeconds);
        }

        if (myTimer.IsPastDue)
        {
            logger.LogWarning("Timer schedule status: overdue");
        }
    }
}