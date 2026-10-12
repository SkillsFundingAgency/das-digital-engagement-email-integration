using System.Threading;
using System.Threading.Tasks;
using DAS.DigitalEngagement.Application.PerformanceImport.Api;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace DAS.DigitalEngagement.EmailIntegration.UnitTests.PerformanceImport.Api;

[TestFixture]
public class PerformanceDataClientTests
{
    [Test]
    public async Task GetSendsAsync_WhenSingleIntegerSubAccountId_UsesInFilter()
    {
        var capturedEndpoint = string.Empty;
        var externalApiServiceMock = new Mock<IExternalApiService>();
        externalApiServiceMock
            .Setup(x => x.GetDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((endpoint, _) => capturedEndpoint = endpoint)
            .ReturnsAsync("{\"value\":[]}");

        var sut = CreateSut(externalApiServiceMock.Object);
        const int subAccountId = 3;

        await foreach (var _ in sut.GetSendsAsync([subAccountId]))
        {
        }

        Assert.That(capturedEndpoint, Does.Contain("SubAccountID%20in%20%283%29"));
        Assert.That(capturedEndpoint, Does.Not.Contain(" in "));
    }

    [Test]
    public async Task GetSendsAsync_WhenMultipleIntegerSubAccountIds_UsesInFilter()
    {
        var capturedEndpoint = string.Empty;
        var externalApiServiceMock = new Mock<IExternalApiService>();
        externalApiServiceMock
            .Setup(x => x.GetDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((endpoint, _) => capturedEndpoint = endpoint)
            .ReturnsAsync("{\"value\":[]}");

        var sut = CreateSut(externalApiServiceMock.Object);
        var ids = new[] { 3, 5 };

        await foreach (var _ in sut.GetSendsAsync(ids))
        {
        }

        Assert.That(capturedEndpoint, Does.Contain("SubAccountID%20in%20%283%2C5%29"));
    }

    private static PerformanceDataClient CreateSut(IExternalApiService externalApiService)
    {
        var options = Options.Create(new EmailMarketingApi
        {
            ApiBaseUrl = "https://api.test.com",
            ApiKey = "test-key",
            ApiRetryCount = 3,
            ChunkSizeKB = 1024,
            PageSize = 5000
        });

        return new PerformanceDataClient(
            externalApiService,
            options,
            Mock.Of<ILogger<PerformanceDataClient>>());
    }
}
