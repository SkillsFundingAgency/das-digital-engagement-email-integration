using Castle.Core.Logging;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.CampaignInterest.Data.Helpers;
using DAS.DigitalEngagement.CampaignInterest.Data.Models;
using DAS.DigitalEngagement.Models.Campaigns;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text.Json.Nodes;

namespace DAS.DigitalEngagement.Application.Services;

public class CampaignService(IExternalApiService externalApiService, IUnitOfWork unitOfWork, ILogger<CampaignService> logger, IOptions<EmailMarketingApi> apiConfig) : ICampaignService
{
    private readonly int _pageSize = apiConfig.Value.PageSize;
    private readonly int _importWindowDays = apiConfig.Value.ImportWindowDays;
    private const string ContactProperty = "Contact";
    private const string EmailProperty = "Email";

    public async Task<IEnumerable<Send>> GetAllSendsFromEShot(int? subAccountId = null, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Retrieving Sends for sub-account {SubAccountId}", subAccountId);

        var endpoint = $"Sends?$expand={Uri.EscapeDataString("SubAccount($select=Name),Campaign($select=FirstSendDate,LastSendDate,Name)")}";

        if (subAccountId != null)
        {
            endpoint += $"&$filter={Uri.EscapeDataString($"SubAccountID eq {subAccountId}")}";
        }

        var response = await externalApiService.GetDataAsync(endpoint);
        var sends = ParseSendsFromResponse(response);

        logger.LogInformation("Successfully retrieved {SendCount} Sends for sub-account {SubAccountId}", sends.Count, subAccountId);

        return sends;
    }

    public async Task<IEnumerable<UserAgentInfo>> GetUserAgentInfoForSendAsync(int sendId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Retrieving user agent information for Send {SendId}", sendId);

        var userAgentInfos = new List<UserAgentInfo>();
        int skip = 0;
        bool hasMorePages = true;

        while (hasMorePages)
        {
            var endpoint = BuildUserAgentEndpoint(sendId, skip, _pageSize);
            var response = await externalApiService.GetDataAsync(endpoint);
            var userAgents = ParseUserAgentInfoFromResponse(response);

            userAgentInfos.AddRange(userAgents);

            logger.LogInformation("Retrieved {UserAgentCount} user agent records for Send {SendId} at skip={Skip}", userAgents.Count, sendId, skip);

            // Determine if there are more pages
            if (userAgents.Count < _pageSize)
            {
                hasMorePages = false;
            }
            else
            {
                skip += _pageSize;
            }
        }

        logger.LogInformation("Successfully retrieved {UserAgentCount} unique user agent information for Send {SendId}", userAgentInfos.Count, sendId);

        return userAgentInfos;
    }

    public async Task<bool> GetDisplayedContactsFromEShot(int sendId, IEnumerable<UserAgentInfo> userAgentInfo, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Retrieving displayed contacts for Send {SendId} with page size {PageSize}", sendId, _pageSize);

        var displayedContacts = new List<DisplayedContact>();
        int skip = 0;
        bool hasMorePages = true;

        while (hasMorePages)
        {
            try
            {
                var endpoint = BuildDisplayedContactsEndpoint(sendId, skip, _pageSize);
                var response = await externalApiService.GetDataAsync(endpoint);
                var contacts = ParseDisplayedContactsFromResponse(response, userAgentInfo);
                displayedContacts.AddRange(contacts);

                logger.LogInformation("Retrieved {ContactCount} displayed contacts for Send {SendId} at skip={Skip}", contacts.Count, sendId, skip);

                // Bulk insert the contacts for this page
                if (await BulkInsertDisplayedContactsAsync(MapToDisplayedEmails(contacts), cancellationToken))
                {
                    logger.LogInformation("Successfully imported displayed contacts for Send {SendId}", sendId);
                }
                else
                {
                    logger.LogError("Failed to import displayed contacts for Send {SendId}", sendId);
                }

                // Determine if there are more pages
                if (contacts.Count < _pageSize)
                {
                    hasMorePages = false;
                }
                else
                {
                    skip += _pageSize;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving or importing displayed contacts for Send {SendId} at skip={Skip}", sendId, skip);
                return false;
            }
        }

        logger.LogInformation("Successfully retrieved {ContactCount} total displayed contacts for Send {SendId}", displayedContacts.Count, sendId);
        return true;
    }

    public async Task<bool> GetClickedLinkContactsFromEShot(int sendId, IEnumerable<UserAgentInfo> userAgentInfo, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Retrieving clicked link contacts for Send {SendId}", sendId);

        var clickedLinkContacts = new List<ClickedLinkContact>();
        int skip = 0;
        bool hasMorePages = true;

        while (hasMorePages)
        {
            try
            {
                var endpoint = BuildClickedLinkContactsEndpoint(sendId, skip, _pageSize);
                var response = await externalApiService.GetDataAsync(endpoint);
                var contacts = ParseClickedLinkContactsFromResponse(response, userAgentInfo);
                clickedLinkContacts.AddRange(contacts);

                logger.LogInformation("Retrieved {ContactCount} clicked link contacts for Send {SendId} at skip={Skip}", contacts.Count, sendId, skip);

                // Bulk insert the clicked link contacts for this page
                if (await BulkInsertClickedLinksAsync(MapToClickedLinks(contacts), cancellationToken))
                {
                    logger.LogInformation("Successfully imported clicked link contacts for Send {SendId}", sendId);
                }
                else
                {
                    logger.LogError("Failed to import clicked link contacts for Send {SendId}", sendId);
                }

                // Determine if there are more pages
                if (contacts.Count < _pageSize)
                {
                    hasMorePages = false;
                }
                else
                {
                    skip += _pageSize;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving or importing clicked link contacts for Send {SendId} at skip={Skip}", sendId, skip);
                return false;
            }
        }

        logger.LogInformation("Successfully retrieved {ContactCount} clicked link contacts for Send {SendId}", clickedLinkContacts.Count, sendId);
        return true;
    }

    public async Task<bool> GetBouncedEmailContactsFromEShot(int sendId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Retrieving bounced email contacts for Send {SendId}", sendId);

        var bouncedContacts = new List<BouncedContact>();

        // Handle pagination
        int skip = 0;
        bool hasMorePages = true;

        while (hasMorePages)
        {
            try
            {
                var endpoint = BuildBouncedContactsEndpoint(sendId, skip, _pageSize);
                var response = await externalApiService.GetDataAsync(endpoint);
                var contacts = ParseBouncedContactsFromResponse(response);
                bouncedContacts.AddRange(contacts);

                logger.LogInformation("Retrieved {ContactCount} bounced email contacts for Send {SendId} at skip={Skip}", contacts.Count, sendId, skip);

                // Bulk insert the bounced contacts for this page
                if (await BulkInsertBouncedContactsAsync(MapToBouncedContacts(bouncedContacts), cancellationToken))
                {
                    logger.LogInformation("Successfully imported bounced email contacts for Send {SendId}", sendId);
                }
                else
                {
                    logger.LogError("Failed to import bounced email contacts for Send {SendId}", sendId);
                }

                // Determine if there are more pages
                if (contacts.Count < _pageSize)
                {
                    hasMorePages = false;
                }
                else
                {
                    skip += _pageSize;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving or importing bounced email contacts for Send {SendId} at skip={Skip}", sendId, skip);
                return false;
            }
        }

        logger.LogInformation("Successfully retrieved {ContactCount} bounced email contacts for Send {SendId}", bouncedContacts.Count, sendId);
        return true;
    }

    public async Task<bool> GetUnsubscribedContactsFromEShot(int sendId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Retrieving unsubscribed email contacts for Send {SendId}", sendId);

        var unsubscribedContacts = new List<UnsubscribedContact>();

        int skip = 0;
        bool hasMorePages = true;

        while (hasMorePages)
        {
            try
            {
                var endpoint = BuildUnsubscribedContactsEndpoint(sendId, skip, _pageSize);
                var response = await externalApiService.GetDataAsync(endpoint);
                var contacts = ParseUnsubscribedContactsFromResponse(response);
                unsubscribedContacts.AddRange(contacts);

                logger.LogInformation("Retrieved {ContactCount} unsubscribed email contacts for Send {SendId} at skip={Skip}", contacts.Count, sendId, skip);

                // Bulk insert the unsubscribed contacts for this page
                if (await BulkInsertUnsubscribedContactsAsync(MapToUnsubscribedContacts(unsubscribedContacts), cancellationToken))
                {
                    logger.LogInformation("Successfully imported unsubscribed email contacts for Send {SendId}", sendId);
                }
                else
                {
                    logger.LogError("Failed to import unsubscribed email contacts for Send {SendId}", sendId);
                }

                // Determine if there are more pages
                if (contacts.Count < _pageSize)
                {
                    hasMorePages = false;
                }
                else
                {
                    skip += _pageSize;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving or importing unsubscribed email contacts for Send {SendId} at skip={Skip}", sendId, skip);
                return false;
            }
        }

        logger.LogInformation("Successfully retrieved {ContactCount} unsubscribed email contacts for Send {SendId}", unsubscribedContacts.Count, sendId);
        return true;
    }

    public async Task<IEnumerable<Send>> GetEligibleSendsAsync(int? subAccountId = null, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Determining eligible sends for import with window of {ImportWindowDays} days", _importWindowDays);

        if (!subAccountId.HasValue)
        {
            logger.LogInformation("No sub-account filter applied when determining eligible sends");
        }
        else
        {
            logger.LogInformation("Filtering eligible sends for sub-account ID {SubAccountId}", subAccountId);
        }

        var allSends = await GetAllSendsFromEShot(subAccountId, cancellationToken);

        if (!allSends.Any())
        {
            logger.LogWarning("No sends found from e-shot API");
            return [];
        }

        var importedMetadata = await GetAllCampaignImportMetadataAsync(cancellationToken);
        var completedSendIds = new HashSet<long>(importedMetadata.Where(m => m.IsImportComplete).Select(m => m.CampaignId));

        var cutoffDate = DateTime.UtcNow.AddDays(-_importWindowDays);

        var eligibleSends = allSends.Where(send =>
        {
            // If already imported, skip
            if (completedSendIds.Contains(send.ID))
                return false;

            if (!DateTime.TryParse(send.SendCompletedDate, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var sendCompletedDate))
            {
                logger.LogWarning("Unable to parse SendCompletedDate '{SendCompletedDate}' for Send {SendId}, skipping", send.SendCompletedDate, send.ID);
                return false;
            }

            // Only include sends within the configured time window
            return sendCompletedDate <= cutoffDate;

        }).ToList();

        logger.LogInformation("Determined {EligibleCount} eligible sends out of {TotalCount} total sends", eligibleSends.Count, allSends.Count());

        return eligibleSends;
    }

    #region Campaign data access methods

    // Campaigns
    public async Task<Campaigns?> GetCampaignDetailsAsync(long campaignId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Retrieving campaign details for CampaignID {CampaignId} from database", campaignId);

        if (campaignId <= 0)
        {
            logger.LogWarning("Invalid CampaignID {CampaignId} provided for retrieval", campaignId);
            return null;
        }

        try
        {
            await unitOfWork.BeginAsync();
            var campaign = await unitOfWork.Campaigns.GetByIdAsync(campaignId);
            if (campaign == null)
            {
                logger.LogWarning("No campaign details found in database for CampaignID {CampaignId}", campaignId);
                return null;
            }

            logger.LogInformation("Successfully retrieved campaign details for CampaignID {CampaignId}", campaignId);
            return campaign;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving campaign details for CampaignID {CampaignId} from database", campaignId);
            return null;
        }
    }

    public async Task<IEnumerable<Campaigns>> GetAllCampaignsAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Retrieving all campaign import metadata from database");

        try
        {
            await unitOfWork.BeginAsync();
            var campaigns = await unitOfWork.Campaigns.GetAllAsync();
            if (campaigns == null || !campaigns.Any())
            {
                logger.LogWarning("No campaigns found in database");
                return [];
            }

            logger.LogInformation("Successfully retrieved all campaigns from database");
            return campaigns;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving all campaigns from database");
            return [];
        }
    }

    public async Task<long> SaveCampaignDetailsAsync(Campaigns campaign, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Saving campaign details for CampaignID {CampaignId} to database, CancellationToken {CancellationToken}", campaign.Id, cancellationToken);
        long campaignId = 0;

        try
        {
            await unitOfWork.BeginAsync();
            campaignId = await unitOfWork.Campaigns.UpsertAsync(campaign);

            if (campaignId == 0)
            {
                logger.LogWarning("No rows were inserted or updated when saving campaign details for CampaignID {CampaignId}, CancellationToken {CancellationToken}", campaign.Id, cancellationToken);
                return campaignId;
            }

            logger.LogInformation("Successfully saved campaign details for CampaignID {CampaignId} to database, CancellationToken {CancellationToken}", campaign.Id, cancellationToken);
            return campaignId;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving campaign details for CampaignID {CampaignId} to database, CancellationToken {CancellationToken}", campaign.Id, cancellationToken);
            return campaignId;
        }
    }

    // CampaignImportMetadata
    public async Task<CampaignImportMetadata?> GetCampaignImportMetadataAsync(long campaignId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Retrieving campaign import metadata for CampaignID {CampaignId} from database", campaignId);

        if (campaignId <= 0)
        {
            logger.LogWarning("Invalid CampaignID {CampaignId} provided for retrieval", campaignId);
            return null;
        }

        try
        {
            await unitOfWork.BeginAsync();
            var metadata = await unitOfWork.CampaignImportMetadata.GetByIdAsync(campaignId);
            if (metadata == null)
            {
                logger.LogWarning("No campaign import metadata found in database for CampaignID {CampaignId}", campaignId);
                return null;
            }

            logger.LogInformation("Successfully retrieved campaign import metadata for CampaignID {CampaignId}", campaignId);
            return metadata;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving campaign import metadata for CampaignID {CampaignId} from database", campaignId);
            return null;
        }
    }

    public async Task<IEnumerable<CampaignImportMetadata>> GetAllCampaignImportMetadataAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Retrieving all campaign import metadata from database");

        try
        {
            await unitOfWork.BeginAsync();
            var metadata = await unitOfWork.CampaignImportMetadata.GetAllAsync();
            if (metadata == null || !metadata.Any())
            {
                logger.LogWarning("No campaign import metadata found in database");
                return [];
            }

            logger.LogInformation("Successfully retrieved all campaign import metadata from database");
            return metadata;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving all campaign import metadata from database");
            return [];       
        }
    }

    public async Task<bool> UpsertCampaignImportMetadataAsync(CampaignImportMetadata metadata, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Upserting campaign import metadata for CampaignID {CampaignId} to database", metadata.CampaignId);
        try
        {
            await unitOfWork.BeginAsync();
            int rowsAffected = await unitOfWork.CampaignImportMetadata.UpsertAsync(metadata);

            if (rowsAffected == 0)
            {
                logger.LogWarning("No rows were inserted or updated when upserting campaign import metadata for CampaignID {CampaignId}", metadata.CampaignId);
                return false;
            }

            logger.LogInformation("Successfully upserted campaign import metadata for CampaignID {CampaignId} to database", metadata.CampaignId);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error upserting campaign import metadata for CampaignID {CampaignId} to database", metadata.CampaignId);
            return false;
        }
    }

    // BouncedEmails
    public async Task<bool> BulkInsertBouncedContactsAsync(IEnumerable<BouncedEmails> bouncedEmails, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Bulk inserting {ContactCount} bounced contacts into database", bouncedEmails.Count());

        if (!bouncedEmails.Any())
        {
            logger.LogWarning("No bounced contacts to insert into database");
            return true;
        }

        try
        {
            await unitOfWork.BeginAsync();
            await unitOfWork.BouncedEmails.BulkInsertAsync(bouncedEmails);
            logger.LogInformation("Successfully bulk inserted {ContactCount} bounced contacts into database", bouncedEmails.Count());
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error bulk inserting bounced contacts into database");
            return false;
        }
    }

    // ClickedLinks
    public async Task<bool> BulkInsertClickedLinksAsync(IEnumerable<ClickedLinks> clickedLinks, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Bulk inserting {ContactCount} clicked link contacts into database", clickedLinks.Count());

        if (!clickedLinks.Any())
        {
            logger.LogWarning("No clicked link contacts to insert into database");
            return true;
        }

        try
        {
            await unitOfWork.BeginAsync();
            await unitOfWork.ClickedLinks.BulkInsertAsync(clickedLinks);
            logger.LogInformation("Successfully bulk inserted {ContactCount} clicked link contacts into database", clickedLinks.Count());
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error bulk inserting clicked link contacts into database");
            return false;
        }
    }

    // DisplayedEmails
    public async Task<bool> BulkInsertDisplayedContactsAsync(IEnumerable<DisplayedEmails> displayedEmails, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Bulk inserting {ContactCount} displayed email contacts into database", displayedEmails.Count());

        if (!displayedEmails.Any())
        {
            logger.LogWarning("No displayed email contacts to insert into database");
            return true;
        }

        try
        {
            await unitOfWork.BeginAsync();
            await unitOfWork.DisplayedEmails.BulkInsertAsync(displayedEmails);
            logger.LogInformation("Successfully bulk inserted {ContactCount} displayed email contacts into database", displayedEmails.Count());
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error bulk inserting displayed email contacts into database");
            return false;
        }
    }

    // UnsubscribedContacts
    public async Task<bool> BulkInsertUnsubscribedContactsAsync(IEnumerable<UnsubscribedContacts> unsubscribedContacts, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Bulk inserting {ContactCount} unsubscribed contacts into database", unsubscribedContacts.Count());

        if (!unsubscribedContacts.Any())
        {
            logger.LogWarning("No unsubscribed contacts to insert into database");
            return true;
        }

        try
        {
            await unitOfWork.BeginAsync();
            await unitOfWork.UnsubscribedContacts.BulkInsertAsync(unsubscribedContacts);
            logger.LogInformation("Successfully bulk inserted {ContactCount} unsubscribed contacts into database", unsubscribedContacts.Count());
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error bulk inserting unsubscribed contacts into database");
            return false;
        }
    }

    #endregion

    #region Private helper methods

    private static string BuildDisplayedContactsEndpoint(int sendId, int skip, int top)
    {
        var filter = Uri.EscapeDataString($"SendID eq {sendId}");
        var expand = Uri.EscapeDataString("Contact($select=Email)");
        var orderBy = Uri.EscapeDataString("ID");

        return $"DisplayedContacts?$expand={expand}&$filter={filter}&$orderby={orderBy}&$skip={skip}&$top={top}";
    }

    private static string BuildClickedLinkContactsEndpoint(int sendId, int skip, int top)
    {
        var filter = Uri.EscapeDataString($"SendID eq {sendId}");
        var expand = Uri.EscapeDataString("Contact($select=Email), Link($select=URL,IsMonitored,ReceivedInMessageFormat)");
        var orderBy = Uri.EscapeDataString("ID");

        return $"ClickedContacts?$expand={expand}&$filter={filter}&$orderby={orderBy}&$skip={skip}&$top={top}";
    }

    private static string BuildBouncedContactsEndpoint(long sendId, int skip, int top)
    {
        var filter = Uri.EscapeDataString($"SendID eq {sendId}");
        var select = Uri.EscapeDataString("ID,BounceReason,BounceType,BounceDate,SendID,CampaignID,ResponseText");
        var expand = Uri.EscapeDataString("Contact($select=Email)");
        var orderBy = Uri.EscapeDataString("ID");

        return $"BouncedContacts?$select={select}&$expand={expand}&$filter={filter}&$orderby={orderBy}&$skip={skip}&$top={top}";
    }

    private static string BuildUnsubscribedContactsEndpoint(int sendId, int skip, int top)
    {
        var filter = Uri.EscapeDataString($"SendID eq {sendId}");
        var select = Uri.EscapeDataString("ID,UnsubscribeDate,SendID,CampaignID,IsGlobalUnsubscribe,IsComplaint");
        var expand = Uri.EscapeDataString("Contact($select=Email)");
        var orderBy = Uri.EscapeDataString("ID");

        return $"UnsubscribedContacts?$expand={expand}&$select={select}&$filter={filter}&$orderby={orderBy}&$skip={skip}&$top={top}";
    }

    private static string BuildUserAgentEndpoint(int sendId, int skip, int top)
    {
        var filter = Uri.EscapeDataString($"SendID eq {sendId}");
        var orderBy = Uri.EscapeDataString("ID");

        return $"UserAgents?$filter={filter}&$orderby={orderBy}&$skip={skip}&$top={top}";
    }

    private List<DisplayedContact> ParseDisplayedContactsFromResponse(string jsonResponse, IEnumerable<UserAgentInfo> userAgentInfos)
    {
        var valueArray = GetValueArrayFromJsonResponse(jsonResponse);

        if (valueArray == null || valueArray.Count == 0)
        {
            logger.LogWarning("No displayed contacts found in e-shot response");
            return [];
        }

        var contacts = new List<DisplayedContact>();
        var userAgentDict = userAgentInfos.ToDictionary(ua => ua.ID);

        foreach (var item in valueArray)
        {
            var userAgentId = item?["UserAgentID"]?.GetValue<int>() ?? 0;
            var userAgent = userAgentId > 0 && userAgentDict.TryGetValue(userAgentId, out var ua) ? ua : null;

            var contact = new DisplayedContact
            {
                ID = item?["ID"]?.GetValue<int>() ?? 0,
                DisplayDate = item?["DisplayDate"]?.GetValue<string>(),
                ContactEmail = item?[ContactProperty]?[EmailProperty]?.GetValue<string>(),
                Format = item?["Format"]?.GetValue<string>(),
                SendID = item?[nameof(DisplayedContact.SendID)]?.GetValue<int>() ?? 0,
                CampaignID = item?[nameof(DisplayedContact.CampaignID)]?.GetValue<int>() ?? 0,
                TimeInSecondsSpentReadingEmail = item?["TimeInSecondsSpentReadingEmail"]?.GetValue<int>() ?? 0,
                IsSuspectedBOT = item?["IsSuspectedBOT"]?.GetValue<bool>() ?? false,
                Device = userAgent?.Device,
                ClientName = userAgent?.ClientName,
                OperatingSystem = userAgent?.OperatingSystem,
                OperatingSystemFamily = userAgent?.OperatingSystemFamily,
                IPAddress = userAgent?.IPAddress,
                ClientType = userAgent?.ClientType,
                ClientFamily = userAgent?.ClientFamily,
            };

            if (contact.ID > 0 && !string.IsNullOrEmpty(contact.DisplayDate))
            {
                contacts.Add(contact);
            }
            else
            {
                logger.LogWarning("Skipping invalid DisplayedContact record: ID={ContactId}, DisplayDate={DisplayDate}", contact.ID, contact.DisplayDate);
            }
        }

        return contacts;
    }

    private List<ClickedLinkContact> ParseClickedLinkContactsFromResponse(string jsonResponse, IEnumerable<UserAgentInfo> userAgentInfos)
    {
        var valueArray = GetValueArrayFromJsonResponse(jsonResponse);

        if (valueArray == null || valueArray.Count == 0)
        {
            logger.LogWarning("No clicked link contacts found in e-shot response");
            return [];
        }

        var contacts = new List<ClickedLinkContact>();
        var userAgentDict = userAgentInfos.ToDictionary(ua => ua.ID);

        foreach (var item in valueArray)
        {
            var userAgentId = item?["UserAgentID"]?.GetValue<int>() ?? 0;
            var userAgent = userAgentId > 0 && userAgentDict.TryGetValue(userAgentId, out var ua) ? ua : null;

            var contact = new ClickedLinkContact
            {
                ID = item?["ID"]?.GetValue<int>() ?? 0,
                ClickedDate = item?["ClickDate"]?.GetValue<string>(),
                ContactEmail = item?["Contact"]?["Email"]?.GetValue<string>(),
                SendID = item?[nameof(ClickedLinkContact.SendID)]?.GetValue<int>() ?? 0,
                CampaignID = item?[nameof(ClickedLinkContact.CampaignID)]?.GetValue<int>() ?? 0,
                FriendlyName = item?["FriendlyName"]?.GetValue<string>(),
                LinkID = item?["LinkID"]?.GetValue<int>() ?? 0,
                URL = item?["Link"]?["URL"]?.GetValue<string>(),
                IsMonitored = item?["Link"]?["IsMonitored"]?.GetValue<bool>() ?? false,
                ReceivedInMessageFormat = item?["Link"]?["ReceivedInMessageFormat"]?.GetValue<string>(),
                IsSuspectedBOT = item?["IsSuspectedBOT"]?.GetValue<bool>() ?? false,
                Device = userAgent?.Device,
                ClientName = userAgent?.ClientName,
                OperatingSystem = userAgent?.OperatingSystem,
                OperatingSystemFamily = userAgent?.OperatingSystemFamily,
                IPAddress = userAgent?.IPAddress,
                ClientType = userAgent?.ClientType,
                ClientFamily = userAgent?.ClientFamily,
            };

            if (contact.ID > 0 && !string.IsNullOrEmpty(contact.ClickedDate))
            {
                contacts.Add(contact);
            }
            else
            {
                logger.LogWarning("Skipping invalid ClickedLinkContact record: ID={ContactId}, ClickedDate={ClickedDate}", contact.ID, contact.ClickedDate);
            }
        }

        return contacts;
    }

    private List<BouncedContact> ParseBouncedContactsFromResponse(string jsonResponse)
    {
        var valueArray = GetValueArrayFromJsonResponse(jsonResponse);

        if (valueArray == null || valueArray.Count == 0)
        {
            logger.LogWarning("No bounced email contacts found in e-shot response");
            return [];
        }

        var contacts = new List<BouncedContact>();

        foreach (var item in valueArray)
        {
            var contact = new BouncedContact
            {
                ID = item?["ID"]?.GetValue<int>() ?? 0,
                BounceReason = item?["BounceReason"]?.GetValue<string>(),
                BounceType = item?["BounceType"]?.GetValue<string>(),
                BounceDate = item?["BounceDate"]?.GetValue<string>(),
                ContactEmail = item?[ContactProperty]?[EmailProperty]?.GetValue<string>(),
                SendID = item?[nameof(BouncedContact.SendID)]?.GetValue<int>() ?? 0,
                CampaignID = item?[nameof(BouncedContact.CampaignID)]?.GetValue<int>() ?? 0,
                ResponseText = item?["ResponseText"]?.GetValue<string>()
            };

            if (contact.ID > 0 && !string.IsNullOrEmpty(contact.BounceDate))
            {
                contacts.Add(contact);
            }
            else
            {
                logger.LogWarning("Skipping invalid BouncedContact record: ID={ContactId}, BounceDate={BounceDate}", contact.ID, contact.BounceDate);
            }
        }

        return contacts;
    }

    private List<UnsubscribedContact> ParseUnsubscribedContactsFromResponse(string jsonResponse)
    {
        var valueArray = GetValueArrayFromJsonResponse(jsonResponse);

        if (valueArray == null || valueArray.Count == 0)
        {
            logger.LogWarning("No unsubscribed email contacts found in e-shot response");
            return [];
        }

        var contacts = new List<UnsubscribedContact>();

        foreach (var item in valueArray)
        {
            var contact = new UnsubscribedContact
            {
                ID = item?["ID"]?.GetValue<int>() ?? 0,
                UnsubscribedDate = item?["UnsubscribedDate"]?.GetValue<string>(),
                ContactEmail = item?[ContactProperty]?[EmailProperty]?.GetValue<string>(),
                SendID = item?[nameof(UnsubscribedContact.SendID)]?.GetValue<int>() ?? 0,
                CampaignID = item?[nameof(UnsubscribedContact.CampaignID)]?.GetValue<int>() ?? 0,
                IsGlobalUnsubscribe = item?["IsGlobalUnsubscribe"]?.GetValue<bool>() ?? false,
                IsComplaint = item?["IsComplaint"]?.GetValue<bool>() ?? false
            };

            if (contact.ID > 0 && !string.IsNullOrEmpty(contact.UnsubscribedDate))
            {
                contacts.Add(contact);
            }
            else
            {
                logger.LogWarning("Skipping invalid UnsubscribedContact record: ID={ContactId}, UnsubscribedDate={UnsubscribedDate}", contact.ID, contact.UnsubscribedDate);
            }
        }

        return contacts;
    }

    private List<Send> ParseSendsFromResponse(string jsonResponse)
    {
        var valueArray = GetValueArrayFromJsonResponse(jsonResponse);

        if (valueArray == null || valueArray.Count == 0)
        {
            logger.LogWarning("No Sends found in e-shot response");
            return [];
        }

        var sends = new List<Send>();

        foreach (var item in valueArray)
        {
            var send = new Send
            {
                ID = item?["ID"]?.GetValue<int>() ?? 0,
                SendName = item?["Name"]?.GetValue<string>(),
                ExternalCampaignID = item?["CampaignID"]?.GetValue<int>() ?? 0,
                CampaignName = item?["Campaign"]?["Name"]?.GetValue<string>() ?? string.Empty,
                Status = item?["Status"]?.GetValue<string>(),
                SubStatus = item?["SubStatus"]?.GetValue<string>(),
                SendDate = item?["SendDate"]?.GetValue<string>(),
                SendCompletedDate = item?["SendCompletedDate"]?.GetValue<string>() ?? string.Empty,
                CampaignType = item?["CampaignType"]?.GetValue<string>(),
                ContactCount = item?["ContactCount"]?.GetValue<int>() ?? 0,
                CreatedBy = item?["CreatedBy"]?.GetValue<string>(),
                CreatedDate = item?["CreatedDate"]?.GetValue<string>(),
                FirstSendDate = item?["Campaign"]?["FirstSendDate"]?.GetValue<string>(),
                LastSendDate = item?["Campaign"]?["LastSendDate"]?.GetValue<string>(),
                FromEmail = item?["FromEmail"]?.GetValue<string>(),
                FromName = item?["FromName"]?.GetValue<string>(),
                ReplyEmail = item?["ReplyEmail"]?.GetValue<string>(),
                SubjectLine = item?["SubjectLine"]?.GetValue<string>(),
                Account = item?["Subaccount"]?["Name"]?.GetValue<string>()
            };

            if (send.ID > 0 && !string.IsNullOrEmpty(send.SendCompletedDate))
            {
                sends.Add(send);
            }
            else
            {
                logger.LogWarning("Skipping invalid Send record: ID={SendId}, SendCompleteDate={SendCompleteDate}", send.ID, send.SendCompletedDate);
            }
        }

        return sends;
    }

    private List<UserAgentInfo> ParseUserAgentInfoFromResponse(string jsonResponse)
    {
        var valueArray = GetValueArrayFromJsonResponse(jsonResponse);

        if (valueArray == null || valueArray.Count == 0)
        {
            logger.LogWarning("No user agent information found in e-shot response");
            return [];
        }

        var userAgentInfos = new List<UserAgentInfo>();
        var seenUserAgents = new HashSet<string>();

        foreach (var item in valueArray)
        {
            var userAgentInfo = new UserAgentInfo
            {
                ID = item?["ID"]?.GetValue<int>() ?? 0,
                CampaignID = item?[nameof(UserAgentInfo.CampaignID)]?.GetValue<int>() ?? 0,
                SendID = item?[nameof(UserAgentInfo.SendID)]?.GetValue<int>() ?? 0,
                IPAddress = item?["IPAddress"]?.GetValue<string>(),
                ClientName = item?["ClientName"]?.GetValue<string>(),
                ClientType = item?["ClientType"]?.GetValue<string>(),
                ClientFamily = item?["ClientFamily"]?.GetValue<string>(),
                Device = item?["Device"]?.GetValue<string>(),
                OperatingSystemFamily = item?["OperatingSystemFamily"]?.GetValue<string>(),
                OperatingSystem = item?["OperatingSystem"]?.GetValue<string>()
            };

            // Create a unique key for deduplication
            var key = $"{userAgentInfo.ID}|{userAgentInfo.IPAddress}|{userAgentInfo.ClientName}|{userAgentInfo.ClientType}|{userAgentInfo.ClientFamily}|{userAgentInfo.Device}|{userAgentInfo.OperatingSystemFamily}|{userAgentInfo.OperatingSystem}";

            if (seenUserAgents.Add(key))
            {
                userAgentInfos.Add(userAgentInfo);
            }
        }

        return userAgentInfos;
    }
    
    private static JsonArray? GetValueArrayFromJsonResponse(string jsonResponse)
    {
        var jsonNode = JsonNode.Parse(jsonResponse);
        var valueArray = jsonNode?["value"]?.AsArray();
        return valueArray;
    }

    private static IEnumerable<DisplayedEmails> MapToDisplayedEmails(IEnumerable<DisplayedContact> displayedContacts)
    {
        return displayedContacts.Select(contact => new DisplayedEmails
        {
            ExternalId = contact.ID,
            CampaignId = contact.CampaignID,
            ContactEmail = contact.ContactEmail,
            DisplayedDate = contact.DisplayDate != null ? DateTime.Parse(contact.DisplayDate, System.Globalization.CultureInfo.InvariantCulture) : default,
            Format = contact.Format,
            TimeDisplayed = contact.TimeInSecondsSpentReadingEmail ?? 0,
            IsSuspectedBot = contact.IsSuspectedBOT,
            IpAddress = contact.IPAddress,
            Device = contact.Device,
            ClientName = contact.ClientName,
            Os = contact.OperatingSystem,
            OsFamily = contact.OperatingSystemFamily,
            ClientType = contact.ClientType,
            ClientFamily = contact.ClientFamily
        });
    }

    private static IEnumerable<ClickedLinks> MapToClickedLinks(IEnumerable<ClickedLinkContact> clickedLinkContacts)
    {
        return clickedLinkContacts.Select(contact => new ClickedLinks
        {
            ExternalId = contact.ID,
            CampaignId = contact.CampaignID,
            ContactEmail = contact.ContactEmail,
            Url = contact.URL,
            LinkId = contact.LinkID,
            ClickedDate = contact.ClickedDate != null ? DateTime.Parse(contact.ClickedDate, System.Globalization.CultureInfo.InvariantCulture) : default,
            FriendlyUrlName = contact.FriendlyName,
            IsMonitored = contact.IsMonitored,
            EmailFormat = contact.ReceivedInMessageFormat,
            IsSuspectedBot = contact.IsSuspectedBOT,
            IpAddress = contact.IPAddress,
            Device = contact.Device,
            ClientName = contact.ClientName,
            Os = contact.OperatingSystem,
            OsFamily = contact.OperatingSystemFamily,
            ClientType = contact.ClientType,
            ClientFamily = contact.ClientFamily
        });
    }

    private static IEnumerable<BouncedEmails> MapToBouncedContacts(IEnumerable<BouncedContact> bouncedContacts)
    {
        return bouncedContacts.Select(contact => new BouncedEmails
        {
            ExternalId = contact.ID,
            CampaignId = contact.CampaignID,
            ContactEmail = contact.ContactEmail,
            BounceDate = contact.BounceDate != null ? DateTime.Parse(contact.BounceDate, System.Globalization.CultureInfo.InvariantCulture) : default,
            BounceReason = contact.BounceReason,
            BounceType = contact.BounceType,
            ResponseText = contact.ResponseText
        });
    }

    private static IEnumerable<UnsubscribedContacts> MapToUnsubscribedContacts(IEnumerable<UnsubscribedContact> unsubscribedContacts)
    {
        return unsubscribedContacts.Select(contact => new UnsubscribedContacts
        {
            Id = contact.ID,
            ExternalId = contact.SendID,
            CampaignId = contact.CampaignID,
            ContactEmail = contact.ContactEmail,
            UnsubscribedDate = contact.UnsubscribedDate != null ? DateTime.Parse(contact.UnsubscribedDate, System.Globalization.CultureInfo.InvariantCulture) : default,
            IsGlobalUnscribe = contact.IsGlobalUnsubscribe,
            IsComplaint = contact.IsComplaint
        });
    }

    #endregion 
}