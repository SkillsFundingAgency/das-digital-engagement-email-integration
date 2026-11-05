using DAS.DigitalEngagement.Application.Handlers.CampaignToStaging;
using DAS.DigitalEngagement.Application.PerformanceImport.Handlers;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.Models.Infrastructure;
using DAS.DigitalEngagement.Models.PerformanceImport;
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
        try
        {
            var (summary, importException) = await ExecuteImportAsync(cancellationToken);
            var report = CreateReport(summary, importException, stopwatch);
            await SaveReportAsync(report, reportFilename, importException, stopwatch, cancellationToken);
            ThrowIfImportFailed(importException, stopwatch);
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

    private async Task<(PerformanceImportReportSummary? Summary, Exception? ImportException)> ExecuteImportAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var summary = await importCampaignStagingHandler.Handle(cancellationToken);
            if (summary is null)
            {
                throw new InvalidOperationException("Performance data import handler returned a null summary.");
            }

            logger.LogInformation("Performance data import completed successfully.");
            return (summary, null);
        }
        catch (PerformanceImportFailedException ex)
        {
            return (ex.Summary, ex);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return (null, ex);
        }
    }

    private string CreateReport(
        PerformanceImportReportSummary? summary,
        Exception? importException,
        Stopwatch stopwatch)
    {
        try
        {
            return summary is not null
                ? performanceImportReportService.CreatePerformanceImportReport(summary)
                : $"Performance Data Import failed after {stopwatch.Elapsed.TotalSeconds:F2} seconds. Exception: {importException}";
        }
        catch (Exception ex)
        {
            if (importException is not null)
            {
                throw new InvalidOperationException(
                    $"Performance data import failed and report creation also failed after {stopwatch.Elapsed.TotalSeconds:F2} seconds.",
                    new AggregateException(importException, ex));
            }

            throw new InvalidOperationException(
                $"Performance data import completed, but its report could not be created after {stopwatch.Elapsed.TotalSeconds:F2} seconds.",
                ex);
        }
    }

    private async Task SaveReportAsync(
        string report,
        string reportFilename,
        Exception? importException,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        try
        {
            await reportService.SaveReportToBlobInternalAsync(report, reportFilename);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (importException is not null)
            {
                throw new InvalidOperationException(
                    $"Performance data import failed and report save also failed after {stopwatch.Elapsed.TotalSeconds:F2} seconds.",
                    new AggregateException(importException, ex));
            }

            throw new InvalidOperationException(
                $"Performance data import completed, but its report could not be saved after {stopwatch.Elapsed.TotalSeconds:F2} seconds.",
                ex);
        }
    }

    private static void ThrowIfImportFailed(Exception? importException, Stopwatch stopwatch)
    {
        if (importException is null)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Performance data import failed after {stopwatch.Elapsed.TotalSeconds:F2} seconds.",
            importException);
    }
}