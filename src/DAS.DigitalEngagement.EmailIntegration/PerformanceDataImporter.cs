using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DAS.DigitalEngagement.EmailIntegration;

public class PerformanceDataImporter
{
    private readonly ILogger<PerformanceDataImporter> _logger;

    public PerformanceDataImporter(ILogger<PerformanceDataImporter> logger)
    {
        _logger = logger;
    }

    [Function("PerformanceDataImporter")]
    public async Task RunAsync([TimerTrigger("*/10 * * * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation($"Performance Data Importer started at: {DateTime.Now}");

        try
        {
            _logger.LogInformation("Performance data import ran successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error importing performance data: {ex.Message}");
        }

        if (myTimer.IsPastDue)
        {
            _logger.LogWarning("Timer schedule status: overdue");
        }
    }
}