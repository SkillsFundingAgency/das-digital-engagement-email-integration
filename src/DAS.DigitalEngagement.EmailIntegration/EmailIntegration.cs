using DAS.DigitalEngagement.Application.Handlers.Import.Interfaces;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

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
    public async Task RunAsync([TimerTrigger("*/5 * * * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation("C# Timer trigger function executed at: {executionTime}", DateTime.Now);
       _logger.LogInformation("Starting Email Integration Job" + _configuration.DataMart[0]);

        try
        {
            await _importDataMartHandler.Handle(_configuration.DataMart[0]);
            _logger.LogInformation("Email Integration Job completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email Integration Job failed with an exception");
        }



    }
}