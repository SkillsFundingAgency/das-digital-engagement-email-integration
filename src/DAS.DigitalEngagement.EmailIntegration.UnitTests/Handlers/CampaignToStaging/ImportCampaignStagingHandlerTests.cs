using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DAS.DigitalEngagement.Application.Handlers.CampaignToStaging;
using DAS.DigitalEngagement.Application.PerformanceImport.Services;
using DAS.DigitalEngagement.Models.PerformanceImport;
using Microsoft.Extensions.Logging;
using Moq;

namespace DAS.DigitalEngagement.EmailIntegration.UnitTests.Handlers.CampaignToStaging;

[TestFixture]
public class ImportCampaignStagingHandlerTests
{
    private Mock<ISendEligibilityService> _sendEligibilityServiceMock = null!;
    private Mock<IPerformanceImportService> _performanceImportServiceMock = null!;
    private Mock<ILogger<ImportCampaignStagingHandler>> _loggerMock = null!;

    [SetUp]
    public void SetUp()
    {
        _sendEligibilityServiceMock = new Mock<ISendEligibilityService>();
        _performanceImportServiceMock = new Mock<IPerformanceImportService>();
        _loggerMock = new Mock<ILogger<ImportCampaignStagingHandler>>();
    }

    [Test]
    public async Task Handle_CallsEligibilityServiceAndImportsEligibleSends()
    {
        var eligibleSends = new List<SendEligibilityRecord>
        {
            new() { Id = 1, SendCompletedDate = DateTimeOffset.UtcNow.AddDays(-10) }
        };

        _sendEligibilityServiceMock
            .Setup(x => x.GetEligibleSendRecordsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(eligibleSends);

        var sut = new ImportCampaignStagingHandler(
            _sendEligibilityServiceMock.Object,
            _performanceImportServiceMock.Object,
            _loggerMock.Object);

        await sut.Handle();

        _sendEligibilityServiceMock.Verify(x => x.GetEligibleSendRecordsAsync(It.IsAny<CancellationToken>()), Times.Once);
        _performanceImportServiceMock.Verify(
            x => x.ImportAsync(
                eligibleSends,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
