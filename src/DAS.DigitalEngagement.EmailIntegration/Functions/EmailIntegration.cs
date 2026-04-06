using DAS.DigitalEngagement.Application.Handlers.Import.Interfaces;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DAS.DigitalEngagement.EmailIntegration.Functions;

public class EmailIntegration
{
    protected readonly ILogger<EmailIntegration> _logger;
    private readonly IImportDataMartHandler _importDataMartHandler;
    private readonly ApplicationConfiguration _configuration;
    private readonly IReportService _reportService;

    public EmailIntegration(ILogger<EmailIntegration> logger, 
        IImportDataMartHandler importDataMartHandler,
        ApplicationConfiguration configuration,
        IReportService reportService)
    {
        _logger = logger;
        _importDataMartHandler = importDataMartHandler;
        _configuration = configuration;
        _reportService = reportService;
    }

    [Function("EmailIntegration")]
    public async Task RunAsync([TimerTrigger("%EmailIntegrationSchedule%")] TimerInfo myTimer)
    {
        // 0 0 22 * * * Everyday at 10pm
        _logger.LogInformation("Timer trigger function executed at: {ExecutionTime}", DateTime.Now);
        _logger.LogInformation(
            "Connection string: {ConnectionString}, API Base URL: {ApiBaseUrl}",
            _configuration.ConnectionString,
            _configuration.EmailMarketingApi?.ApiBaseUrl
        );

        var reportFilename = $"Data-Mart/Lead/Import-{DateTime.UtcNow.ToString("s", System.Globalization.CultureInfo.InvariantCulture)}";
        string report;

        try
        {
            var importSummary = await _importDataMartHandler.Handle(_configuration.DataMart);

            report =_reportService.CreateImportSummaryReport(importSummary);
            _logger.LogInformation("Import Summary: {ImportSummary}", importSummary.ToString());
            _logger.LogInformation("Email Integration Job completed successfully");

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email Integration Job failed with an exception");
            report = $"Unable to import custom object Lead: {ex}";

        }

        await _reportService.SaveReportToBlob(report, reportFilename);
    }
}