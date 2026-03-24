using DAS.DigitalEngagement.CampaignInterest.Data.Models;
using DAS.DigitalEngagement.CampaignInterest.Data.Service;

namespace DAS.DigitalEngagement.CampaignInterest.Data.Repositories;

public interface IBouncedEmailsRepository
{
    Task BulkInsertAsync(IEnumerable<BouncedEmails> bouncedEmails);
}

public class BouncedEmailsRepository(IBulkInsertService bulkInsertService) : IBouncedEmailsRepository
{
    public async Task BulkInsertAsync(IEnumerable<BouncedEmails> bouncedEmails)
    {
        await bulkInsertService.BulkInsertAsync(bouncedEmails, "dbo.BouncedEmails");
    }
}