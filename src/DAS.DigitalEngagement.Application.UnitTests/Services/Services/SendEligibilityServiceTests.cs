using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DAS.DigitalEngagement.Application.PerformanceImport.Api;
using DAS.DigitalEngagement.Application.PerformanceImport.Services;
using DAS.DigitalEngagement.CampaignInterest.Data.Repositories.Interfaces;
using DAS.DigitalEngagement.CampaignInterest.Data.Models;
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
    public async Task GetEligibleSendRecordsAsync_FiltersOutRecentAndCompletedSends()
    {
        var service = CreateSut(null);
        var sends = new List<SendEligibilityRecord>
        {
            new() { Id = 1, SendCompletedDate = DateTimeOffset.UtcNow.AddDays(-10) },
            new() { Id = 2, SendCompletedDate = DateTimeOffset.UtcNow.AddDays(-10) },
            new() { Id = 3, SendCompletedDate = DateTimeOffset.UtcNow.AddDays(-1) }
        };

        SetupClientToReturnSends(sends);
        _campaignImportMetadataRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new CampaignImportMetadata { SendId = 2, IsImportComplete = true },
                new CampaignImportMetadata { SendId = 99, IsImportComplete = false }
            });

        var result = await service.GetEligibleSendRecordsAsync();

        Assert.That(result.Select(x => x.Id), Is.EqualTo(new[] { 1L }));
    }

    [Test]
    public async Task GetEligibleSendRecordsAsync_WhenNoSendsReturnedSkipsMetadataLookup()
    {
        var service = CreateSut(null);
        _performanceDataClientMock
            .Setup(x => x.GetSendsAsync(null, It.IsAny<CancellationToken>()))
            .Returns(EmptySendsAsync());

        var result = await service.GetEligibleSendRecordsAsync();

        Assert.That(result, Is.Empty);
        _campaignImportMetadataRepositoryMock.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetAllCampaignImportMetadataAsync_ReturnsRepositoryData()
    {
        var service = CreateSut(null);
        var metadata = new[]
        {
            new CampaignImportMetadata { SendId = 1, IsImportComplete = true },
            new CampaignImportMetadata { SendId = 2, IsImportComplete = false }
        };

        _campaignImportMetadataRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(metadata);

        var result = await service.GetAllCampaignImportMetadataAsync();

        Assert.That(result, Is.EquivalentTo(metadata));
        _campaignImportMetadataRepositoryMock.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
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
        SetupClientToReturnSends(new[]
        {
            new SendEligibilityRecord
            {
                Id = 1,
                SendCompletedDate = DateTimeOffset.UtcNow.AddDays(-10)
            }
        });
    }

    private void SetupClientToReturnSends(IReadOnlyCollection<SendEligibilityRecord> sends)
    {
        _performanceDataClientMock
            .Setup(x => x.GetSendsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable(sends));
    }

    private static async IAsyncEnumerable<SendEligibilityRecord> ToAsyncEnumerable(IReadOnlyCollection<SendEligibilityRecord> sends)
    {
        await Task.Yield();
        foreach (var send in sends)
        {
            yield return send;
        }
    }

    private static async IAsyncEnumerable<SendEligibilityRecord> EmptySendsAsync()
    {
        await Task.Yield();
        yield break;
    }
}
