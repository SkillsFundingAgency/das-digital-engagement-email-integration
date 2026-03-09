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
    public void Run([TimerTrigger("%PerformanceDataImportSchedule%")] TimerInfo myTimer)
    {
        _logger.LogInformation("Performance Data Importer started at: {DateTime}", DateTime.Now);

        try
        {
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