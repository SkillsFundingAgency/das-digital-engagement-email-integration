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
}