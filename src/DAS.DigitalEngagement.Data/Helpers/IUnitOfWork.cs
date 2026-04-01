using DAS.DigitalEngagement.CampaignInterest.Data.Repositories;

namespace DAS.DigitalEngagement.CampaignInterest.Data.Helpers;

public interface IUnitOfWork : IAsyncDisposable
{
    IBouncedEmailsRepository BouncedEmails { get; }
    ICampaignsRepository Campaigns { get; }
    IClickedLinksRepository ClickedLinks { get; }
    IDisplayedEmailsRepository DisplayedEmails { get; }
    IUnsubscribedContactsRepository UnsubscribedContacts { get; }
    ICampaignImportMetadataRepository CampaignImportMetadata { get; }
    Task BeginAsync();
}