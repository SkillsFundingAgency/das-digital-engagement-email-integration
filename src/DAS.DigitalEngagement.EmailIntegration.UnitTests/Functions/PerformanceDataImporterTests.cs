using DAS.DigitalEngagement.Application.Handlers.CampaignToStaging;
using DAS.DigitalEngagement.EmailIntegration.Functions;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DAS.DigitalEngagement.EmailIntegration.UnitTests.Functions;

[TestFixture]
public class PerformanceDataImporterTests
{
    private Mock<IImportCampaignStagingHandler> _importCampaignStagingHandlerMock = null!;
    private Mock<ILogger<PerformanceDataImporter>> _loggerMock = null!;
    private ApplicationConfiguration _configuration = null!;
    private PerformanceDataImporter _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _importCampaignStagingHandlerMock = new Mock<IImportCampaignStagingHandler>();
        _loggerMock = new Mock<ILogger<PerformanceDataImporter>>();
        _configuration = new ApplicationConfiguration
        {
            ConnectionString = new ConnectionString
            {
                DataMart = "TestConnectionString",
                CampaignsDatabase = "TestCampaignsDatabase"
            },
            DataMart = new List<DataMartSettings>(),
            EmailMarketingApi = new EmailMarketingApi
            {
                ApiBaseUrl = "https://api.test.com"
                , ApiKey = "TestApiKey"
                , ApiRetryCount = 3
                , ChunkSizeKB = 1024
            }
            , GovNotifyConfiguration = new GovNotifyConfiguration
            {
                ApiKey = "TestGovNotifyApiKey",
                MonitoringReportTemplateId = "TestTemplateId",
                RecipientEmailAddresses = new List<string>()
            }
        };

        _sut = new PerformanceDataImporter(
            _importCampaignStagingHandlerMock.Object,
            _configuration,
            _loggerMock.Object);
    }

    [Test]
    public async Task Run_InvokesHandler_AndLogsSuccess_WhenImportCompletes()
    {
        // Arrange
        var timerInfo = new TimerInfo();

        // Act
        await _sut.Run(timerInfo, CancellationToken.None);

        // Assert
        _importCampaignStagingHandlerMock.Verify(x => x.Handle(CancellationToken.None), Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Performance data import started at")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Performance data import using API base URL https://api.test.com")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Performance data import completed successfully.")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Performance data import finished at")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task Run_ThrowsInvalidOperationException_WhenHandlerFails()
    {
        // Arrange
        var timerInfo = new TimerInfo();
        var innerException = new InvalidOperationException("Import failed");
        _importCampaignStagingHandlerMock
            .Setup(x => x.Handle(It.IsAny<CancellationToken>()))
            .ThrowsAsync(innerException);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.Run(timerInfo, CancellationToken.None));

        // Assert
        Assert.That(exception!.Message, Does.StartWith("Performance data import failed after "));
        Assert.That(exception.InnerException, Is.SameAs(innerException));
        _importCampaignStagingHandlerMock.Verify(x => x.Handle(CancellationToken.None), Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Performance data import finished at")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Performance data import using API base URL https://api.test.com")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
