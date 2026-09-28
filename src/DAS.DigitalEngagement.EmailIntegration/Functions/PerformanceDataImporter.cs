//using DAS.DigitalEngagement.Application.Handlers.Campaigns;
using DAS.DigitalEngagement.Application.Handlers.CampaignToStaging;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace DAS.DigitalEngagement.EmailIntegration.Functions;

public class PerformanceDataImporter(
    IImportCampaignStagingHandler importCampaignStagingHandler,
    ApplicationConfiguration configuration,
    ILogger<PerformanceDataImporter> logger)
{
    [Function("PerformanceDataImporter")]
    public async Task Run(
        [TimerTrigger("%PerformanceDataImportSchedule%", RunOnStartup = true)] TimerInfo myTimer,
        CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;
        logger.LogInformation(
            "Performance data import started at {StartedAt}.",
            startedAt);
        logger.LogInformation(
            "Performance data import using API base URL {ApiBaseUrl}.",
            configuration.EmailMarketingApi?.ApiBaseUrl
        );

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await importCampaignStagingHandler.Handle(cancellationToken);
            logger.LogInformation("Performance data import completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Performance data import failed after {ElapsedSeconds} seconds.", stopwatch.Elapsed.TotalSeconds);
            throw;
        }
        finally
        {
            stopwatch.Stop();

            logger.LogInformation(
                "Performance data import finished at {FinishedAt} in {ElapsedMs} ms ({ElapsedSeconds} seconds).",
                startedAt.Add(stopwatch.Elapsed),
                stopwatch.ElapsedMilliseconds,
                stopwatch.Elapsed.TotalSeconds);
        }
    }
}