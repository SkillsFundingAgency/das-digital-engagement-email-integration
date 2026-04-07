using Azure.Core;
using DAS.DigitalEngagement.CampaignInterest.Data.Repositories;
using DAS.DigitalEngagement.CampaignInterest.Data.Service;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DAS.DigitalEngagement.CampaignInterest.Data.Helpers;

public class UnitOfWork(TokenCredential tokenCredential, IOptions<ConnectionString> connectionStrings, ILoggerFactory loggerFactory) : IUnitOfWork
{
    private readonly ILogger<UnitOfWork> _logger = loggerFactory.CreateLogger<UnitOfWork>();

    public IBouncedEmailsRepository BouncedEmails { get; private set; } = null!;
    public ICampaignsRepository Campaigns { get; private set; } = null!;
    public IClickedLinksRepository ClickedLinks { get; private set; } = null!;
    public IDisplayedEmailsRepository DisplayedEmails { get; private set; } = null!;
    public IUnsubscribedContactsRepository UnsubscribedContacts { get; private set; } = null!;
    public ICampaignImportMetadataRepository CampaignImportMetadata { get; private set; } = null!;

    public Task BeginAsync()
    {
        _logger.LogInformation("Starting database transaction");

        string connectionString = connectionStrings.Value.CampaignsDatabase ?? "";

        IDbConnectionFactory factory = new SqlConnectionFactory(connectionString, tokenCredential);

        var bulkService = new BulkInsertService(factory, loggerFactory.CreateLogger<BulkInsertService>());

        BouncedEmails = new BouncedEmailsRepository(bulkService);
        Campaigns = new CampaignsRepository(factory, loggerFactory.CreateLogger<CampaignsRepository>());
        ClickedLinks = new ClickedLinksRepository(bulkService);
        DisplayedEmails = new DisplayedEmailsRepository(bulkService);
        UnsubscribedContacts = new UnsubscribedContactsRepository(bulkService);
        CampaignImportMetadata = new CampaignImportMetadataRepository(factory, loggerFactory.CreateLogger<CampaignImportMetadataRepository>());

        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
    }
}