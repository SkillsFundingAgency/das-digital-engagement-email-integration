using DAS.DigitalEngagement.Application.Handlers.Campaigns;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DAS.DigitalEngagement.EmailIntegration.UnitTests.Functions;

public class PerformanceDataImporterTests
{
    private readonly Mock<ILogger<PerformanceDataImporter>> _loggerMock;
    private readonly Mock<IImportCampaignPerformanceHandler> _importCampaignPerformanceHandlerMock;

    public PerformanceDataImporterTests()
    {
        _loggerMock = new Mock<ILogger<PerformanceDataImporter>>();
        _importCampaignPerformanceHandlerMock = new Mock<IImportCampaignPerformanceHandler>();
    }

    [Test]
    public async Task Run_LogsInformation_WhenNoException()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<PerformanceDataImporter>>();
        var importHandlerMock = new Mock<IImportCampaignPerformanceHandler>();
        var sut = new PerformanceDataImporter(importHandlerMock.Object, loggerMock.Object);
        var timerInfo = new TimerInfo();
        // Act
        await sut.Run(timerInfo);
        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Performance Data Importer started at")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Performance data import ran successfully.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public async Task Run_LogsError_WhenHandlerThrowsException()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<PerformanceDataImporter>>();
        var importHandlerMock = new Mock<IImportCampaignPerformanceHandler>();
        importHandlerMock
            .Setup(x => x.Handle(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Handler failure"));
        var sut = new PerformanceDataImporter(importHandlerMock.Object, loggerMock.Object);
        var timerInfo = new TimerInfo();

        // Act
        await sut.Run(timerInfo);

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error importing performance data")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public async Task Run_DoesNotThrow_WhenHandlerThrowsException()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<PerformanceDataImporter>>();
        var importHandlerMock = new Mock<IImportCampaignPerformanceHandler>();
        importHandlerMock
            .Setup(x => x.Handle(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Handler failure"));
        var sut = new PerformanceDataImporter(importHandlerMock.Object, loggerMock.Object);
        var timerInfo = new TimerInfo();

        // Act & Assert - should not throw
        Assert.DoesNotThrowAsync(async () => await sut.Run(timerInfo));
    }

    [Test]
    public async Task Run_DoesNotLogSuccess_WhenHandlerThrowsException()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<PerformanceDataImporter>>();
        var importHandlerMock = new Mock<IImportCampaignPerformanceHandler>();
        importHandlerMock
            .Setup(x => x.Handle(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Handler failure"));
        var sut = new PerformanceDataImporter(importHandlerMock.Object, loggerMock.Object);
        var timerInfo = new TimerInfo();

        // Act
        await sut.Run(timerInfo);

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Performance data import ran successfully")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Never);
    }

    [Test]
    public async Task Run_LogsWarning_WhenTimerIsPastDue()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<PerformanceDataImporter>>();
        var importHandlerMock = new Mock<IImportCampaignPerformanceHandler>();
        var sut = new PerformanceDataImporter(importHandlerMock.Object, loggerMock.Object);
        var timerInfo = new TimerInfo { IsPastDue = true };
        // Act
        await sut.Run(timerInfo);
        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Timer schedule status: overdue")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public async Task Run_CallsHandlerHandle()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<PerformanceDataImporter>>();
        var importHandlerMock = new Mock<IImportCampaignPerformanceHandler>();
        var sut = new PerformanceDataImporter(importHandlerMock.Object, loggerMock.Object);
        var timerInfo = new TimerInfo();

        // Act
        await sut.Run(timerInfo);

        // Assert
        importHandlerMock.Verify(x => x.Handle(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Run_LogsElapsedTime_Always()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<PerformanceDataImporter>>();
        var importHandlerMock = new Mock<IImportCampaignPerformanceHandler>();
        var sut = new PerformanceDataImporter(importHandlerMock.Object, loggerMock.Object);
        var timerInfo = new TimerInfo();

        // Act
        await sut.Run(timerInfo);

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Performance data import finished in")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public async Task Run_LogsElapsedTime_EvenWhenHandlerThrows()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<PerformanceDataImporter>>();
        var importHandlerMock = new Mock<IImportCampaignPerformanceHandler>();
        importHandlerMock
            .Setup(x => x.Handle(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Handler failure"));
        var sut = new PerformanceDataImporter(importHandlerMock.Object, loggerMock.Object);
        var timerInfo = new TimerInfo();

        // Act
        await sut.Run(timerInfo);

        // Assert - finally block should still log elapsed time
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Performance data import finished in")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public async Task Run_DoesNotLogOverdueWarning_WhenTimerIsNotPastDue()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<PerformanceDataImporter>>();
        var importHandlerMock = new Mock<IImportCampaignPerformanceHandler>();
        var sut = new PerformanceDataImporter(importHandlerMock.Object, loggerMock.Object);
        var timerInfo = new TimerInfo { IsPastDue = false };

        // Act
        await sut.Run(timerInfo);

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Timer schedule status: overdue")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Never);
    }

}
