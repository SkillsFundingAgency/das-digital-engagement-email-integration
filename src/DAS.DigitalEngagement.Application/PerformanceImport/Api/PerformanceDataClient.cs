using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.Models.PerformanceImport;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace DAS.DigitalEngagement.Application.PerformanceImport.Api;

public sealed class PerformanceDataClient(
    IExternalApiService externalApiService,
    IOptions<EmailMarketingApi> apiOptions,
    ILogger<PerformanceDataClient> logger) : IPerformanceDataClient
{
    private readonly int _pageSize = apiOptions.Value.PageSize;

    public IAsyncEnumerable<SendEligibilityRecord> GetSendsAsync(int? subAccountId = null, CancellationToken cancellationToken = default)
    {
        var endpoint = "Sends?$select=ID,SendCompletedDate";
        if (subAccountId is not null)
        {
            endpoint += $"&$filter={Uri.EscapeDataString($"SubAccountID eq {subAccountId}")}";
        }

        return ReadPagesAsync<SendEligibilityRecord>(endpoint, cancellationToken);
    }

    public IAsyncEnumerable<Send> GetSendsByIdsAsync(IReadOnlyCollection<long> sendIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sendIds);
        if (sendIds.Count == 0)
        {
            return EmptyAsync<Send>();
        }

        var endpoint = $"Sends?$filter=ID in ({string.Join(",", sendIds)})&$orderby=ID";
        return ReadPagesAsync<Send>(endpoint, cancellationToken);
    }

    public IAsyncEnumerable<Campaign> GetCampaignsAsync(IReadOnlyCollection<long> campaignIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(campaignIds);
        if (campaignIds.Count == 0)
        {
            return EmptyAsync<Campaign>();
        }

        var endpoint = $"Campaigns?$filter=ID in ({string.Join(",", campaignIds.Distinct())})&$orderby=ID";
        return ReadPagesAsync<Campaign>(endpoint, cancellationToken);
    }

    /// <summary>
    /// Streams SendContact records across all API pages without materializing the full response.
    /// Use <see cref="GetSendContactPagesAsync"/> when page boundaries are required for ordered processing.
    /// </summary>
    public async IAsyncEnumerable<SendContactApiRecord> GetSendContactsAsync(
        IReadOnlyCollection<long> sendIds,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var page in GetSendContactPagesAsync(sendIds, cancellationToken).WithCancellation(cancellationToken))
        {
            foreach (var record in page)
            {
                yield return record;
            }
        }
    }

    /// <summary>
    /// Returns SendContact records one API page at a time so callers can process each page independently.
    /// </summary>
    public IAsyncEnumerable<IReadOnlyCollection<SendContactApiRecord>> GetSendContactPagesAsync(
        IReadOnlyCollection<long> sendIds,
        CancellationToken cancellationToken = default) =>
        ReadPageCollectionsAsync<SendContactApiRecord>(BuildInEndpoint("SendContacts", "SendID", sendIds), cancellationToken);

    public IAsyncEnumerable<ContactApiRecord> GetContactsAsync(IReadOnlyCollection<long> contactIds, CancellationToken cancellationToken = default)
    {
        if (contactIds.Count == 0) return EmptyAsync<ContactApiRecord>();
        return ReadPagesAsync<ContactApiRecord>($"Contacts?$filter=ID in ({string.Join(",", contactIds)})&$orderby=ID", cancellationToken);
    }

    public IAsyncEnumerable<LinkApiRecord> GetLinksAsync(IReadOnlyCollection<long> sendIds, CancellationToken cancellationToken = default) =>
        ReadPagesAsync<LinkApiRecord>(BuildInEndpoint("Links", "SendID", sendIds), cancellationToken);

    public IAsyncEnumerable<DisplayedContactApiRecord> GetDisplayedContactsAsync(IReadOnlyCollection<long> sendContactIds, CancellationToken cancellationToken = default) =>
        ReadPagesAsync<DisplayedContactApiRecord>(BuildInEndpoint("DisplayedContacts", "SendContactID", sendContactIds), cancellationToken);

    public IAsyncEnumerable<ClickedContactApiRecord> GetClickedContactsAsync(IReadOnlyCollection<long> sendContactIds, CancellationToken cancellationToken = default) =>
        ReadPagesAsync<ClickedContactApiRecord>(BuildInEndpoint("ClickedContacts", "SendContactID", sendContactIds), cancellationToken);

    public IAsyncEnumerable<BouncedContactApiRecord> GetBouncedContactsAsync(IReadOnlyCollection<long> sendContactIds, CancellationToken cancellationToken = default) =>
        ReadPagesAsync<BouncedContactApiRecord>(BuildInEndpoint("BouncedContacts", "SendContactID", sendContactIds), cancellationToken);

    public IAsyncEnumerable<UnsubscribedContactApiRecord> GetUnsubscribedContactsAsync(IReadOnlyCollection<long> sendContactIds, CancellationToken cancellationToken = default) =>
        ReadPagesAsync<UnsubscribedContactApiRecord>(BuildInEndpoint("UnsubscribedContacts", "SendContactID", sendContactIds), cancellationToken);

    public IAsyncEnumerable<UserAgentApiRecord> GetUserAgentsAsync(IReadOnlyCollection<long> sendContactIds, CancellationToken cancellationToken = default) =>
        ReadPagesAsync<UserAgentApiRecord>(BuildInEndpoint("UserAgents", "SendContactID", sendContactIds), cancellationToken);

    private async IAsyncEnumerable<T> ReadPagesAsync<T>(
        string initialEndpoint,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var page in ReadPageCollectionsAsync<T>(initialEndpoint, cancellationToken)
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
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (_pageSize <= 0)
        {
            throw new InvalidOperationException("EmailMarketingApi:PageSize must be greater than zero.");
        }

        var nextEndpoint = AddPaging(initialEndpoint, 0);
        var skip = 0;
        var pageNumber = 0;
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

            if (page.Value is null)
            {
                throw new JsonException("The e-shot API response did not contain a value array.");
            }

            pageNumber++;
            logger.LogDebug(
                "Retrieved API page {PageNumber} containing {RecordCount} {RecordType} records.",
                pageNumber,
                page.Value.Count,
                typeof(T).Name);

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

    private static string BuildInEndpoint(string resource, string field, IReadOnlyCollection<long> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        if (ids.Count == 0)
        {
            return $"{resource}?$filter=false";
        }

        return $"{resource}?$filter={Uri.EscapeDataString($"{field} in ({string.Join(",", ids.Distinct())})")}&$orderby=ID";
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

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };
}
