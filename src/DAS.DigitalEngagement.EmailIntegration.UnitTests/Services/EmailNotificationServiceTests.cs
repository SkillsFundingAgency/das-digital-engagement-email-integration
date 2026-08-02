using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using DAS.DigitalEngagement.Application.Services;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.Models.Infrastructure;
using Notify.Models.Responses;

namespace DAS.DigitalEngagement.EmailIntegration.UnitTests.Services;

[TestFixture]
public class EmailNotificationServiceTests
{
    private Mock<ILogger<EmailNotificationService>> _mockLogger;
    private Mock<INotificationClientWrapper> _mockNotificationClient;
    private Mock<IEmailDomainChecker> _mockEmailDomainChecker;
    private GovNotifyConfiguration _configuration;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<EmailNotificationService>>();
        _mockNotificationClient = new Mock<INotificationClientWrapper>();
        _mockEmailDomainChecker = new Mock<IEmailDomainChecker>();
        _configuration = new GovNotifyConfiguration
        {
            ApiKey = "test_service_id-1a234567-89ab-cdef-0123-456789abcdef-1a234567-89ab-cdef-0123-456789abcdef",
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
            new EmailNotificationService(null, _mockLogger.Object, _mockNotificationClient.Object, _mockEmailDomainChecker.Object));
    }

    [Test]  
    public void Constructor_WhenLoggerIsNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new EmailNotificationService(_configuration, null, _mockNotificationClient.Object, _mockEmailDomainChecker.Object));
    }

    [Test]
    public void Constructor_WhenNotificationClientIsNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new EmailNotificationService(_configuration, _mockLogger.Object, null, _mockEmailDomainChecker.Object));
    }

    [Test]
    public void Constructor_WhenApiKeyIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        _configuration.ApiKey = null;

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => 
            new EmailNotificationService(_configuration, _mockLogger.Object, _mockNotificationClient.Object, _mockEmailDomainChecker.Object));
        
        Assert.That(ex.Message, Is.EqualTo("GovUK Notify API Key is not configured"));
    }

    [Test]
    public void Constructor_WhenApiKeyIsEmpty_ThrowsInvalidOperationException()
    {
        // Arrange
        _configuration.ApiKey = string.Empty;

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => 
            new EmailNotificationService(_configuration, _mockLogger.Object, _mockNotificationClient.Object, _mockEmailDomainChecker.Object));
        
        Assert.That(ex.Message, Is.EqualTo("GovUK Notify API Key is not configured"));
    }

    [Test]
    public void Constructor_WhenApiKeyIsWhitespace_ThrowsInvalidOperationException()
    {
        // Arrange
        _configuration.ApiKey = "   ";

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => 
            new EmailNotificationService(_configuration, _mockLogger.Object, _mockNotificationClient.Object, _mockEmailDomainChecker.Object));
        
        Assert.That(ex.Message, Is.EqualTo("GovUK Notify API Key is not configured"));
    }

    [Test]
    public void Constructor_WhenValidConfiguration_CreatesInstance()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => 
            new EmailNotificationService(_configuration, _mockLogger.Object, _mockNotificationClient.Object, _mockEmailDomainChecker.Object));
    }

    #endregion

    #region SendMonitoringReportAsync - No Recipients Tests

    [Test]
    public async Task SendMonitoringReportAsync_WhenNoRecipients_LogsWarningAndReturns()
    {
        // Arrange
        _configuration.RecipientEmailAddresses = null;
        var service = new EmailNotificationService(_configuration, _mockLogger.Object, _mockNotificationClient.Object, _mockEmailDomainChecker.Object);

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
        
        _mockNotificationClient.Verify(
            x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>()),
            Times.Never);
    }

    [Test]
    public async Task SendMonitoringReportAsync_WhenRecipientsListIsEmpty_LogsWarningAndReturns()
    {
        // Arrange
        _configuration.RecipientEmailAddresses = new List<string>();
        var service = new EmailNotificationService(_configuration, _mockLogger.Object, _mockNotificationClient.Object, _mockEmailDomainChecker.Object);

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
        
        _mockNotificationClient.Verify(
            x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>()),
            Times.Never);
    }

    #endregion

    #region SendMonitoringReportAsync - Success Scenarios

    [Test]
    public async Task SendMonitoringReportAsync_WhenSingleRecipientSucceeds_LogsSuccessMessages()
    {
        // Arrange
        var mockResponse = new EmailNotificationResponse
        {
            id = "notification-id-123"
        };
        
        _mockNotificationClient
            .Setup(x => x.SendEmailAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<Dictionary<string, dynamic>>()))
            .ReturnsAsync(mockResponse);

        // Ensure domain check passes
        _mockEmailDomainChecker
            .Setup(x => x.IsValidDomainAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new EmailNotificationService(_configuration, _mockLogger.Object, _mockNotificationClient.Object, _mockEmailDomainChecker.Object);

        // Act
        await service.SendMonitoringReportAsync("TestIntegration", "Report Content", "https://blob.url", CancellationToken.None);

        // Assert
        _mockNotificationClient.Verify(
            x => x.SendEmailAsync(
                "test@example.com",
                "template-id-123",
                It.Is<Dictionary<string, object>>(d => (string)d["integration_name"] == "TestIntegration" && (string)d["report_content"] == "Report Content" && (string)d["blob_url"] == "https://blob.url" &&
                    d.ContainsKey("report_date"))),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Monitoring report email sent to") && 
                                          v.ToString().Contains("notification-id-123")),
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

        var mockResponse = new EmailNotificationResponse { id = "notification-id-123" };
        
        _mockNotificationClient
            .Setup(x => x.SendEmailAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<Dictionary<string, dynamic>>()))
            .ReturnsAsync(mockResponse);

        // Ensure domain check passes
        _mockEmailDomainChecker
            .Setup(x => x.IsValidDomainAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new EmailNotificationService(_configuration, _mockLogger.Object, _mockNotificationClient.Object, _mockEmailDomainChecker.Object);

        // Act
        await service.SendMonitoringReportAsync("TestIntegration", "Report Content", "https://blob.url", CancellationToken.None);

        // Assert
        _mockNotificationClient.Verify(
            x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, dynamic>>()),
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
        var mockResponse = new EmailNotificationResponse { id = "notification-id-123" };
        Dictionary<string, dynamic> capturedPersonalisation = null;
        
        _mockNotificationClient
            .Setup(x => x.SendEmailAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<Dictionary<string, dynamic>>()))
            .Callback<string, string, Dictionary<string, dynamic>>((email, template, personalisation) =>
            {
                capturedPersonalisation = personalisation;
            })
            .ReturnsAsync(mockResponse);

        // Ensure domain check passes so SendEmailAsync is invoked
        _mockEmailDomainChecker
            .Setup(x => x.IsValidDomainAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new EmailNotificationService(_configuration, _mockLogger.Object, _mockNotificationClient.Object, _mockEmailDomainChecker.Object);

        // Act
        await service.SendMonitoringReportAsync("TestIntegration", "Report Content", "https://blob.url", CancellationToken.None);

        // Assert
        Assert.That(capturedPersonalisation, Is.Not.Null);
        Assert.That(capturedPersonalisation, Contains.Key("report_date"));
        Assert.That(capturedPersonalisation["report_date"].ToString(), Does.Contain("UTC"));
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

        var mockResponse = new EmailNotificationResponse { id = "notification-id-123" };
        
        _mockNotificationClient
            .SetupSequence(x => x.SendEmailAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<Dictionary<string, dynamic>>()))
            .ReturnsAsync(mockResponse)
            .ThrowsAsync(new Exception("Failed to send"))
            .ReturnsAsync(mockResponse);

        // Ensure domain check passes
        _mockEmailDomainChecker
            .Setup(x => x.IsValidDomainAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new EmailNotificationService(_configuration, _mockLogger.Object, _mockNotificationClient.Object, _mockEmailDomainChecker.Object);

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

    #endregion

    [Test]
    public async Task SendMonitoringReportAsync_WhenAllRecipientsFail_LogsError()
    {
        // Arrange - multiple recipients but Notify client always fails
        _configuration.RecipientEmailAddresses = new List<string> { "fail1@example.com", "fail2@example.com" };

        _mockNotificationClient
            .Setup(x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, dynamic>>()))
            .ThrowsAsync(new Exception("Failed to send"));

        // Ensure domain check passes (so failures are simulated by client)
        _mockEmailDomainChecker
            .Setup(x => x.IsValidDomainAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new EmailNotificationService(_configuration, _mockLogger.Object, _mockNotificationClient.Object, _mockEmailDomainChecker.Object);

        // Act
        await service.SendMonitoringReportAsync("MyIntegration", "report", "https://blob.url", CancellationToken.None);

        // Assert - SendEmailAsync attempted for each recipient
        _mockNotificationClient.Verify(
            x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, dynamic>>()),
            Times.Exactly(2));

        // Assert - final error logged when all recipients fail, includes integration name
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
        var mockResponse = new EmailNotificationResponse { id = "notification-id-123" };
        
        _mockNotificationClient
            .Setup(x => x.SendEmailAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<Dictionary<string, dynamic>>() ))
            .ReturnsAsync(mockResponse);

        // Ensure domain check passes
        _mockEmailDomainChecker
            .Setup(x => x.IsValidDomainAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new EmailNotificationService(_configuration, _mockLogger.Object, _mockNotificationClient.Object, _mockEmailDomainChecker.Object);

        // Act & Assert
         Assert.DoesNotThrowAsync(async () =>
            await service.SendMonitoringReportAsync(null, "Report Content", "https://blob.url", CancellationToken.None));

        _mockNotificationClient.Verify(
            x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<Dictionary<string, object>>(d => d["integration_name"] == null)),
            Times.Once);
    }

    [Test]
    public async Task SendMonitoringReportAsync_WhenReportContentIsNull_StillSendsEmail()
    {
        // Arrange
        var mockResponse = new EmailNotificationResponse { id = "notification-id-123" };
        
        _mockNotificationClient
            .Setup(x => x.SendEmailAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<Dictionary<string, dynamic>>() ))
            .ReturnsAsync(mockResponse);

        // Ensure domain check passes
        _mockEmailDomainChecker
            .Setup(x => x.IsValidDomainAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new EmailNotificationService(_configuration, _mockLogger.Object, _mockNotificationClient.Object, _mockEmailDomainChecker.Object);

        // Act & Assert
         Assert.DoesNotThrowAsync(async () =>
            await service.SendMonitoringReportAsync("TestIntegration", null, "https://blob.url", CancellationToken.None));

        _mockNotificationClient.Verify(
            x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<Dictionary<string, object>>(d => d["report_content"] == null)),
            Times.Once);
    }

    [Test]
    public async Task SendMonitoringReportAsync_WhenBlobUrlIsNull_StillSendsEmail()
    {
        // Arrange
        var mockResponse = new EmailNotificationResponse { id = "notification-id-123" };
        
        _mockNotificationClient
            .Setup(x => x.SendEmailAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<Dictionary<string, dynamic>>() ))
            .ReturnsAsync(mockResponse);

        // Ensure domain check passes
        _mockEmailDomainChecker
            .Setup(x => x.IsValidDomainAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new EmailNotificationService(_configuration, _mockLogger.Object, _mockNotificationClient.Object, _mockEmailDomainChecker.Object);

        // Act & Assert
         Assert.DoesNotThrowAsync(async () =>
            await service.SendMonitoringReportAsync("TestIntegration", "Report Content", null, CancellationToken.None));

        _mockNotificationClient.Verify(
            x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<Dictionary<string, object>>(d => d["blob_url"] == null)),
            Times.Once);
    }

    [Test]
    public async Task SendMonitoringReportAsync_WhenCancellationTokenProvided_CompletesSuccessfully()
    {
        // Arrange
        var mockResponse = new EmailNotificationResponse { id = "notification-id-123" };
        
        _mockNotificationClient
            .Setup(x => x.SendEmailAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<Dictionary<string, dynamic>>()))
            .ReturnsAsync(mockResponse);

        _mockEmailDomainChecker
            .Setup(x => x.IsValidDomainAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new EmailNotificationService(_configuration, _mockLogger.Object, _mockNotificationClient.Object, _mockEmailDomainChecker.Object);
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
        var mockResponse = new EmailNotificationResponse { id = "notification-id-123" };
        
        _mockNotificationClient
            .Setup(x => x.SendEmailAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<Dictionary<string, dynamic>>()))
            .ReturnsAsync(mockResponse);

        _mockEmailDomainChecker
            .Setup(x => x.IsValidDomainAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new EmailNotificationService(_configuration, _mockLogger.Object, _mockNotificationClient.Object, _mockEmailDomainChecker.Object);

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
            Times.Exactly(2)); // Once for individual send, once for batch summary
    }

    [Test]
    public async Task SendMonitoringReportAsync_LogsRecipientEmailInSuccessMessage()
    {
        // Arrange
        var recipientEmail = "specific@example.com";
        _configuration.RecipientEmailAddresses = new List<string> { recipientEmail };
        var mockResponse = new EmailNotificationResponse { id = "notification-id-123" };
        
        _mockNotificationClient
            .Setup(x => x.SendEmailAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<Dictionary<string, dynamic>>()))
            .ReturnsAsync(mockResponse);

        _mockEmailDomainChecker
            .Setup(x => x.IsValidDomainAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new EmailNotificationService(_configuration, _mockLogger.Object, _mockNotificationClient.Object, _mockEmailDomainChecker.Object);

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

        var mockResponse = new EmailNotificationResponse { id = "notification-id-123" };
        var attemptedEmails = new List<string>();

        _mockNotificationClient
            .Setup(x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, dynamic>>()))
            .Callback<string, string, Dictionary<string, dynamic>>((email, template, personalisation) =>
            {
                attemptedEmails.Add(email);
            })
            .ReturnsAsync(mockResponse);

        _mockEmailDomainChecker
            .Setup(x => x.IsValidDomainAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new EmailNotificationService(_configuration, _mockLogger.Object, _mockNotificationClient.Object, _mockEmailDomainChecker.Object);

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
                It.IsAny<Func<It.IsAnyType, Exception, string>>() ),
            Times.AtLeastOnce);

        // Assert - SendEmailAsync invoked exactly for the two valid recipients
        _mockNotificationClient.Verify(
            x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, dynamic>>()),
            Times.Exactly(2));
    }


    [Test]
    public async Task SendMonitoringReportAsync_SkipsWhitespaceRecipient_LogsWarning()
    {
        // Arrange - single recipient that is whitespace
        _configuration.RecipientEmailAddresses = new List<string> { "   " };

        var service = new EmailNotificationService(_configuration, _mockLogger.Object, _mockNotificationClient.Object, _mockEmailDomainChecker.Object);

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
        _mockNotificationClient.Verify(
            x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>()),
            Times.Never);
    }
}