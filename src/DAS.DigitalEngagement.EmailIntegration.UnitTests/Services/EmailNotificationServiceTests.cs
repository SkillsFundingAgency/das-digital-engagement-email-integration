using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using DAS.DigitalEngagement.Application.Services;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.Models.Infrastructure;
using SFA.DAS.Notifications.Messages.Commands;

namespace DAS.DigitalEngagement.EmailIntegration.UnitTests.Services;

[TestFixture]
public class EmailNotificationServiceTests
{
    private Mock<ILogger<EmailNotificationService>> _mockLogger;
    private Mock<INotificationService> _mockNotificationService;
    private Mock<IEmailDomainChecker> _mockEmailDomainChecker;
    private GovNotifyConfiguration _configuration;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<EmailNotificationService>>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockEmailDomainChecker = new Mock<IEmailDomainChecker>();
        _configuration = new GovNotifyConfiguration
        {
            MonitoringReportTemplateId = "template-id-123",
            RecipientEmailAddresses = new List<string> { "test@example.com" }
        };
    }

    #region Constructor Tests

    [Test]
    public void Constructor_WhenConfigurationIsNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new EmailNotificationService(null, _mockLogger.Object, _mockNotificationService.Object, _mockEmailDomainChecker.Object));
    }

    [Test]
    public void Constructor_WhenLoggerIsNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new EmailNotificationService(_configuration, null, _mockNotificationService.Object, _mockEmailDomainChecker.Object));
    }

    [Test]
    public void Constructor_WhenNotificationServiceIsNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new EmailNotificationService(_configuration, _mockLogger.Object, null, _mockEmailDomainChecker.Object));
    }

    [Test]
    public void Constructor_WhenValidConfiguration_CreatesInstance()
    {
        // Act & Assert
        Assert.DoesNotThrow(() =>
            new EmailNotificationService(_configuration, _mockLogger.Object, _mockNotificationService.Object, _mockEmailDomainChecker.Object));
    }

    #endregion

    #region SendMonitoringReportAsync - No Recipients Tests

    [Test]
    public async Task SendMonitoringReportAsync_WhenNoRecipients_LogsWarningAndReturns()
    {
        // Arrange
        _configuration.RecipientEmailAddresses = null;
        var service = new EmailNotificationService(_configuration, _mockLogger.Object, _mockNotificationService.Object, _mockEmailDomainChecker.Object);

        // Act
        await service.SendMonitoringReportAsync("TestIntegration", "Report Content", "https://blob.url", CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("No recipient email addresses configured")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _mockNotificationService.Verify(
            x => x.Send(It.IsAny<SendEmailCommand>()),
            Times.Never);
    }

    [Test]
    public async Task SendMonitoringReportAsync_WhenRecipientsListIsEmpty_LogsWarningAndReturns()
    {
        // Arrange
        _configuration.RecipientEmailAddresses = new List<string>();
        var service = new EmailNotificationService(_configuration, _mockLogger.Object, _mockNotificationService.Object, _mockEmailDomainChecker.Object);

        // Act
        await service.SendMonitoringReportAsync("TestIntegration", "Report Content", "https://blob.url", CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("No recipient email addresses configured")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _mockNotificationService.Verify(
            x => x.Send(It.IsAny<SendEmailCommand>()),
            Times.Never);
    }

    #endregion

    #region SendMonitoringReportAsync - Success Scenarios

    [Test]
    public async Task SendMonitoringReportAsync_WhenSingleRecipientSucceeds_LogsSuccessMessages()
    {
        // Arrange
        _mockNotificationService
            .Setup(x => x.Send(It.IsAny<SendEmailCommand>()))
            .Returns(Task.CompletedTask);

        // Ensure domain check passes
        _mockEmailDomainChecker
            .Setup(x => x.IsValidDomainAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new EmailNotificationService(_configuration, _mockLogger.Object, _mockNotificationService.Object, _mockEmailDomainChecker.Object);

        // Act
        await service.SendMonitoringReportAsync("TestIntegration", "Report Content", "https://blob.url", CancellationToken.None);

        // Assert
        _mockNotificationService.Verify(
            x => x.Send(It.Is<SendEmailCommand>(c =>
                c.TemplateId == "template-id-123" &&
                c.RecipientsAddress == "test@example.com" &&
                c.Tokens["integration_name"] == "TestIntegration" &&
                c.Tokens["report_content"] == "Report Content" &&
                c.Tokens["blob_url"] == "https://blob.url" &&
                c.Tokens.ContainsKey("report_date"))),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Monitoring report email sent to") &&
                                          v.ToString().Contains("test@example.com")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Sent: 1, Failed: 0")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public async Task SendMonitoringReportAsync_WhenMultipleRecipients_SendsToAll()
    {
        // Arrange
        _configuration.RecipientEmailAddresses = new List<string>
        {
            "test1@example.com",
            "test2@example.com",
            "test3@example.com"
        };

        _mockNotificationService
            .Setup(x => x.Send(It.IsAny<SendEmailCommand>()))
            .Returns(Task.CompletedTask);

        // Ensure domain check passes
        _mockEmailDomainChecker
            .Setup(x => x.IsValidDomainAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new EmailNotificationService(_configuration, _mockLogger.Object, _mockNotificationService.Object, _mockEmailDomainChecker.Object);

        // Act
        await service.SendMonitoringReportAsync("TestIntegration", "Report Content", "https://blob.url", CancellationToken.None);

        // Assert
        _mockNotificationService.Verify(
            x => x.Send(It.IsAny<SendEmailCommand>()),
            Times.Exactly(3));

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Sent: 3, Failed: 0")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public async Task SendMonitoringReportAsync_IncludesReportDateInPersonalisation()
    {
        // Arrange
        SendEmailCommand capturedCommand = null;

        _mockNotificationService
            .Setup(x => x.Send(It.IsAny<SendEmailCommand>()))
            .Callback<SendEmailCommand>(command => capturedCommand = command)
            .Returns(Task.CompletedTask);

        // Ensure domain check passes so Send is invoked
        _mockEmailDomainChecker
            .Setup(x => x.IsValidDomainAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new EmailNotificationService(_configuration, _mockLogger.Object, _mockNotificationService.Object, _mockEmailDomainChecker.Object);

        // Act
        await service.SendMonitoringReportAsync("TestIntegration", "Report Content", "https://blob.url", CancellationToken.None);

        // Assert
        Assert.That(capturedCommand, Is.Not.Null);
        Assert.That(capturedCommand.Tokens, Contains.Key("report_date"));
        Assert.That(capturedCommand.Tokens["report_date"], Does.Contain("UTC"));
    }

    #endregion

    #region SendMonitoringReportAsync - Failure Scenarios

    [Test]
    public async Task SendMonitoringReportAsync_WhenSomeRecipientsFail_LogsErrorsAndContinues()
    {
        // Arrange
        _configuration.RecipientEmailAddresses = new List<string>
        {
            "test1@example.com",
            "test2@example.com",
            "test3@example.com"
        };

        _mockNotificationService
            .SetupSequence(x => x.Send(It.IsAny<SendEmailCommand>()))
            .Returns(Task.CompletedTask)
            .ThrowsAsync(new Exception("Failed to send"))
            .Returns(Task.CompletedTask);

        // Ensure domain check passes
        _mockEmailDomainChecker
            .Setup(x => x.IsValidDomainAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new EmailNotificationService(_configuration, _mockLogger.Object, _mockNotificationService.Object, _mockEmailDomainChecker.Object);

        // Act
        await service.SendMonitoringReportAsync("TestIntegration", "Report Content", "https://blob.url", CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to send monitoring report email")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Sent: 2, Failed: 1")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public async Task SendMonitoringReportAsync_WhenAllRecipientsFail_LogsError()
    {
        _configuration.RecipientEmailAddresses = new List<string> { "fail1@example.com", "fail2@example.com" };

        _mockNotificationService
            .Setup(x => x.Send(It.IsAny<SendEmailCommand>()))
            .ThrowsAsync(new Exception("Failed to send"));

        _mockEmailDomainChecker
            .Setup(x => x.IsValidDomainAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new EmailNotificationService(_configuration, _mockLogger.Object, _mockNotificationService.Object, _mockEmailDomainChecker.Object);

        await service.SendMonitoringReportAsync("MyIntegration", "report", "https://blob.url", CancellationToken.None);

        _mockNotificationService.Verify(
            x => x.Send(It.IsAny<SendEmailCommand>()),
            Times.Exactly(2));

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to send monitoring report to all recipients for integration") && v.ToString().Contains("MyIntegration")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public async Task SendMonitoringReportAsync_WhenIntegrationNameIsNull_StillSendsEmail()
    {
        // Arrange
        _mockNotificationService
            .Setup(x => x.Send(It.IsAny<SendEmailCommand>()))
            .Returns(Task.CompletedTask);

        // Ensure domain check passes
        _mockEmailDomainChecker
            .Setup(x => x.IsValidDomainAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new EmailNotificationService(_configuration, _mockLogger.Object, _mockNotificationService.Object, _mockEmailDomainChecker.Object);

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
           await service.SendMonitoringReportAsync(null, "Report Content", "https://blob.url", CancellationToken.None));

        _mockNotificationService.Verify(
            x => x.Send(It.Is<SendEmailCommand>(c => c.Tokens["integration_name"] == string.Empty)),
            Times.Once);
    }

    [Test]
    public async Task SendMonitoringReportAsync_WhenReportContentIsNull_StillSendsEmail()
    {
        // Arrange
        _mockNotificationService
            .Setup(x => x.Send(It.IsAny<SendEmailCommand>()))
            .Returns(Task.CompletedTask);

        // Ensure domain check passes
        _mockEmailDomainChecker
            .Setup(x => x.IsValidDomainAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new EmailNotificationService(_configuration, _mockLogger.Object, _mockNotificationService.Object, _mockEmailDomainChecker.Object);

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
           await service.SendMonitoringReportAsync("TestIntegration", null, "https://blob.url", CancellationToken.None));

        _mockNotificationService.Verify(
            x => x.Send(It.Is<SendEmailCommand>(c => c.Tokens["report_content"] == string.Empty)),
            Times.Once);
    }

    [Test]
    public async Task SendMonitoringReportAsync_WhenBlobUrlIsNull_StillSendsEmail()
    {
        // Arrange
        _mockNotificationService
            .Setup(x => x.Send(It.IsAny<SendEmailCommand>()))
            .Returns(Task.CompletedTask);

        // Ensure domain check passes
        _mockEmailDomainChecker
            .Setup(x => x.IsValidDomainAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new EmailNotificationService(_configuration, _mockLogger.Object, _mockNotificationService.Object, _mockEmailDomainChecker.Object);

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
           await service.SendMonitoringReportAsync("TestIntegration", "Report Content", null, CancellationToken.None));

        _mockNotificationService.Verify(
            x => x.Send(It.Is<SendEmailCommand>(c => c.Tokens["blob_url"] == string.Empty)),
            Times.Once);
    }

    [Test]
    public async Task SendMonitoringReportAsync_WhenCancellationTokenProvided_CompletesSuccessfully()
    {
        // Arrange
        _mockNotificationService
            .Setup(x => x.Send(It.IsAny<SendEmailCommand>()))
            .Returns(Task.CompletedTask);

        _mockEmailDomainChecker
            .Setup(x => x.IsValidDomainAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new EmailNotificationService(_configuration, _mockLogger.Object, _mockNotificationService.Object, _mockEmailDomainChecker.Object);

        var cts = new CancellationTokenSource();

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
           await service.SendMonitoringReportAsync("TestIntegration", "Report Content", "https://blob.url", cts.Token));
    }

    [Test]
    public async Task SendMonitoringReportAsync_LogsIntegrationNameInAllMessages()
    {
        // Arrange
        var integrationName = "CustomIntegration";

        _mockNotificationService
            .Setup(x => x.Send(It.IsAny<SendEmailCommand>()))
            .Returns(Task.CompletedTask);

        _mockEmailDomainChecker
            .Setup(x => x.IsValidDomainAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new EmailNotificationService(_configuration, _mockLogger.Object, _mockNotificationService.Object, _mockEmailDomainChecker.Object);

        // Act
        await service.SendMonitoringReportAsync(integrationName, "Report Content", "https://blob.url", CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(integrationName)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Exactly(3)); // Sending log, success log, and batch summary log
    }

    [Test]
    public async Task SendMonitoringReportAsync_LogsRecipientEmailInSuccessMessage()
    {
        // Arrange
        var recipientEmail = "specific@example.com";
        _configuration.RecipientEmailAddresses = new List<string> { recipientEmail };

        _mockNotificationService
            .Setup(x => x.Send(It.IsAny<SendEmailCommand>()))
            .Returns(Task.CompletedTask);

        _mockEmailDomainChecker
            .Setup(x => x.IsValidDomainAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new EmailNotificationService(_configuration, _mockLogger.Object, _mockNotificationService.Object, _mockEmailDomainChecker.Object);

        // Act
        await service.SendMonitoringReportAsync("TestIntegration", "Report Content", "https://blob.url", CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(recipientEmail)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
    }

    [Test]
    public async Task SendMonitoringReportAsync_WhenRecipientsContainInvalidEmails_SkipsInvalidAddressesAndLogsWarnings()
    {
        // Arrange
        var recipients = new List<string>
        {
            "valid1@example.com",
            "invalid-email",
            "valid2@example.com"
        };
        _configuration.RecipientEmailAddresses = recipients;

        var attemptedEmails = new List<string>();

        _mockNotificationService
            .Setup(x => x.Send(It.IsAny<SendEmailCommand>()))
            .Callback<SendEmailCommand>(command => attemptedEmails.Add(command.RecipientsAddress))
            .Returns(Task.CompletedTask);

        _mockEmailDomainChecker
            .Setup(x => x.IsValidDomainAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new EmailNotificationService(_configuration, _mockLogger.Object, _mockNotificationService.Object, _mockEmailDomainChecker.Object);

        // Act
        await service.SendMonitoringReportAsync("TestIntegration", "Report Content", "https://blob.url", CancellationToken.None);

        // Assert - only valid addresses were attempted
        Assert.That(attemptedEmails.Count, Is.EqualTo(2));
        Assert.That(attemptedEmails, Does.Contain("valid1@example.com"));
        Assert.That(attemptedEmails, Does.Contain("valid2@example.com"));
        Assert.That(attemptedEmails, Does.Not.Contain("invalid-email"));

        // Assert - a warning was logged containing the invalid email address
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("invalid-email")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);

        // Assert - Send invoked exactly for the two valid recipients
        _mockNotificationService.Verify(
            x => x.Send(It.IsAny<SendEmailCommand>()),
            Times.Exactly(2));
    }

    [Test]
    public async Task SendMonitoringReportAsync_SkipsWhitespaceRecipient_LogsWarning()
    {
        // Arrange - single recipient that is whitespace
        _configuration.RecipientEmailAddresses = new List<string> { "   " };

        var service = new EmailNotificationService(_configuration, _mockLogger.Object, _mockNotificationService.Object, _mockEmailDomainChecker.Object);

        // Act
        await service.SendMonitoringReportAsync("MyIntegration", "report", "https://blob.url", CancellationToken.None);

        // Assert - warning logged about skipping empty recipient and includes integration name
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Skipping empty recipient address configured for integration") && v.ToString().Contains("MyIntegration")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        // Assert - no attempt to send email
        _mockNotificationService.Verify(
            x => x.Send(It.IsAny<SendEmailCommand>()),
            Times.Never);
    }

    [Test]
    public async Task SendMonitoringReportAsync_DomainLookupThrows_LogsWarningAndSkipsRecipient()
    {
        // Arrange
        _configuration.RecipientEmailAddresses = new List<string> { "test@example.com" };
        var dnsEx = new Exception("dns lookup failed");

        _mockEmailDomainChecker
            .Setup(x => x.IsValidDomainAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(dnsEx);

        var service = new EmailNotificationService(_configuration, _mockLogger.Object, _mockNotificationService.Object, _mockEmailDomainChecker.Object);

        // Act
        await service.SendMonitoringReportAsync("IntegrationDomainThrow", "Report Content", "https://blob.url", CancellationToken.None);

        // Assert - domain lookup exception logged as a warning and contains the email
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Domain lookup failed validating email") && v.ToString().Contains("test@example.com")),
                dnsEx,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        // Assert - no attempt to send email
        _mockNotificationService.Verify(
            x => x.Send(It.IsAny<SendEmailCommand>()),
            Times.Never);

        // Assert - batch summary logged with Sent: 0, Failed: 1
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Sent: 0, Failed: 1")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        // Assert - final error logged because all recipients failed
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to send monitoring report to all recipients for integration") && v.ToString().Contains("IntegrationDomainThrow")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public async Task SendMonitoringReportAsync_DomainInvalid_LogsWarningAndSkipsRecipient()
    {
        // Arrange
        _configuration.RecipientEmailAddresses = new List<string> { "test@example.com" };

        _mockEmailDomainChecker
            .Setup(x => x.IsValidDomainAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new EmailNotificationService(_configuration, _mockLogger.Object, _mockNotificationService.Object, _mockEmailDomainChecker.Object);

        // Act
        await service.SendMonitoringReportAsync("IntegrationDomainInvalid", "Report Content", "https://blob.url", CancellationToken.None);

        // Assert - invalid domain warning logged and contains the email
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Email domain appears invalid for") && v.ToString().Contains("test@example.com")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        // Assert - no attempt to send email
        _mockNotificationService.Verify(
            x => x.Send(It.IsAny<SendEmailCommand>()),
            Times.Never);

        // Assert - batch summary logged with Sent: 0, Failed: 1
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Sent: 0, Failed: 1")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        // Assert - final error logged because all recipients failed
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to send monitoring report to all recipients for integration") && v.ToString().Contains("IntegrationDomainInvalid")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
    #endregion
}
