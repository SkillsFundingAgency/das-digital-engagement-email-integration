using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DAS.DigitalEngagement.Application.PerformanceImport.Api;
using DAS.DigitalEngagement.Application.PerformanceImport.Persistence;
using DAS.DigitalEngagement.Application.PerformanceImport.Services;
using DAS.DigitalEngagement.CampaignInterest.Data.Service;
using DAS.DigitalEngagement.Models.Infrastructure;
using DAS.DigitalEngagement.Models.PerformanceImport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace DAS.DigitalEngagement.EmailIntegration.UnitTests.Services;

[TestFixture]
public class PerformanceImportServiceTests
{
    private Mock<IPerformanceDataClient> _dataClientMock = null!;
    private Mock<IPerformanceDataWriter> _dataWriterMock = null!;
    private Mock<ISqlBulkInserter> _bulkInserterMock = null!;
    private Mock<ILogger<PerformanceImportService>> _loggerMock = null!;

    [SetUp]
    public void SetUp()
    {
        _dataClientMock = new Mock<IPerformanceDataClient>();
        _dataWriterMock = new Mock<IPerformanceDataWriter>();
        _bulkInserterMock = new Mock<ISqlBulkInserter>();
        _loggerMock = new Mock<ILogger<PerformanceImportService>>();

        _dataWriterMock.Setup(x => x.WriteContactsAsync(It.IsAny<IReadOnlyCollection<ContactApiRecord>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _dataWriterMock.Setup(x => x.WriteSendContactsAsync(It.IsAny<IReadOnlyCollection<SendContactApiRecord>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _dataWriterMock.Setup(x => x.WriteLinksAsync(It.IsAny<IReadOnlyCollection<LinkApiRecord>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _dataWriterMock.Setup(x => x.WriteUserAgentsAsync(It.IsAny<IReadOnlyCollection<UserAgentApiRecord>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _dataWriterMock.Setup(x => x.WriteDisplayedContactsAsync(It.IsAny<IReadOnlyCollection<DisplayedContactApiRecord>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _dataWriterMock.Setup(x => x.WriteClickedContactsAsync(It.IsAny<IReadOnlyCollection<ClickedContactApiRecord>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _dataWriterMock.Setup(x => x.WriteBouncedContactsAsync(It.IsAny<IReadOnlyCollection<BouncedContactApiRecord>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _dataWriterMock.Setup(x => x.WriteUnsubscribedContactsAsync(It.IsAny<IReadOnlyCollection<UnsubscribedContactApiRecord>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _dataWriterMock.Setup(x => x.WriteImportCompletionsAsync(It.IsAny<IReadOnlyCollection<Send>>(), It.IsAny<DateTimeOffset>(), It.IsAny<IReadOnlyDictionary<long, bool>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _bulkInserterMock.Setup(x => x.QueryExistingIdsAsync(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<long>());
    }

    [Test]
    public async Task ImportAsync_WhenEligibleSendsEmpty_DoesNothing()
    {
        var sut = CreateSut();

        await sut.ImportAsync(Array.Empty<SendEligibilityRecord>(), DateTimeOffset.UtcNow);

        _dataClientMock.VerifyNoOtherCalls();
        _dataWriterMock.VerifyNoOtherCalls();
        _bulkInserterMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task ImportAsync_ProcessesEligibleSendsAndWritesCompletionMetadata()
    {
        var sut = CreateSut();
        var eligibleSends = new[]
        {
            new SendEligibilityRecord { Id = 1 },
            new SendEligibilityRecord { Id = 2 }
        };

        _dataClientMock.Setup(x => x.GetSendsByIdsAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable(new[]
            {
                new Send { Id = 1, CampaignId = 10 },
                new Send { Id = 2, CampaignId = 11 }
            }));

        _dataClientMock.Setup(x => x.GetCampaignsAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable(new[]
            {
                new Campaign { Id = 10 },
                new Campaign { Id = 11 }
            }));

        _dataClientMock.Setup(x => x.GetLinksAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable(new[] { new LinkApiRecord { Id = 100, SendId = 1 } }));

        _dataClientMock.Setup(x => x.GetSendContactPagesAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
            .Returns(ToPageEnumerable([new[] { new SendContactApiRecord { Id = 1, ContactId = 100, SendId = 1 } } ]));

        _dataClientMock.Setup(x => x.GetContactsAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable(new[] { new ContactApiRecord { Id = 100 } }));

        _dataClientMock.Setup(x => x.GetUserAgentsAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable(new[] { new UserAgentApiRecord { Id = 1, SendContactId = 1 } }));

        _dataClientMock.Setup(x => x.GetDisplayedContactsAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable(new[] { new DisplayedContactApiRecord { Id = 1, SendContactId = 1 } }));

        _dataClientMock.Setup(x => x.GetClickedContactsAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable(new[] { new ClickedContactApiRecord { Id = 1, SendContactId = 1 } }));

        _dataClientMock.Setup(x => x.GetBouncedContactsAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable(new[] { new BouncedContactApiRecord { Id = 1, SendContactId = 1 } }));

        _dataClientMock.Setup(x => x.GetUnsubscribedContactsAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable(new[] { new UnsubscribedContactApiRecord { Id = 1, SendContactId = 1 } }));

        await sut.ImportAsync(eligibleSends, DateTimeOffset.UtcNow);

        _dataWriterMock.Verify(x => x.WriteImportCompletionsAsync(
            It.Is<IReadOnlyCollection<Send>>(s => s.Count == 2),
            It.IsAny<DateTimeOffset>(),
            It.Is<IReadOnlyDictionary<long, bool>>(d => d.Count == 2 && d[1] && d[2]),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _dataWriterMock.Verify(x => x.WriteSendContactsAsync(It.IsAny<IReadOnlyCollection<SendContactApiRecord>>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _dataWriterMock.Verify(x => x.WriteLinksAsync(It.IsAny<IReadOnlyCollection<LinkApiRecord>>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    private PerformanceImportService CreateSut()
    {
        return new PerformanceImportService(
            _dataClientMock.Object,
            _dataWriterMock.Object,
            _bulkInserterMock.Object,
            Options.Create(new EmailMarketingApi
            {
                ApiBaseUrl = "https://api.test.com",
                ApiKey = "test-key",
                ApiRetryCount = 3,
                ChunkSizeKB = 1024,
                ApiFilterBatchSize = 100,
                SendContactFilterBatchSize = 25,
                MaxConcurrentEventImports = 1,
                SqlWriteBatchSize = 5,
                PageSize = 5000
            }),
            _loggerMock.Object);
    }

    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> values)
    {
        foreach (var value in values)
        {
            await Task.Yield();
            yield return value;
        }
    }

    private static async IAsyncEnumerable<IReadOnlyCollection<SendContactApiRecord>> ToPageEnumerable(IEnumerable<IReadOnlyCollection<SendContactApiRecord>> pages)
    {
        foreach (var page in pages)
        {
            await Task.Yield();
            yield return page;
        }
    }
}