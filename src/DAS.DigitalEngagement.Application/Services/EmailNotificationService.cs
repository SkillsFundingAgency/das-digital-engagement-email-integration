using Notify.Models.Responses;
using Microsoft.Extensions.Logging;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.Models.Infrastructure;

namespace DAS.DigitalEngagement.Application.Services;

public class EmailNotificationService : IEmailNotificationService
{
    private readonly INotificationClientWrapper _notificationClient;
    private readonly GovNotifyConfiguration _configuration;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(
        GovNotifyConfiguration configuration,
        ILogger<EmailNotificationService> logger,
        INotificationClientWrapper notificationClient)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _notificationClient = notificationClient ?? throw new ArgumentNullException(nameof(notificationClient));
        
        if (string.IsNullOrWhiteSpace(_configuration.ApiKey))
        {
            throw new InvalidOperationException("GovUK Notify API Key is not configured");
        }
    }

    public async Task SendMonitoringReportAsync(string integrationName, string reportContent, string blobUrl, CancellationToken cancellationToken = default)
    {
        if (_configuration.RecipientEmailAddresses == null || _configuration.RecipientEmailAddresses.Count == 0)
        {
            _logger.LogWarning("No recipient email addresses configured for monitoring report");
            return;
        }

        var personalisation = new Dictionary<string, dynamic>
        {
            { "integration_name", integrationName },
            { "report_content", reportContent },
            { "blob_url", blobUrl },
            { "report_date", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC") }
        };

        var sentCount = 0;
        var failedCount = 0;

        foreach (var recipient in _configuration.RecipientEmailAddresses)
        {
            try
            {
                var response = await _notificationClient.SendEmailAsync(
                    recipient,
                    _configuration.MonitoringReportTemplateId,
                    personalisation);

                _logger.LogInformation(
                    "Monitoring report email sent to {EmailAddress} for integration {IntegrationName}. Notification ID: {NotificationId}", 
                    recipient, integrationName, response.id);
                
                sentCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Failed to send monitoring report email to {EmailAddress} for integration {IntegrationName}", 
                    recipient, integrationName);
                failedCount++;
            }
        }

        _logger.LogInformation(
            "Monitoring report email batch completed for integration {IntegrationName}. Sent: {SentCount}, Failed: {FailedCount}", 
            integrationName, sentCount, failedCount);

        if (failedCount == _configuration.RecipientEmailAddresses.Count)
        {
            throw new InvalidOperationException($"Failed to send monitoring report to all recipients for integration {integrationName}");
        }
    }
}
