using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.Models.Infrastructure;

namespace DAS.DigitalEngagement.Application.Services;

public class EmailNotificationService : IEmailNotificationService
{
    private readonly INotificationClientWrapper _notificationClient;
    private readonly GovNotifyConfiguration _configuration;
    private readonly ILogger<EmailNotificationService> _logger;

    private static readonly Regex EmailRegex = new(
                @"^[-!#$%&'*+/0-9=?A-Z^_a-z{|}~](\.?[-!#$%&'*+/0-9=?A-Z^_a-z{|}~])*@[a-zA-Z0-9_](-?[a-zA-Z0-9_])*(\.[a-zA-Z0-9](-?[a-zA-Z0-9])*)+$",
                 RegexOptions.Compiled |
                 RegexOptions.CultureInvariant |
                 RegexOptions.IgnoreCase,
                 TimeSpan.FromSeconds(2));

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
            var trimmedRecipient = recipient?.Trim();

            if (string.IsNullOrWhiteSpace(trimmedRecipient))
            {
                _logger.LogWarning("Skipping empty recipient address configured for integration {IntegrationName}", integrationName);
                failedCount++;
                continue;
            }

            if (!EmailRegex.IsMatch(trimmedRecipient))
            {
                _logger.LogWarning(
                    "Invalid email address '{EmailAddress}' configured for integration {IntegrationName}. Skipping.",
                    trimmedRecipient,
                    integrationName);
                failedCount++;
                continue;
            }

            try
            {
                var response = await _notificationClient.SendEmailAsync(
                    trimmedRecipient,
                    _configuration.MonitoringReportTemplateId,
                    personalisation);

                _logger.LogInformation(
                    "Monitoring report email sent to {EmailAddress} for integration {IntegrationName}. Notification ID: {NotificationId}", 
                    trimmedRecipient, integrationName, response.id);
                
                sentCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Failed to send monitoring report email to {EmailAddress} for integration {IntegrationName}", 
                    trimmedRecipient, integrationName);
                failedCount++;
            }
        }

        _logger.LogInformation(
            "Monitoring report email batch completed for integration {IntegrationName}. Sent: {SentCount}, Failed: {FailedCount}", 
            integrationName, sentCount, failedCount);

        if (failedCount == _configuration.RecipientEmailAddresses.Count)
        {
            _logger.LogError("Failed to send monitoring report to all recipients for integration {IntegrationName}", integrationName);
        }
    }
}
