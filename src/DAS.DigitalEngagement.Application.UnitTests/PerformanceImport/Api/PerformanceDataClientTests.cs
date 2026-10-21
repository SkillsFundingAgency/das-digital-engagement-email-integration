using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DAS.DigitalEngagement.Application.PerformanceImport.Api;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace DAS.DigitalEngagement.Application.UnitTests.PerformanceImport.Api;

[TestFixture]
public class PerformanceDataClientTests
{
    private Mock<IExternalApiService> _externalApiServiceMock = null!;
    private Mock<ILogger<PerformanceDataClient>> _loggerMock = null!;
    private IOptions<EmailMarketingApi> _options = null!;

    [SetUp]
    public void SetUp()
    {
        _externalApiServiceMock = new Mock<IExternalApiService>();
        _loggerMock = new Mock<ILogger<PerformanceDataClient>>();
        _options = Options.Create(new EmailMarketingApi
        {
            ApiBaseUrl = "https://api.test.com",
            ApiKey = "test-key",
            ApiRetryCount = 3,
            ChunkSizeKB = 1024,
            PageSize = 2
        });
    }

    [Test]
    public async Task GetSendsAsync_ReturnsRecords_AndUsesDefaultEndpoint_WhenSubAccountIdsAreNull()
    {
        var client = CreateClient();
        SetupPagedResponse("Sends?$select=ID,SendCompletedDate&$orderby=ID&$top=2&$skip=0", new
        {
            value = new[]
            {
                new { ID = 10L, SendCompletedDate = "2025-01-01T10:00:00Z" },
                new { ID = 11L, SendCompletedDate = "2025-01-02T10:00:00Z" }
            }
        });
        SetupPagedResponse("Sends?$select=ID,SendCompletedDate&$orderby=ID&$top=2&$skip=2", new Dictionary<string, object?> { { "value", Array.Empty<object>() } });

        var result = await EnumerateAsync(client.GetSendsAsync());

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].Id, Is.EqualTo(10));
        Assert.That(result[1].Id, Is.EqualTo(11));
        VerifyRequestedEndpoint("Sends?$select=ID,SendCompletedDate&$orderby=ID&$top=2&$skip=0", Times.Once());
    }

    [Test]
    public async Task GetSendsAsync_DeduplicatesSubAccountIds_AndEncodesFilter()
    {
        var client = CreateClient();
        var endpoint = "Sends?$select=ID,SendCompletedDate&$orderby=ID&$filter=SubAccountID%20in%20%281%2C2%29&$top=2&$skip=0";
        SetupPagedResponse(endpoint, new { value = Array.Empty<object>() });

        var result = await EnumerateAsync(client.GetSendsAsync([1, 2, 2]));

        Assert.That(result, Is.Empty);
        VerifyRequestedEndpoint(endpoint, Times.Once());
    }

    [Test]
    public async Task GetSendContactPagesAsync_ReturnsEmpty_WhenNoIdsProvided()
    {
        var client = CreateClient();

        var pages = await EnumeratePagesAsync(client.GetSendContactPagesAsync([]));

        Assert.That(pages, Is.Empty);
        _externalApiServiceMock.Verify(x => x.GetDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetSendsByIdsAsync_ReturnsEmpty_WhenNoIdsProvided()
    {
        var client = CreateClient();

        var result = await EnumerateAsync(client.GetSendsByIdsAsync([]));

        Assert.That(result, Is.Empty);
        _externalApiServiceMock.Verify(x => x.GetDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void GetSendsByIdsAsync_Throws_WhenSendIdsIsNull()
    {
        var client = CreateClient();

        Assert.That(() => client.GetSendsByIdsAsync(null!), Throws.ArgumentNullException);
    }

    [Test]
    public async Task GetSendsByIdsAsync_UsesOrderedEndpoint()
    {
        var client = CreateClient();
        var endpoint = "Sends?$filter=ID in (5,9)&$orderby=ID&$top=2&$skip=0";
        SetupPagedResponse(endpoint, new
        {
            value = new[]
            {
                new { ID = 5L, Name = "Send 5" }
            }
        });

        var result = await EnumerateAsync(client.GetSendsByIdsAsync([5, 9]));

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Id, Is.EqualTo(5));
        VerifyRequestedEndpoint(endpoint, Times.Once());
    }

    [Test]
    public async Task GetLinksAsync_ReturnsEmpty_WhenNoIdsProvided()
    {
        var client = CreateClient();

        var result = await EnumerateAsync(client.GetLinksAsync([]));

        Assert.That(result, Is.Empty);
        _externalApiServiceMock.Verify(x => x.GetDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetCampaignsAsync_ReturnsEmpty_WhenNoIdsProvided()
    {
        var client = CreateClient();

        var result = await EnumerateAsync(client.GetCampaignsAsync([]));

        Assert.That(result, Is.Empty);
        _externalApiServiceMock.Verify(x => x.GetDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void GetCampaignsAsync_Throws_WhenCampaignIdsIsNull()
    {
        var client = CreateClient();

        Assert.That(() => client.GetCampaignsAsync(null!), Throws.ArgumentNullException);
    }

    [Test]
    public async Task GetCampaignsAsync_UsesDistinctIdsAndMapsValues()
    {
        var client = CreateClient();
        var endpoint = "Campaigns?$filter=ID in (7,8)&$orderby=ID&$top=2&$skip=0";
        SetupPagedResponse(endpoint, new
        {
            value = new[]
            {
                new { ID = 7L, Name = "Campaign 7", Status = "Active" }
            }
        });

        var result = await EnumerateAsync(client.GetCampaignsAsync([7, 8, 7]));

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Id, Is.EqualTo(7));
        Assert.That(result[0].Name, Is.EqualTo("Campaign 7"));
        Assert.That(result[0].Status, Is.EqualTo("Active"));
        VerifyRequestedEndpoint(endpoint, Times.Once());
    }

    [Test]
    public async Task GetDisplayedContactsAsync_ReturnsEmpty_WhenNoIdsProvided()
    {
        var client = CreateClient();

        var result = await EnumerateAsync(client.GetDisplayedContactsAsync([]));

        Assert.That(result, Is.Empty);
        _externalApiServiceMock.Verify(x => x.GetDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetSendContactPagesAsync_ReturnsPages()
    {
        var client = CreateClient();
        var endpoint = "SendContacts?$filter=SendID%20in%20%281%2C2%29&$orderby=ID&$top=2&$skip=0";
        SetupPagedResponse(endpoint, new
        {
            value = new[]
            {
                new { ID = 1L, SendID = 1L, ContactID = 100L, PublicIP = "127.0.0.1" }
            }
        });

        var pages = await EnumeratePagesAsync(client.GetSendContactPagesAsync([1, 2]));

        Assert.That(pages, Has.Count.EqualTo(1));
        Assert.That(pages[0], Has.Count.EqualTo(1));
        Assert.That(pages[0].First().SendId, Is.EqualTo(1));
        VerifyRequestedEndpoint(endpoint, Times.Once());
    }

    [Test]
    public async Task GetClickedContactsAsync_ReturnsEmpty_WhenNoIdsProvided()
    {
        var client = CreateClient();

        var result = await EnumerateAsync(client.GetClickedContactsAsync([]));

        Assert.That(result, Is.Empty);
        _externalApiServiceMock.Verify(x => x.GetDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetContactsAsync_ReturnsEmpty_WhenNoIdsProvided()
    {
        var client = CreateClient();

        var result = await EnumerateAsync(client.GetContactsAsync([]));

        Assert.That(result, Is.Empty);
        _externalApiServiceMock.Verify(x => x.GetDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetLinksAsync_UsesExpectedEndpoint()
    {
        var client = CreateClient();
        var endpoint = "Links?$filter=SendID%20in%20%2812%2C13%29&$orderby=ID&$top=2&$skip=0";
        SetupPagedResponse(endpoint, new
        {
            value = new[]
            {
                new { ID = 1L, URL = "https://example.com", FriendlyName = "Example", IsMonitored = true, ReceivedInMessageFormat = "html", SendID = 12L }
            }
        });

        var result = await EnumerateAsync(client.GetLinksAsync([12, 13]));

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].SendId, Is.EqualTo(12));
        Assert.That(result[0].Url, Is.EqualTo("https://example.com"));
        VerifyRequestedEndpoint(endpoint, Times.Once());
    }

    [Test]
    public async Task GetBouncedContactsAsync_ReturnsEmpty_WhenNoIdsProvided()
    {
        var client = CreateClient();

        var result = await EnumerateAsync(client.GetBouncedContactsAsync([]));

        Assert.That(result, Is.Empty);
        _externalApiServiceMock.Verify(x => x.GetDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetDisplayedContactsAsync_UsesExpectedEndpoint()
    {
        var client = CreateClient();
        var endpoint = "DisplayedContacts?$filter=SendID%20in%20%2821%29&$orderby=ID&$top=2&$skip=0";
        SetupPagedResponse(endpoint, new { value = new[] { new { ID = 1L, SendContactID = 21L } } });

        var result = await EnumerateAsync(client.GetDisplayedContactsAsync([21]));

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].SendContactId, Is.EqualTo(21));
        VerifyRequestedEndpoint(endpoint, Times.Once());
    }

    [Test]
    public async Task GetUnsubscribedContactsAsync_ReturnsEmpty_WhenNoIdsProvided()
    {
        var client = CreateClient();

        var result = await EnumerateAsync(client.GetUnsubscribedContactsAsync([]));

        Assert.That(result, Is.Empty);
        _externalApiServiceMock.Verify(x => x.GetDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetClickedContactsAsync_UsesExpectedEndpoint()
    {
        var client = CreateClient();
        var endpoint = "ClickedContacts?$filter=SendID%20in%20%2822%29&$orderby=ID&$top=2&$skip=0";
        SetupPagedResponse(endpoint, new { value = new[] { new { ID = 1L, SendContactID = 22L } } });

        var result = await EnumerateAsync(client.GetClickedContactsAsync([22]));

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].SendContactId, Is.EqualTo(22));
        VerifyRequestedEndpoint(endpoint, Times.Once());
    }

    [Test]
    public async Task GetUserAgentsAsync_ReturnsEmpty_WhenNoIdsProvided()
    {
        var client = CreateClient();

        var result = await EnumerateAsync(client.GetUserAgentsAsync([]));

        Assert.That(result, Is.Empty);
        _externalApiServiceMock.Verify(x => x.GetDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetBouncedContactsAsync_UsesExpectedEndpoint()
    {
        var client = CreateClient();
        var endpoint = "BouncedContacts?$filter=SendID%20in%20%2823%29&$orderby=ID&$top=2&$skip=0";
        SetupPagedResponse(endpoint, new { value = new[] { new { ID = 1L, SendContactID = 23L } } });

        var result = await EnumerateAsync(client.GetBouncedContactsAsync([23]));

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].SendContactId, Is.EqualTo(23));
        VerifyRequestedEndpoint(endpoint, Times.Once());
    }

    [Test]
    public async Task GetUnsubscribedContactsAsync_UsesExpectedEndpoint()
    {
        var client = CreateClient();
        var endpoint = "UnsubscribedContacts?$filter=SendID%20in%20%2824%29&$orderby=ID&$top=2&$skip=0";
        SetupPagedResponse(endpoint, new { value = new[] { new { ID = 1L, SendContactID = 24L } } });

        var result = await EnumerateAsync(client.GetUnsubscribedContactsAsync([24]));

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].SendContactId, Is.EqualTo(24));
        VerifyRequestedEndpoint(endpoint, Times.Once());
    }

    [Test]
    public async Task GetUserAgentsAsync_UsesExpectedEndpoint()
    {
        var client = CreateClient();
        var endpoint = "UserAgents?$filter=SendID%20in%20%2825%29&$orderby=ID&$top=2&$skip=0";
        SetupPagedResponse(endpoint, new { value = new[] { new { ID = 1L, SendContactID = 25L } } });

        var result = await EnumerateAsync(client.GetUserAgentsAsync([25]));

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].SendContactId, Is.EqualTo(25));
        VerifyRequestedEndpoint(endpoint, Times.Once());
    }

    private PerformanceDataClient CreateClient() => new(_externalApiServiceMock.Object, _options, _loggerMock.Object);

    private void SetupPagedResponse(string endpoint, object response)
    {
        _externalApiServiceMock
            .Setup(x => x.GetDataAsync(endpoint, It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(response));
    }

    private void VerifyRequestedEndpoint(string endpoint, Times times)
    {
        _externalApiServiceMock.Verify(x => x.GetDataAsync(endpoint, It.IsAny<CancellationToken>()), times);
    }

    private static async Task<List<T>> EnumerateAsync<T>(IAsyncEnumerable<T> source)
    {
        var results = new List<T>();
        await foreach (var item in source)
        {
            results.Add(item);
        }

        return results;
    }

    private static async Task<List<IReadOnlyCollection<T>>> EnumeratePagesAsync<T>(IAsyncEnumerable<IReadOnlyCollection<T>> source)
    {
        var results = new List<IReadOnlyCollection<T>>();
        await foreach (var page in source)
        {
            results.Add(page);
        }

        return results;
    }
}
