using System.Collections.Concurrent;
using DAS.DigitalEngagement.Models.PerformanceImport;

namespace DAS.DigitalEngagement.Application.PerformanceImport.Api;

public interface IPerformanceDataClient
{
    IAsyncEnumerable<SendEligibilityRecord> GetSendsAsync(IReadOnlyCollection<int>? subAccountIds = null, CancellationToken cancellationToken = default, ConcurrentDictionary<string, long?>? oDataCounts = null);
    IAsyncEnumerable<Send> GetSendsByIdsAsync(IReadOnlyCollection<long> sendIds, CancellationToken cancellationToken = default, ConcurrentDictionary<string, long?>? oDataCounts = null);
    IAsyncEnumerable<Campaign> GetCampaignsAsync(IReadOnlyCollection<long> campaignIds, CancellationToken cancellationToken = default, ConcurrentDictionary<string, long?>? oDataCounts = null);
    IAsyncEnumerable<IReadOnlyCollection<SendContactApiRecord>> GetSendContactPagesAsync(IReadOnlyCollection<long> sendIds, CancellationToken cancellationToken = default, ConcurrentDictionary<string, long?>? oDataCounts = null);
    IAsyncEnumerable<ContactApiRecord> GetContactsAsync(IReadOnlyCollection<long> contactIds, CancellationToken cancellationToken = default, ConcurrentDictionary<string, long?>? oDataCounts = null);
    IAsyncEnumerable<LinkApiRecord> GetLinksAsync(IReadOnlyCollection<long> sendIds, CancellationToken cancellationToken = default, ConcurrentDictionary<string, long?>? oDataCounts = null);
    IAsyncEnumerable<DisplayedContactApiRecord> GetDisplayedContactsAsync(IReadOnlyCollection<long> sendIds, CancellationToken cancellationToken = default, ConcurrentDictionary<string, long?>? oDataCounts = null);
    IAsyncEnumerable<ClickedContactApiRecord> GetClickedContactsAsync(IReadOnlyCollection<long> sendIds, CancellationToken cancellationToken = default, ConcurrentDictionary<string, long?>? oDataCounts = null);
    IAsyncEnumerable<BouncedContactApiRecord> GetBouncedContactsAsync(IReadOnlyCollection<long> sendIds, CancellationToken cancellationToken = default, ConcurrentDictionary<string, long?>? oDataCounts = null);
    IAsyncEnumerable<UnsubscribedContactApiRecord> GetUnsubscribedContactsAsync(IReadOnlyCollection<long> sendIds, CancellationToken cancellationToken = default, ConcurrentDictionary<string, long?>? oDataCounts = null);
    IAsyncEnumerable<UserAgentApiRecord> GetUserAgentsAsync(IReadOnlyCollection<long> sendIds, CancellationToken cancellationToken = default, ConcurrentDictionary<string, long?>? oDataCounts = null);
}
