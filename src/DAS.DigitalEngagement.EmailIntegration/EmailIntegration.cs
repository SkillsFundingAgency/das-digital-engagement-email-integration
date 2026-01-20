using DAS.DigitalEngagement.Application.Handlers.Import.Interfaces;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DAS.DigitalEngagement.EmailIntegration;

public class EmailIntegration
{
    // private readonly ILogger _logger;
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
    public async Task RunAsync([TimerTrigger("%EmailIntegrationSchedule%")] TimerInfo myTimer)
    {
        // 0 0 22 * * * Everyday at 10pm
        _logger.LogInformation("C# Timer trigger function executed at: {executionTime}", DateTime.Now);

        var dataMartSettings = _configuration.DataMart?.FirstOrDefault();
        if (dataMartSettings == null)
        {
            _logger.LogError("First DataMartSettings entry is null.");
            return;
        }

        _logger.LogInformation("Starting Email Integration Job" + dataMartSettings);
        _logger.LogInformation("Connection string " + _configuration.ConnectionString);
        _logger.LogInformation("Api Base Url " + _configuration?.EShotAPIM?.ApiBaseUrl);
        _logger.LogInformation("View Name " + dataMartSettings.ViewName);

        try
        {
            var result = await _importDataMartHandler.Handle(dataMartSettings);
            _logger.LogInformation("Email Integration Job completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email Integration Job failed with an exception");
        }
    }
}