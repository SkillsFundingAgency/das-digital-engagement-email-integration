using DAS.DigitalEngagement.CampaignInterest.Data.Models;
using DAS.DigitalEngagement.CampaignInterest.Data.Service;

namespace DAS.DigitalEngagement.CampaignInterest.Data.Repositories;

public interface IDisplayedEmailsRepository
{
    Task BulkInsertAsync(IEnumerable<DisplayedEmails> displayedEmails);
}

public class DisplayedEmailsRepository(IBulkInsertService bulkInsertService) : IDisplayedEmailsRepository
{
    public async Task BulkInsertAsync(IEnumerable<DisplayedEmails> displayedEmails)
    {
        await bulkInsertService.BulkInsertAsync(displayedEmails, "dbo.DisplayedEmails");
    }
}