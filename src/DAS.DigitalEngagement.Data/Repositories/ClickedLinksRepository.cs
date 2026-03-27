using DAS.DigitalEngagement.CampaignInterest.Data.Models;
using DAS.DigitalEngagement.CampaignInterest.Data.Service;

namespace DAS.DigitalEngagement.CampaignInterest.Data.Repositories;

public interface IClickedLinksRepository
{
    Task BulkInsertAsync(IEnumerable<ClickedLinks> clickedLinks);
}

public class ClickedLinksRepository(IBulkInsertService bulkInsertService) : IClickedLinksRepository
{
    public async Task BulkInsertAsync(IEnumerable<ClickedLinks> clickedLinks)
    {
        await bulkInsertService.BulkInsertAsync(clickedLinks, "dbo.ClickedLinks");
    }
}