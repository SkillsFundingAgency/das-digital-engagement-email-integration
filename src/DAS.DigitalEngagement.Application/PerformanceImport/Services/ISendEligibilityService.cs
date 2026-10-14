using DAS.DigitalEngagement.CampaignInterest.Data.Models;
using DAS.DigitalEngagement.Models.PerformanceImport;

namespace DAS.DigitalEngagement.Application.PerformanceImport.Services;

public interface ISendEligibilityService
{
    Task<IReadOnlyList<SendEligibilityRecord>> GetEligibleSendRecordsAsync(
        int? subAccountId = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<CampaignImportMetadata>> GetAllCampaignImportMetadataAsync(
        CancellationToken cancellationToken = default);

}
