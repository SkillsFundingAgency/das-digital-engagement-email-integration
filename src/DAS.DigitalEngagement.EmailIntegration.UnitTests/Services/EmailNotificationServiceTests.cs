using DAS.DigitalEngagement.Application.Services;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DAS.DigitalEngagement.Application.Tests.Services;

[TestFixture]
public class EmailNotificationServiceTests
{
    private Mock<ILogger<EmailNotificationService>> _mockLogger;
    private GovNotifyConfiguration _configuration;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<EmailNotificationService>>();
        _configuration = new GovNotifyConfiguration
        {
            ApiKey = "test_service_id-1a234567-89ab-cdef-0123-456789abcdef-1a234567-89ab-cdef-0123-456789abcdef",
            MonitoringReportTemplateId = "template-id-123",
            RecipientEmailAddresses = new List<string> { "test@example.com" }
        };
    }

    [Test]
    public void Constructor_WhenConfigurationIsNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new EmailNotificationService(null, _mockLogger.Object));
    }

    [Test]
    public void Constructor_WhenLoggerIsNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new EmailNotificationService(_configuration, null));
    }

    [Test]
    public void Constructor_WhenApiKeyIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        _configuration.ApiKey = null;

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => 
            new EmailNotificationService(_configuration, _mockLogger.Object));
        
        Assert.That(ex.Message, Is.EqualTo("GovUK Notify API Key is not configured"));
    }

    [Test]
    public void Constructor_WhenApiKeyIsEmpty_ThrowsInvalidOperationException()
    {
        // Arrange
        _configuration.ApiKey = string.Empty;

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => 
            new EmailNotificationService(_configuration, _mockLogger.Object));
        
        Assert.That(ex.Message, Is.EqualTo("GovUK Notify API Key is not configured"));
    }

    [Test]
    public void Constructor_WhenApiKeyIsWhitespace_ThrowsInvalidOperationException()
    {
        // Arrange
        _configuration.ApiKey = "   ";

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => 
            new EmailNotificationService(_configuration, _mockLogger.Object));
        
        Assert.That(ex.Message, Is.EqualTo("GovUK Notify API Key is not configured"));
    }

    [Test]
    public void Constructor_WhenValidConfiguration_CreatesInstance()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => 
            new EmailNotificationService(_configuration, _mockLogger.Object));
    }

    [Test]
    public async Task SendMonitoringReportAsync_WhenNoRecipients_LogsWarningAndReturns()
    {
        // Arrange
        _configuration.RecipientEmailAddresses = null;
        var service = new EmailNotificationService(_configuration, _mockLogger.Object);

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
    }

    [Test]
    public async Task SendMonitoringReportAsync_WhenRecipientsListIsEmpty_LogsWarningAndReturns()
    {
        // Arrange
        _configuration.RecipientEmailAddresses = new List<string>();
        var service = new EmailNotificationService(_configuration, _mockLogger.Object);

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
    }
}