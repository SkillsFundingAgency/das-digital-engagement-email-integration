using DAS.DigitalEngagement.Application.Handlers.CampaignToStaging;
using DAS.DigitalEngagement.Application.PerformanceImport.Handlers;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace DAS.DigitalEngagement.EmailIntegration.Functions;

public class PerformanceDataImporter(
    IImportCampaignStagingHandler importCampaignStagingHandler,
    ApplicationConfiguration configuration,
    IPerformanceImportReportService performanceImportReportService,
    IReportService reportService,
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
        var reportFilename = $"Performance-Import/Import-{startedAt.ToString("s", System.Globalization.CultureInfo.InvariantCulture)}";
        string report;

        try
        {
            var summary = await importCampaignStagingHandler.Handle(cancellationToken);
            logger.LogInformation("Performance data import completed successfully.");

            report = performanceImportReportService.CreatePerformanceImportReport(summary);

            await reportService.SaveReportToBlobInternalAsync(report, reportFilename);
        }
        catch (PerformanceImportFailedException ex)
        {
            report = performanceImportReportService.CreatePerformanceImportReport(ex.Summary);
            await reportService.SaveReportToBlobInternalAsync(report, reportFilename);

            throw new InvalidOperationException(
                $"Performance data import failed after {stopwatch.Elapsed.TotalSeconds:F2} seconds.",
                ex);
        }
        catch (Exception ex)
        {
            report = $"Performance Data Import failed after {stopwatch.Elapsed.TotalSeconds:F2} seconds. Exception: {ex}";
            await reportService.SaveReportToBlobInternalAsync(report, reportFilename);

            throw new InvalidOperationException(
                $"Performance data import failed after {stopwatch.Elapsed.TotalSeconds:F2} seconds.",
                ex);
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