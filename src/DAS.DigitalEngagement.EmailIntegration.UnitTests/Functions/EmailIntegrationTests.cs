using System;
using System.Threading.Tasks;
using DAS.DigitalEngagement.Application.Handlers.Import.Interfaces;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using DAS.DigitalEngagement.Models.Import;

namespace DAS.DigitalEngagement.EmailIntegration.UnitTests.Functions;

[TestFixture]
public class EmailIntegrationTests
{
    private Mock<ILogger<EmailIntegration>> _loggerMock;
    private Mock<IImportDataMartHandler> _importDataMartHandlerMock;
    private ApplicationConfiguration _configuration;
    private EmailIntegration _sut;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<EmailIntegration>>();
        _importDataMartHandlerMock = new Mock<IImportDataMartHandler>();
        _configuration = new ApplicationConfiguration
        {
            ConnectionString = new ConnectionString { DataMart = "TestConnectionString" },
            EShotAPIM = new EShotAPIM
            {
                ApiBaseUrl = "https://api.test.com",
                ApiClientId = "TestClientId",
                ApiRetryCount = 3,
                ChunkSizeKB = 1024
            },
            DataMart = new List<DataMartSettings>
            {
                new DataMartSettings
                {
                    ViewName = "TestView",
                    ObjectName = "TestObject",
                    Config = "TestConfig",
                    FieldMapping = "TestFieldMapping",
                    TemplatedUploadId = 1
                }
            }
        };
        _sut = new EmailIntegration(_loggerMock.Object, _importDataMartHandlerMock.Object, _configuration);
    }

    [Test]
    public async Task RunAsync_LogsInformationAndCallsHandler_WhenNoException()
    {
        // Arrange
        var timerInfo = new TimerInfo();
        var importSummary = new ImportSummaryResult(); 
        _importDataMartHandlerMock
            .Setup(h => h.Handle(_configuration.DataMart))
            .ReturnsAsync(importSummary);

        // Act
        await _sut.RunAsync(timerInfo);

        // Assert
        _importDataMartHandlerMock.Verify(h => h.Handle(_configuration.DataMart), Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Timer trigger function executed at")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                // Updated to match the actual logged value (the type name)
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Connection string: DAS.DigitalEngagement.Models.Infrastructure.ConnectionString")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("API Base URL: https://api.test.com")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Import Summary")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Email Integration Job completed successfully")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public async Task RunAsync_LogsError_WhenExceptionThrown()
    {
        // Arrange
        var timerInfo = new TimerInfo();
        var exception = new Exception("Test exception");
        _importDataMartHandlerMock
            .Setup(h => h.Handle(_configuration.DataMart))
            .ThrowsAsync(exception);

        // Act
        await _sut.RunAsync(timerInfo);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Email Integration Job failed with an exception")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}
