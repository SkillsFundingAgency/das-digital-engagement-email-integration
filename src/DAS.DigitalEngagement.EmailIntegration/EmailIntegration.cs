using DAS.DigitalEngagement.Application.Handlers.Import.Interfaces;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DAS.DigitalEngagement.EmailIntegration;

public class EmailIntegration
{
    protected readonly ILogger<EmailIntegration> _logger;
    private readonly IImportDataMartHandler _importDataMartHandler;
    private readonly ApplicationConfiguration _configuration;

    public EmailIntegration(ILogger<EmailIntegration> logger, IImportDataMartHandler importDataMartHandler,
        ApplicationConfiguration configuration)
    {
        _logger = logger;
        _importDataMartHandler = importDataMartHandler;
        _configuration = configuration;
    }

    [Function("EmailIntegration")]
    public async Task RunAsync([TimerTrigger("0 0 22 * * *")] TimerInfo myTimer)
    {
        // 0 0 22 * * * Everyday at 10pm
        _logger.LogInformation("Timer trigger function executed at: {ExecutionTime}", DateTime.Now);
        _logger.LogInformation(
            "Connection string: {ConnectionString}, API Base URL: {ApiBaseUrl}",
            _configuration.ConnectionString,
            _configuration.EShotAPIM?.ApiBaseUrl
        );

        try
        {
            var importSummary = await _importDataMartHandler.Handle(_configuration.DataMart);

            _logger.LogInformation("Import Summary: {ImportSummary}", importSummary.ToString());
            _logger.LogInformation("Email Integration Job completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email Integration Job failed with an exception");
        }
    }
}