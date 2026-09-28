using DAS.DigitalEngagement.CampaignInterest.Data.Repositories.Interfaces;
using DAS.DigitalEngagement.Application.PerformanceImport.Api;
using DAS.DigitalEngagement.CampaignInterest.Data.Models;
using DAS.DigitalEngagement.Models.PerformanceImport;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DAS.DigitalEngagement.Application.PerformanceImport.Services;

public sealed class SendEligibilityService(
    IPerformanceDataClient performanceDataClient,
    ICampaignImportMetadataRepository campaignImportMetadataRepository,
    ILogger<SendEligibilityService> logger,
    IOptions<EmailMarketingApi> apiConfig) : ISendEligibilityService
{
    private readonly int _importWindowDays = apiConfig.Value.ImportWindowDays;

    public async Task<IReadOnlyList<SendEligibilityRecord>> GetEligibleSendRecordsAsync(
        int? subAccountId = null,
        CancellationToken cancellationToken = default)
    {
        var sends = new List<SendEligibilityRecord>();
        await foreach (var send in performanceDataClient.GetSendsAsync(subAccountId, cancellationToken))
        {
            sends.Add(send);
        }

        logger.LogInformation("Retrieved {SendCount} sends for eligibility evaluation.", sends.Count);

        if (sends.Count == 0)
        {
            logger.LogWarning("No sends returned from e-shot API");
            return [];
        }

        var metadata = await GetAllCampaignImportMetadataAsync(cancellationToken);
        var completedSendIds = metadata
            .Where(item => item.IsImportComplete)
            .Select(item => (long)item.SendId)
            .ToHashSet();

        var cutoff = DateTimeOffset.UtcNow.AddDays(-_importWindowDays);
        var eligibleSends = sends
            .Where(send => send.SendCompletedDate <= cutoff)
            .Where(send => !completedSendIds.Contains(send.Id))
            .ToList();

        logger.LogInformation(
            "Eligibility evaluation completed. Metadata entries: {MetadataCount}, completed sends: {CompletedSendCount}, cutoff: {Cutoff}, eligible sends: {EligibleSendCount}.",
            metadata.Count(),
            completedSendIds.Count,
            cutoff,
            eligibleSends.Count);

        return eligibleSends;
    }

    public async Task<IEnumerable<CampaignImportMetadata>> GetAllCampaignImportMetadataAsync(
        CancellationToken cancellationToken = default)
    {
        var metadata = await campaignImportMetadataRepository.GetAllAsync(cancellationToken);
        logger.LogInformation("Retrieved {Count} campaign import metadata entries", metadata.Count());
        return metadata;
    }

}
