using System.Net.Mail;
using Microsoft.Extensions.Logging;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.Models.Infrastructure;

namespace DAS.DigitalEngagement.Application.Services;

public class EmailNotificationService : IEmailNotificationService
{
    private readonly INotificationClientWrapper _notificationClient;
    private readonly GovNotifyConfiguration _configuration;
    private readonly ILogger<EmailNotificationService> _logger;
    private readonly IEmailDomainChecker _emailDomainChecker;

    public EmailNotificationService(
        GovNotifyConfiguration configuration,
        ILogger<EmailNotificationService> logger,
        INotificationClientWrapper notificationClient,
        IEmailDomainChecker emailDomainChecker)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _notificationClient = notificationClient ?? throw new ArgumentNullException(nameof(notificationClient));
        _emailDomainChecker = emailDomainChecker ?? throw new ArgumentNullException(nameof(emailDomainChecker));

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

            if (!MailAddress.TryCreate(trimmedRecipient, out _))
            {
                _logger.LogWarning(
                    "Invalid email address '{EmailAddress}' configured for integration {IntegrationName}. Skipping.",
                    trimmedRecipient,
                    integrationName);
                failedCount++;
                continue;
            }

            bool domainValid;
            try
            {
                domainValid = await _emailDomainChecker.IsValidDomainAsync(trimmedRecipient, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Domain lookup failed validating email '{EmailAddress}' for integration {IntegrationName}. Skipping.",
                    trimmedRecipient, integrationName);
                failedCount++;
                continue;
            }

            if (!domainValid)
            {
                _logger.LogWarning(
                    "Email domain appears invalid for '{EmailAddress}' for integration {IntegrationName}. Skipping.",
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

                if (response == null || string.IsNullOrWhiteSpace(response.id))
                {
                    _logger.LogError(
                        "Failed to send monitoring report email to {EmailAddress} for integration {IntegrationName} (no notification id).",
                        trimmedRecipient, integrationName);
                    failedCount++;
                }
                else
                {
                    _logger.LogInformation(
                        "Monitoring report email sent to {EmailAddress} for integration {IntegrationName}. Notification ID: {NotificationId}",
                        trimmedRecipient, integrationName, response.id);

                    sentCount++;
                }
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
