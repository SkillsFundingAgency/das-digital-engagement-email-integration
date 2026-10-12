using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DAS.DigitalEngagement.Application.PerformanceImport.Api;
using DAS.DigitalEngagement.Application.PerformanceImport.Services;
using DAS.DigitalEngagement.CampaignInterest.Data.Repositories.Interfaces;
using DAS.DigitalEngagement.Models.Infrastructure;
using DAS.DigitalEngagement.Models.PerformanceImport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace DAS.DigitalEngagement.EmailIntegration.UnitTests.Services;

[TestFixture]
public class SendEligibilityServiceTests
{
    private Mock<IPerformanceDataClient> _performanceDataClientMock = null!;
    private Mock<ICampaignImportMetadataRepository> _campaignImportMetadataRepositoryMock = null!;
    private Mock<ILogger<SendEligibilityService>> _loggerMock = null!;

    [SetUp]
    public void SetUp()
    {
        _performanceDataClientMock = new Mock<IPerformanceDataClient>();
        _campaignImportMetadataRepositoryMock = new Mock<ICampaignImportMetadataRepository>();
        _loggerMock = new Mock<ILogger<SendEligibilityService>>();
    }

    [Test]
    public async Task GetEligibleSendRecordsAsync_WhenConfiguredSubAccountIdsPassesIdsToClient()
    {
        var expectedIds = new int[] { 3, 5 };
        var service = CreateSut(expectedIds);
        SetupClientToReturnOneSend();
        _campaignImportMetadataRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await service.GetEligibleSendRecordsAsync();

        _performanceDataClientMock.Verify(
            x => x.GetSendsAsync(It.Is<IReadOnlyCollection<int>>(ids => ids.SequenceEqual(expectedIds)), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task GetEligibleSendRecordsAsync_WhenSubAccountIdsNullPassesNoFilterToClient()
    {
        var service = CreateSut(null);
        SetupClientToReturnOneSend();
        _campaignImportMetadataRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await service.GetEligibleSendRecordsAsync();

        _performanceDataClientMock.Verify(
            x => x.GetSendsAsync(null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task GetEligibleSendRecordsAsync_WhenSubAccountIdsEmptyPassesNoFilterToClient()
    {
        var service = CreateSut(Array.Empty<int>());
        SetupClientToReturnOneSend();
        _campaignImportMetadataRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await service.GetEligibleSendRecordsAsync();

        _performanceDataClientMock.Verify(
            x => x.GetSendsAsync(null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private SendEligibilityService CreateSut(int[] subAccountIds)
    {
        return new SendEligibilityService(
            _performanceDataClientMock.Object,
            _campaignImportMetadataRepositoryMock.Object,
            _loggerMock.Object,
            Options.Create(new EmailMarketingApi
            {
                ApiBaseUrl = "https://api.test.com",
                ApiKey = "test-key",
                ApiRetryCount = 3,
                ChunkSizeKB = 1024,
                ImportWindowDays = 7,
                SubAccountIds = subAccountIds
            }));
    }

    private void SetupClientToReturnOneSend()
    {
        _performanceDataClientMock
            .Setup(x => x.GetSendsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .Returns(GetSendsAsync());
    }

    private static async IAsyncEnumerable<SendEligibilityRecord> GetSendsAsync()
    {
        await Task.Yield();
        yield return new SendEligibilityRecord
        {
            Id = 1,
            SendCompletedDate = DateTimeOffset.UtcNow.AddDays(-10)
        };
    }
}
