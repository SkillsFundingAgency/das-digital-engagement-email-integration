using Azure.Core;
using DAS.DigitalEngagement.CampaignInterest.Data.Repositories;
using DAS.DigitalEngagement.CampaignInterest.Data.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DAS.DigitalEngagement.CampaignInterest.Data.Helpers;

public class UnitOfWork(IConfiguration config, TokenCredential tokenCredential, ILogger<UnitOfWork> logger) : IUnitOfWork
{
    public IBouncedEmailsRepository BouncedEmails { get; private set; } = null!;
    public ICampaignsRepository Campaigns { get; private set; } = null!;
    public IClickedLinksRepository ClickedLinks { get; private set; } = null!;
    public IDisplayedEmailsRepository DisplayedEmails { get; private set; } = null!;
    public IUnsubscribedContactsRepository UnsubscribedContacts { get; private set; } = null!;
    public ICampaignImportMetadataRepository CampaignImportMetadata { get; private set; } = null!;

    public async Task BeginAsync()
    {
        logger.LogInformation("Starting database transaction");

        string connectionString = config.GetConnectionString("DefaultConnection")!;

        IDbConnectionFactory factory = new SqlConnectionFactory(connectionString, tokenCredential);

        var bulkService = new BulkInsertService(factory, new LoggerFactory().CreateLogger<BulkInsertService>());

        BouncedEmails = new BouncedEmailsRepository(bulkService);
        Campaigns = new CampaignsRepository(factory, new LoggerFactory().CreateLogger<CampaignsRepository>());
        ClickedLinks = new ClickedLinksRepository(bulkService);
        DisplayedEmails = new DisplayedEmailsRepository(bulkService);
        UnsubscribedContacts = new UnsubscribedContactsRepository(bulkService);
        CampaignImportMetadata = new CampaignImportMetadataRepository(factory, new LoggerFactory().CreateLogger<CampaignImportMetadataRepository>());
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
    }
}