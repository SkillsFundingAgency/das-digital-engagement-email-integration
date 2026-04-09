using DAS.DigitalEngagement.CampaignInterest.Data.Models;
using DAS.DigitalEngagement.Models.Campaigns;

namespace DAS.DigitalEngagement.Application.Services.Interfaces;

public interface ICampaignService
{
    /// <summary>
    /// Retrieves all Sends (sent campaigns) for a specific sub-account from e-shot.
    /// </summary>
    /// <param name="subAccountId">The e-shot sub-account identifier (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of Send objects containing campaign performance data</returns>
    Task<IEnumerable<Send>> GetAllSendsAsync(int? subAccountId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get displayed email interactions for a Send
    /// </summary>
    /// <param name="sendId">The Send identifier</param>
    /// <param name="userAgentInfo">Collection of UserAgentInfo objects containing device and client information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of DisplayedContact objects containing display interactions</returns>
    Task<IEnumerable<DisplayedContact>> GetDisplayedContactsForSendAsync(int sendId, IEnumerable<UserAgentInfo> userAgentInfo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get clicked link interactions for a Send
    /// </summary>
    /// <param name="sendId">The Send identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of ClickedLinkContact objects containing link click interactions</returns>
    Task<IEnumerable<ClickedLinkContact>> GetClickedLinkContactsForSendAsync(int sendId, IEnumerable<UserAgentInfo> userAgentInfo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get bounced email interactions for a Send
    /// </summary>
    /// <param name="sendId">The Send identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of BouncedContact objects containing bounce information</returns>
    Task<IEnumerable<BouncedContact>> GetBouncedEmailContactsForSendAsync(int sendId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get unsubscribed email interactions for a Send
    /// </summary>
    /// <param name="sendId">The Send identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of UnsubscribedContact objects containing unsubscribe information</returns>
    Task<IEnumerable<UnsubscribedContact>> GetUnsubscribedContactsForSendAsync(int sendId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get unique user agent information for a Send to determine device types and email clients
    /// </summary>
    /// <param name="sendId">The Send identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of unique UserAgentInfo objects containing device and client information</returns>
    Task<IEnumerable<UserAgentInfo>> GetUserAgentInfoForSendAsync(int sendId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines which Sends are eligible for import by comparing all Sends from the e-shot API
    /// against already-imported metadata, filtering by completion status and a configurable time window.
    /// </summary>
    /// <param name="subAccountId">Optional sub-account filter. If null, returns eligible Sends from all sub-accounts.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of Send objects that are eligible for import</returns>
    Task<IEnumerable<Send>> GetEligibleSendsAsync(int? subAccountId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the details of a specific campaign by its identifier.
    /// </summary>
    /// <param name="campaignId">The campaign identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A Campaigns object containing the campaign details, or null if not found</returns>
    Task<Campaigns?> GetCampaignDetailsAsync(long campaignId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the details of the given campaign.
    /// </summary>
    /// <param name="campaign">The Campaigns object containing the campaign details to be saved</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the campaign details were successfully saved, otherwise false</returns>
    Task<bool> SaveCampaignDetailsAsync(Campaigns campaign, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves the import metadata for the specified campaign.
    /// </summary>
    /// <param name="campaignId">The unique identifier of the campaign for which to retrieve import metadata. Must be a positive integer.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the campaign import metadata if
    /// found; otherwise, null.</returns>
    Task<CampaignImportMetadata?> GetCampaignImportMetadataAsync(long campaignId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new or updates an existing campaign import metadata record asynchronously.
    /// </summary>
    /// <param name="metadata">The campaign import metadata to insert or update. Cannot be null.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task that represents the asynchronous operation. The task result is true if the metadata was inserted or
    /// updated successfully; otherwise, false.</returns>
    Task<bool> UpsertCampaignImportMetadataAsync(CampaignImportMetadata metadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously inserts a collection of bounced email contacts into the data store in bulk.
    /// </summary>
    /// <param name="bouncedEmails">The collection of bounced email contact records to insert. Cannot be null or contain null elements.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task that represents the asynchronous operation. The task result is true if all contacts were inserted
    /// successfully; otherwise, false.</returns>
    Task<bool> BulkInsertBouncedContactsAsync(IEnumerable<BouncedEmails> bouncedEmails, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously inserts a collection of clicked link records in bulk.
    /// </summary>
    /// <param name="clickedLinks">The collection of clicked link entities to insert. Cannot be null or contain null elements.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if all records were
    /// inserted successfully; otherwise, <see langword="false"/>.</returns>
    Task<bool> BulkInsertClickedLinksAsync(IEnumerable<ClickedLinks> clickedLinks, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts a collection of displayed email contacts into the data store asynchronously.
    /// </summary>
    /// <param name="displayedEmails">The collection of displayed email contacts to insert. Cannot be null. Each item represents a contact to be added.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task that represents the asynchronous operation. The task result is true if all contacts were inserted
    /// successfully; otherwise, false.</returns>
    Task<bool> BulkInsertDisplayedContactsAsync(IEnumerable<DisplayedEmails> displayedEmails, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously inserts a collection of unsubscribed contacts in bulk.
    /// </summary>
    /// <param name="unsubscribedContacts">The collection of unsubscribed contacts to insert. Cannot be null or contain null elements.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task that represents the asynchronous operation. The task result is true if the insertion succeeds; otherwise, false.</returns>
    Task<bool> BulkInsertUnsubscribedContactsAsync(IEnumerable<UnsubscribedContacts> unsubscribedContacts, CancellationToken cancellationToken = default);
}