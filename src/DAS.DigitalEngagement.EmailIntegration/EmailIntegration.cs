using DAS.DigitalEngagement.Application.Handlers.Import.Interfaces;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DAS.DigitalEngagement.EmailIntegration;

public class EmailIntegration
{
    private readonly ILogger _logger;
    private readonly IImportDataMartHandler _importDataMartHandler;
    private readonly ApplicationConfiguration _configuration;


    public EmailIntegration(ILoggerFactory loggerFactory, IImportDataMartHandler importDataMartHandler,
        ApplicationConfiguration configuration)
    {
        _logger = loggerFactory.CreateLogger<EmailIntegration>();
        _importDataMartHandler = importDataMartHandler;
        _configuration = configuration;
    }

    [Function("EmailIntegration")]
    public async Task RunAsync([TimerTrigger("%EmailIntegrationSchedule%")] TimerInfo myTimer)
    {
        // 0 0 22 * * * Everyday at 10pm
        _logger.LogInformation("C# Timer trigger function executed at: {executionTime}", DateTime.Now);
       _logger.LogInformation("Starting Email Integration Job" + _configuration.DataMart[0]);
        _logger.LogInformation("Connection string "+ _configuration.ConnectionString);
        _logger.LogInformation("Api Base Url " + _configuration?.EShotAPIM?.ApiBaseUrl);
        _logger.LogInformation("View Name " + _configuration?.DataMart[0].ViewName);


        try
        {
           var result = await _importDataMartHandler.Handle(_configuration.DataMart[0]);
            _logger.LogInformation("Email Integration Job completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email Integration Job failed with an exception");
        }
    }
}