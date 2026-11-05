using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Concurrent;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.Models.PerformanceImport;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Extensions.Options;

namespace DAS.DigitalEngagement.Application.PerformanceImport.Api;

public sealed class PerformanceDataClient(
    IExternalApiService externalApiService,
    IOptions<EmailMarketingApi> apiOptions) : IPerformanceDataClient
{
    private readonly int _pageSize = apiOptions.Value.PageSize;

    public IAsyncEnumerable<SendEligibilityRecord> GetSendsAsync(IReadOnlyCollection<int>? subAccountIds = null, CancellationToken cancellationToken = default, ConcurrentDictionary<string, long?>? oDataCounts = null)
    {
        var filters = new List<string> { "IsArchived eq false" };

        if (subAccountIds is { Count: > 0 })
        {
            filters.Add($"SubAccountID in ({string.Join(",", subAccountIds.Distinct())})");
        }

        var endpoint = $"Sends?$select=ID,SendCompletedDate&$orderby=ID&$count=true&$filter={Uri.EscapeDataString(string.Join(" and ", filters))}";

        return ReadPagesAsync<SendEligibilityRecord>(endpoint, cancellationToken, PerformanceImportResourceNames.Sends, oDataCounts);
    }

    public IAsyncEnumerable<Send> GetSendsByIdsAsync(IReadOnlyCollection<long> sendIds, CancellationToken cancellationToken = default, ConcurrentDictionary<string, long?>? oDataCounts = null)
    {
        ArgumentNullException.ThrowIfNull(sendIds);
        if (sendIds.Count == 0)
        {
            return EmptyAsync<Send>();
        }

        var endpoint = $"Sends?$filter=ID in ({string.Join(",", sendIds)})&$orderby=ID&$count=true";
        return ReadPagesAsync<Send>(endpoint, cancellationToken, PerformanceImportResourceNames.Sends, oDataCounts);
    }

    public IAsyncEnumerable<Campaign> GetCampaignsAsync(IReadOnlyCollection<long> campaignIds, CancellationToken cancellationToken = default, ConcurrentDictionary<string, long?>? oDataCounts = null)
    {
        ArgumentNullException.ThrowIfNull(campaignIds);
        if (campaignIds.Count == 0)
        {
            return EmptyAsync<Campaign>();
        }

        var endpoint = $"Campaigns?$filter=ID in ({string.Join(",", campaignIds.Distinct())})&$orderby=ID&$count=true";
        return ReadPagesAsync<Campaign>(endpoint, cancellationToken, PerformanceImportResourceNames.Campaigns, oDataCounts);
    }

    /// <summary>
    /// Returns SendContact records one API page at a time so callers can process each page independently.
    /// </summary>
    public IAsyncEnumerable<IReadOnlyCollection<SendContactApiRecord>> GetSendContactPagesAsync(
        IReadOnlyCollection<long> sendIds,
        CancellationToken cancellationToken = default,
        ConcurrentDictionary<string, long?>? oDataCounts = null)
    {
        ArgumentNullException.ThrowIfNull(sendIds);
        if (sendIds.Count == 0)
        {
            return EmptyPagesAsync<SendContactApiRecord>();
        }

        return ReadPageCollectionsAsync<SendContactApiRecord>(BuildInEndpoint(PerformanceImportResourceNames.SendContacts, "SendID", sendIds), cancellationToken, PerformanceImportResourceNames.SendContacts, oDataCounts);
    }

    public IAsyncEnumerable<ContactApiRecord> GetContactsAsync(IReadOnlyCollection<long> contactIds, CancellationToken cancellationToken = default, ConcurrentDictionary<string, long?>? oDataCounts = null)
    {
        ArgumentNullException.ThrowIfNull(contactIds);
        if (contactIds.Count == 0) return EmptyAsync<ContactApiRecord>();
        return ReadPagesAsync<ContactApiRecord>($"Contacts?$filter=ID in ({string.Join(",", contactIds)})&$orderby=ID", cancellationToken, PerformanceImportResourceNames.Contacts, oDataCounts);
    }

    public IAsyncEnumerable<LinkApiRecord> GetLinksAsync(IReadOnlyCollection<long> sendIds, CancellationToken cancellationToken = default, ConcurrentDictionary<string, long?>? oDataCounts = null)
    {
        ArgumentNullException.ThrowIfNull(sendIds);
        if (sendIds.Count == 0)
        {
            return EmptyAsync<LinkApiRecord>();
        }

        return ReadPagesAsync<LinkApiRecord>(BuildInEndpoint(PerformanceImportResourceNames.Links, "SendID", sendIds), cancellationToken, PerformanceImportResourceNames.Links, oDataCounts);
    }

    public IAsyncEnumerable<DisplayedContactApiRecord> GetDisplayedContactsAsync(IReadOnlyCollection<long> sendIds, CancellationToken cancellationToken = default, ConcurrentDictionary<string, long?>? oDataCounts = null)
    {
        ArgumentNullException.ThrowIfNull(sendIds);
        if (sendIds.Count == 0)
        {
            return EmptyAsync<DisplayedContactApiRecord>();
        }

        return ReadPagesAsync<DisplayedContactApiRecord>(BuildInEndpoint(PerformanceImportResourceNames.DisplayedContacts, "SendID", sendIds), cancellationToken, PerformanceImportResourceNames.DisplayedContacts, oDataCounts);
    }

    public IAsyncEnumerable<ClickedContactApiRecord> GetClickedContactsAsync(IReadOnlyCollection<long> sendIds, CancellationToken cancellationToken = default, ConcurrentDictionary<string, long?>? oDataCounts = null)
    {
        ArgumentNullException.ThrowIfNull(sendIds);
        if (sendIds.Count == 0)
        {
            return EmptyAsync<ClickedContactApiRecord>();
        }

        return ReadPagesAsync<ClickedContactApiRecord>(BuildInEndpoint(PerformanceImportResourceNames.ClickedContacts, "SendID", sendIds), cancellationToken, PerformanceImportResourceNames.ClickedContacts, oDataCounts);
    }

    public IAsyncEnumerable<BouncedContactApiRecord> GetBouncedContactsAsync(IReadOnlyCollection<long> sendIds, CancellationToken cancellationToken = default, ConcurrentDictionary<string, long?>? oDataCounts = null)
    {
        ArgumentNullException.ThrowIfNull(sendIds);
        if (sendIds.Count == 0)
        {
            return EmptyAsync<BouncedContactApiRecord>();
        }

        return ReadPagesAsync<BouncedContactApiRecord>(BuildInEndpoint(PerformanceImportResourceNames.BouncedContacts, "SendID", sendIds), cancellationToken, PerformanceImportResourceNames.BouncedContacts, oDataCounts);
    }

    public IAsyncEnumerable<UnsubscribedContactApiRecord> GetUnsubscribedContactsAsync(IReadOnlyCollection<long> sendIds, CancellationToken cancellationToken = default, ConcurrentDictionary<string, long?>? oDataCounts = null)
    {
        ArgumentNullException.ThrowIfNull(sendIds);
        if (sendIds.Count == 0)
        {
            return EmptyAsync<UnsubscribedContactApiRecord>();
        }

        return ReadPagesAsync<UnsubscribedContactApiRecord>(BuildInEndpoint(PerformanceImportResourceNames.UnsubscribedContacts, "SendID", sendIds), cancellationToken, PerformanceImportResourceNames.UnsubscribedContacts, oDataCounts);
    }

    public IAsyncEnumerable<UserAgentApiRecord> GetUserAgentsAsync(IReadOnlyCollection<long> sendIds, CancellationToken cancellationToken = default, ConcurrentDictionary<string, long?>? oDataCounts = null)
    {
        ArgumentNullException.ThrowIfNull(sendIds);
        if (sendIds.Count == 0)
        {
            return EmptyAsync<UserAgentApiRecord>();
        }

        return ReadPagesAsync<UserAgentApiRecord>(BuildInEndpoint(PerformanceImportResourceNames.UserAgents, "SendID", sendIds), cancellationToken, PerformanceImportResourceNames.UserAgents, oDataCounts);
    }

    private async IAsyncEnumerable<T> ReadPagesAsync<T>(
        string initialEndpoint,
        [EnumeratorCancellation] CancellationToken cancellationToken,
        string resourceName,
        ConcurrentDictionary<string, long?>? oDataCounts)
    {
        await foreach (var page in ReadPageCollectionsAsync<T>(initialEndpoint, cancellationToken, resourceName, oDataCounts)
            .WithCancellation(cancellationToken))
        {
            foreach (var item in page)
            {
                yield return item;
            }
        }
    }

    private async IAsyncEnumerable<IReadOnlyCollection<T>> ReadPageCollectionsAsync<T>(
        string initialEndpoint,
        [EnumeratorCancellation] CancellationToken cancellationToken,
        string resourceName,
        ConcurrentDictionary<string, long?>? oDataCounts)
    {
        if (_pageSize <= 0)
        {
            throw new InvalidOperationException("EmailMarketingApi:PageSize must be greater than zero.");
        }

        var nextEndpoint = AddPaging(initialEndpoint, 0);
        var skip = 0;
        var isFirstPageInRequest = true;
        var serverPaging = false;
        var seenEndpoints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (!string.IsNullOrWhiteSpace(nextEndpoint))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!seenEndpoints.Add(nextEndpoint))
            {
                throw new InvalidOperationException($"The e-shot API repeated pagination link '{nextEndpoint}'.");
            }

            var json = await externalApiService.GetDataAsync(nextEndpoint, cancellationToken);
            var page = JsonSerializer.Deserialize<ODataResponse<T>>(json, SerializerOptions)
                ?? throw new JsonException("The e-shot API returned an invalid response.");

            if (isFirstPageInRequest)
            {
                AddODataCount(oDataCounts, resourceName, page.Count);

                isFirstPageInRequest = false;
            }

            if (page.Value is null)
            {
                throw new JsonException("The e-shot API response did not contain a value array.");
            }

            yield return page.Value;

            // If the server has started providing NextLink pagination, continue to follow NextLink
            // and never fall back to manual $skip pagination. If NextLink is not provided and
            // server pagination was previously in use, stop paging to avoid requesting stale pages.
            if (!string.IsNullOrWhiteSpace(page.NextLink))
            {
                serverPaging = true;
                nextEndpoint = NormalizeNextLink(page.NextLink);
                continue;
            }

            if (serverPaging)
            {
                // Server pagination ended; stop rather than switching back to $skip which would be stale.
                yield break;
            }

            if (page.Value.Count < _pageSize)
            {
                yield break;
            }

            skip += _pageSize;
            nextEndpoint = AddPaging(initialEndpoint, skip);
        }
    }

    private static async IAsyncEnumerable<T> EmptyAsync<T>()
    {
        await Task.CompletedTask;
        yield break;
    }

    private static async IAsyncEnumerable<IReadOnlyCollection<T>> EmptyPagesAsync<T>()
    {
        await Task.CompletedTask;
        yield break;
    }

    private static string BuildInEndpoint(string resource, string field, IReadOnlyCollection<long> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        if (ids.Count == 0)
        {
            return $"{resource}?$filter=false";
        }

        return $"{resource}?$filter={Uri.EscapeDataString($"{field} in ({string.Join(",", ids.Distinct())})")}&$orderby=ID&$count=true";
    }

    private string AddPaging(string endpoint, int skip) => $"{endpoint}&$top={_pageSize}&$skip={skip}";

    private static string NormalizeNextLink(string nextLink)
    {
        if (!Uri.TryCreate(nextLink, UriKind.Absolute, out var absolute))
        {
            return nextLink.TrimStart('/');
        }

        return absolute.PathAndQuery.TrimStart('/');
    }

    private static void AddODataCount(ConcurrentDictionary<string, long?>? oDataCounts, string resourceName, long? count)
    {
        if (oDataCounts is null || !count.HasValue)
        {
            return;
        }

        oDataCounts.AddOrUpdate(
            resourceName,
            count.Value,
            (key, existingCount) => (existingCount ?? 0) + count.Value);
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };
}
