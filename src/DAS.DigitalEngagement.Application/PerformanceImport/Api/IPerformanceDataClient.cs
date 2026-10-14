using DAS.DigitalEngagement.Models.PerformanceImport;

namespace DAS.DigitalEngagement.Application.PerformanceImport.Api;

public interface IPerformanceDataClient
{
    IAsyncEnumerable<SendEligibilityRecord> GetSendsAsync(int? subAccountId = null, CancellationToken cancellationToken = default);
    IAsyncEnumerable<Send> GetSendsByIdsAsync(IReadOnlyCollection<long> sendIds, CancellationToken cancellationToken = default);
    IAsyncEnumerable<Campaign> GetCampaignsAsync(IReadOnlyCollection<long> campaignIds, CancellationToken cancellationToken = default);
    IAsyncEnumerable<SendContactApiRecord> GetSendContactsAsync(IReadOnlyCollection<long> sendIds, CancellationToken cancellationToken = default);
    IAsyncEnumerable<IReadOnlyCollection<SendContactApiRecord>> GetSendContactPagesAsync(IReadOnlyCollection<long> sendIds, CancellationToken cancellationToken = default);
    IAsyncEnumerable<ContactApiRecord> GetContactsAsync(IReadOnlyCollection<long> contactIds, CancellationToken cancellationToken = default);
    IAsyncEnumerable<LinkApiRecord> GetLinksAsync(IReadOnlyCollection<long> sendIds, CancellationToken cancellationToken = default);
    IAsyncEnumerable<DisplayedContactApiRecord> GetDisplayedContactsAsync(IReadOnlyCollection<long> sendContactIds, CancellationToken cancellationToken = default);
    IAsyncEnumerable<ClickedContactApiRecord> GetClickedContactsAsync(IReadOnlyCollection<long> sendContactIds, CancellationToken cancellationToken = default);
    IAsyncEnumerable<BouncedContactApiRecord> GetBouncedContactsAsync(IReadOnlyCollection<long> sendContactIds, CancellationToken cancellationToken = default);
    IAsyncEnumerable<UnsubscribedContactApiRecord> GetUnsubscribedContactsAsync(IReadOnlyCollection<long> sendContactIds, CancellationToken cancellationToken = default);
    IAsyncEnumerable<UserAgentApiRecord> GetUserAgentsAsync(IReadOnlyCollection<long> sendContactIds, CancellationToken cancellationToken = default);
}
