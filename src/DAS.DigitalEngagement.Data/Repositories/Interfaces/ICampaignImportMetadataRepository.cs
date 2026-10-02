using DAS.DigitalEngagement.CampaignInterest.Data.Models;

namespace DAS.DigitalEngagement.CampaignInterest.Data.Repositories.Interfaces;

public interface ICampaignImportMetadataRepository
{
    Task<IEnumerable<CampaignImportMetadata>> GetAllAsync(CancellationToken cancellationToken = default);
}
