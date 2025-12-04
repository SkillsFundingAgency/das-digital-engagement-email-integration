using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DAS.DigitalEngagement.EmailIntegration;

public class EmailIntegration
{
    private readonly ILogger _logger;

    public EmailIntegration(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<EmailIntegration>();
    }

    [Function("EmailIntegration")]
    public void Run([TimerTrigger("*/5 * * * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation("C# Timer trigger function executed at: {executionTime}", DateTime.Now);
        
        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next timer schedule at: {nextSchedule}", myTimer.ScheduleStatus.Next);
        }
    }
}