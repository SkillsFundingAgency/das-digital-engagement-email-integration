using DAS.DigitalEngagement.Application.Handlers.Campaigns;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Configuration;
using System.Diagnostics;

namespace DAS.DigitalEngagement.EmailIntegration;

public class PerformanceDataImporter
{
    private readonly IImportCampaignPerformanceHandler _importCampaignPerformanceHandler;
    private readonly ILogger<PerformanceDataImporter> _logger;
    private readonly ApplicationConfiguration _configuration;

    public PerformanceDataImporter(
        IImportCampaignPerformanceHandler importCampaignPerformanceHandler,
        ApplicationConfiguration configuration,
        ILogger<PerformanceDataImporter> logger)
    {
        _importCampaignPerformanceHandler = importCampaignPerformanceHandler;
        _configuration = configuration;
        _logger = logger;
    }

    [Function("PerformanceDataImporter")]
    public async Task Run([TimerTrigger("%PerformanceDataImportSchedule%", RunOnStartup = true)] TimerInfo myTimer)
    {
        _logger.LogInformation("Performance Data Importer started at: {DateTime}", DateTime.Now);
        _logger.LogInformation(
            "Connection string: {ConnectionString}, API Base URL: {ApiBaseUrl}",
            _configuration.ConnectionString.DataMart,
            _configuration.EmailMarketingApi?.ApiBaseUrl
        );

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _importCampaignPerformanceHandler.Handle();
            _logger.LogInformation("Performance data import ran successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing performance data");
        }
        finally
        {
            stopwatch.Stop();

            _logger.LogInformation(
                "Performance data import finished in {ElapsedMs} ms ({ElapsedSeconds} seconds).",
                stopwatch.ElapsedMilliseconds,
                stopwatch.Elapsed.TotalSeconds);
        }

        if (myTimer.IsPastDue)
        {
            _logger.LogWarning("Timer schedule status: overdue");
        }
    }
}