using DAS.DigitalEngagement.CampaignInterest.Data.Models;
using DAS.DigitalEngagement.CampaignInterest.Data.Service;

namespace DAS.DigitalEngagement.CampaignInterest.Data.Repositories;

public interface IUnsubscribedContactsRepository
{
    Task BulkInsertAsync(IEnumerable<UnsubscribedContacts> unsubscribedContacts);
}

public class UnsubscribedContactsRepository(IBulkInsertService bulkInsertService) : IUnsubscribedContactsRepository
{
    public async Task BulkInsertAsync(IEnumerable<UnsubscribedContacts> unsubscribedContacts)
    {
        await bulkInsertService.BulkInsertAsync(unsubscribedContacts, "dbo.UnsubscribedContacts");
    }
}