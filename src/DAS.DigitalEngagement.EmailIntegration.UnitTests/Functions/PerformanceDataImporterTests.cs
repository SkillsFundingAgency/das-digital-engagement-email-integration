using DAS.DigitalEngagement.Application.Handlers.CampaignToStaging;
using DAS.DigitalEngagement.Application.PerformanceImport.Handlers;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.EmailIntegration.Functions;
using DAS.DigitalEngagement.Models.Infrastructure;
using DAS.DigitalEngagement.Models.PerformanceImport;
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
    private Mock<IPerformanceImportReportService> _performanceImportReportServiceMock = null!;
    private Mock<IReportService> _reportServiceMock = null!;
    private Mock<ILogger<PerformanceDataImporter>> _loggerMock = null!;
    private ApplicationConfiguration _configuration = null!;
    private PerformanceDataImporter _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _importCampaignStagingHandlerMock = new Mock<IImportCampaignStagingHandler>();
        _performanceImportReportServiceMock = new Mock<IPerformanceImportReportService>();
        _reportServiceMock = new Mock<IReportService>();
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

        _importCampaignStagingHandlerMock
            .Setup(x => x.Handle(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new PerformanceImportReportSummary
            {
                StartedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
                EndedAtUtc = DateTimeOffset.UtcNow,
                Status = PerformanceImportStatus.Completed,
                EligibleSends = 1
            }));
        _performanceImportReportServiceMock
            .Setup(x => x.CreatePerformanceImportReport(It.IsAny<PerformanceImportReportSummary>()))
            .Returns("report-content");
        _reportServiceMock
            .Setup(x => x.SaveReportToBlobInternalAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("https://example.test/report.txt");

        _sut = new PerformanceDataImporter(
            _importCampaignStagingHandlerMock.Object,
            _configuration,
            _performanceImportReportServiceMock.Object,
            _reportServiceMock.Object,
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
        _performanceImportReportServiceMock.Verify(x => x.CreatePerformanceImportReport(It.IsAny<PerformanceImportReportSummary>()), Times.Once);
        _reportServiceMock.Verify(x => x.SaveReportToBlobInternalAsync(
                "report-content",
                It.Is<string>(name => name.StartsWith("Performance-Import/Import-"))),
            Times.Once);
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
        _reportServiceMock.Verify(x => x.SaveReportToBlobInternalAsync(
                It.Is<string>(report => report.Contains("Performance Data Import failed")),
                It.Is<string>(name => name.StartsWith("Performance-Import/Import-"))),
            Times.Once);
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

    [Test]
    public async Task Run_WhenImportFailsWithSummary_SavesStructuredFailureReport()
    {
        var timerInfo = new TimerInfo();
        var summary = new PerformanceImportReportSummary
        {
            Status = PerformanceImportStatus.Partial,
            EligibleSends = 2,
            FailureCount = 1,
            StartedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
            EndedAtUtc = DateTimeOffset.UtcNow,
            Messages = ["Outer batch 1 had 1 failed groups."]
        };

        _importCampaignStagingHandlerMock
            .Setup(x => x.Handle(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PerformanceImportFailedException(
                summary,
                "failure",
                new AggregateException(new InvalidOperationException("test"))));

        _performanceImportReportServiceMock
            .Setup(x => x.CreatePerformanceImportReport(summary))
            .Returns("structured-failure-report");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.Run(timerInfo, CancellationToken.None));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.InnerException, Is.TypeOf<PerformanceImportFailedException>());
        _performanceImportReportServiceMock.Verify(x => x.CreatePerformanceImportReport(summary), Times.Once);
        _reportServiceMock.Verify(x => x.SaveReportToBlobInternalAsync(
                "structured-failure-report",
                It.Is<string>(name => name.StartsWith("Performance-Import/Import-"))),
            Times.Once);
    }
}
