using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.Models.Campaigns;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json.Nodes;

namespace DAS.DigitalEngagement.Application.Services;

public class CampaignService : ICampaignService
{
    private readonly IExternalApiService _externalApiService;
    private readonly ILogger<CampaignService> _logger;
    private readonly int _pageSize;

    public CampaignService(
        IExternalApiService externalApiService,
        ILogger<CampaignService> logger,
        IOptions<EmailMarketingApi> apiConfig)
    {
        _externalApiService = externalApiService;
        _logger = logger;
        _pageSize = apiConfig.Value.PageSize;
    }
    public async Task<IEnumerable<Send>> GetAllSendsAsync(int? subAccountId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving Sends for sub-account {SubAccountId}", subAccountId);

        var endpoint = $"Sends?$expand={Uri.EscapeDataString("SubAccount($select=Name),Campaign($select=FirstSendDate,LastSendDate)")}";

        if (subAccountId != null)
        {
            endpoint += $"&$filter={Uri.EscapeDataString($"SubAccountID eq {subAccountId}")}";
        }

        var response = await _externalApiService.GetDataAsync(endpoint);
        var sends = ParseSendsFromResponse(response);

        _logger.LogInformation("Successfully retrieved {SendCount} Sends for sub-account {SubAccountId}", sends.Count(), subAccountId);

        return sends;
    }

    public async Task<IEnumerable<UserAgentInfo>> GetUserAgentInfoForSendAsync(int sendId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving user agent information for Send {SendId}", sendId);

        var userAgentInfos = new List<UserAgentInfo>();
        int skip = 0;
        bool hasMorePages = true;

        while (hasMorePages)
        {
            var endpoint = BuildUserAgentEndpoint(sendId, skip, _pageSize);
            var response = await _externalApiService.GetDataAsync(endpoint);
            var userAgents = ParseUserAgentInfoFromResponse(response);

            userAgentInfos.AddRange(userAgents);

            _logger.LogInformation("Retrieved {UserAgentCount} user agent records for Send {SendId} at skip={Skip}", userAgents.Count(), sendId, skip);

            // Determine if there are more pages
            if (userAgents.Count() < _pageSize)
            {
                hasMorePages = false;
            }
            else
            {
                skip += _pageSize;
            }
        }

        _logger.LogInformation("Successfully retrieved {UserAgentCount} unique user agent information for Send {SendId}", userAgentInfos.Count, sendId);

        return userAgentInfos;
    }

    public async Task<IEnumerable<DisplayedContact>> GetDisplayedContactsForSendAsync(int sendId, IEnumerable<UserAgentInfo> userAgentInfo, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving displayed contacts for Send {SendId} with page size {PageSize}", sendId, _pageSize);

        var displayedContacts = new List<DisplayedContact>();
        int skip = 0;
        bool hasMorePages = true;

        while (hasMorePages)
        {
            var endpoint = BuildDisplayedContactsEndpoint(sendId, skip, _pageSize);
            var response = await _externalApiService.GetDataAsync(endpoint);
            var contacts = ParseDisplayedContactsFromResponse(response, userAgentInfo);

            displayedContacts.AddRange(contacts);

            _logger.LogInformation("Retrieved {ContactCount} displayed contacts for Send {SendId} at skip={Skip}", contacts.Count(), sendId, skip);

            // Determine if there are more pages
            if (contacts.Count() < _pageSize)
            {
                hasMorePages = false;
            }
            else
            {
                skip += _pageSize;
            }
        }

        _logger.LogInformation("Successfully retrieved {ContactCount} total displayed contacts for Send {SendId}", displayedContacts.Count, sendId);

        return displayedContacts;
    }

    public async Task<IEnumerable<ClickedLinkContact>> GetClickedLinkContactsForSendAsync(int sendId, IEnumerable<UserAgentInfo> userAgentInfo, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving clicked link contacts for Send {SendId}", sendId);

        var clickedLinkContacts = new List<ClickedLinkContact>();
        int skip = 0;
        bool hasMorePages = true;

        while (hasMorePages)
        {
            var endpoint = BuildClickedLinkContactsEndpoint(sendId, skip, _pageSize);
            var response = await _externalApiService.GetDataAsync(endpoint);
            var contacts = ParseClickedLinkContactsFromResponse(response, userAgentInfo);

            clickedLinkContacts.AddRange(contacts);

            _logger.LogInformation("Retrieved {ContactCount} clicked link contacts for Send {SendId} at skip={Skip}", contacts.Count(), sendId, skip);

            // Determine if there are more pages
            if (contacts.Count() < _pageSize)
            {
                hasMorePages = false;
            }
            else
            {
                skip += _pageSize;
            }
        }

        _logger.LogInformation("Successfully retrieved {ContactCount} clicked link contacts for Send {SendId}", clickedLinkContacts.Count, sendId);

        return clickedLinkContacts;
    }

    public async Task<IEnumerable<BouncedContact>> GetBouncedEmailContactsForSendAsync(int sendId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving bounced email contacts for Send {SendId}", sendId);

        var bouncedContacts = new List<BouncedContact>();

        // Handle pagination
        int skip = 0;
        bool hasMorePages = true;

        while (hasMorePages)
        {
            var endpoint = BuildBouncedContactsEndpoint(sendId, skip, _pageSize);
            var response = await _externalApiService.GetDataAsync(endpoint);
            var contacts = ParseBouncedContactsFromResponse(response);

            bouncedContacts.AddRange(contacts);

            // Determine if there are more pages
            if (contacts.Count() < _pageSize)
            {
                hasMorePages = false;
            }
            else
            {
                skip += _pageSize;
            }
        }

        _logger.LogInformation("Successfully retrieved {ContactCount} bounced email contacts for Send {SendId}", bouncedContacts.Count, sendId);

        return bouncedContacts;
    }

    public async Task<IEnumerable<UnsubscribedContact>> GetUnsubscribedContactsForSendAsync(int sendId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving unsubscribed email contacts for Send {SendId}", sendId);

        var unsubscribedContacts = new List<UnsubscribedContact>();

        int skip = 0;
        bool hasMorePages = true;

        while (hasMorePages)
        {
            var endpoint = BuildUnsubscribedContactsEndpoint(sendId, skip, _pageSize);

            var response = await _externalApiService.GetDataAsync(endpoint);
            var contacts = ParseUnsubscribedContactsFromResponse(response);

            unsubscribedContacts.AddRange(contacts);

            _logger.LogInformation("Retrieved {ContactCount} clicked link contacts for Send {SendId} at skip={Skip}", contacts.Count(), sendId, skip);

            // Determine if there are more pages
            if (contacts.Count() < _pageSize)
            {
                hasMorePages = false;
            }
            else
            {
                skip += _pageSize;
            }
        }

        _logger.LogInformation("Successfully retrieved {ContactCount} unsubscribed email contacts for Send {SendId}", unsubscribedContacts.Count, sendId);

        return unsubscribedContacts;
    }

    #region Private helper methods
    private static string BuildDisplayedContactsEndpoint(int sendId, int skip, int top)
    {
        var filter = Uri.EscapeDataString($"SendID eq {sendId}");
        var select = Uri.EscapeDataString("ID,DisplayDate");
        var expand = Uri.EscapeDataString("Contact($select=Email)");
        var orderBy = Uri.EscapeDataString("ID");

        return $"DisplayedContacts?$expand={expand}&$filter={filter}&$orderby={orderBy}&$skip={skip}&$top={top}";
    }

    private static string BuildClickedLinkContactsEndpoint(int sendId, int skip, int top)
    {
        var filter = Uri.EscapeDataString($"SendID eq {sendId}");
        var select = Uri.EscapeDataString("ID,ClickDate,LinkURL");
        var expand = Uri.EscapeDataString("Contact($select=Email), Link($select=URL,IsMonitored,ReceivedInMessageFormat)");
        var orderBy = Uri.EscapeDataString("ID");

        return $"ClickedContacts?$expand={expand}&$filter={filter}&$orderby={orderBy}&$skip={skip}&$top={top}";
    }

    private static string BuildBouncedContactsEndpoint(int sendId, int skip, int top)
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
        var select = Uri.EscapeDataString("ID,SendID,IPAddress,clientName,ClientType,ClientFamily,Device,OperatingSystemFamily,OperatingSystem");
        var orderBy = Uri.EscapeDataString("ID");

        return $"UserAgents?$filter={filter}&$orderby={orderBy}&$skip={skip}&$top={top}";
    }

    private IEnumerable<DisplayedContact> ParseDisplayedContactsFromResponse(string jsonResponse, IEnumerable<UserAgentInfo> userAgentInfos)
    {
        var jsonNode = JsonNode.Parse(jsonResponse);
        var valueArray = jsonNode?["value"]?.AsArray();

        if (valueArray == null || valueArray.Count == 0)
        {
            _logger.LogWarning("No displayed contacts found in e-shot response");
            return Enumerable.Empty<DisplayedContact>();
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
                ContactEmail = item?["Contact"]?["Email"]?.GetValue<string>(),
                Format = item?["Format"]?.GetValue<string>(),
                SendID = item?["SendID"]?.GetValue<int>() ?? 0,
                CampaignID = item?["CampaignID"]?.GetValue<int>() ?? 0,
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
                _logger.LogWarning("Skipping invalid DisplayedContact record: ID={ContactId}, DisplayDate={DisplayDate}", contact.ID, contact.DisplayDate);
            }
        }

        return contacts;
    }

    private IEnumerable<ClickedLinkContact> ParseClickedLinkContactsFromResponse(string jsonResponse, IEnumerable<UserAgentInfo> userAgentInfos)
    {
        var jsonNode = JsonNode.Parse(jsonResponse);
        var valueArray = jsonNode?["value"]?.AsArray();

        if (valueArray == null || valueArray.Count == 0)
        {
            _logger.LogWarning("No clicked link contacts found in e-shot response");
            return Enumerable.Empty<ClickedLinkContact>();
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
                SendID = item?["SendID"]?.GetValue<int>() ?? 0,
                CampaignID = item?["CampaignID"]?.GetValue<int>() ?? 0,
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
                _logger.LogWarning("Skipping invalid ClickedLinkContact record: ID={ContactId}, ClickedDate={ClickedDate}", contact.ID, contact.ClickedDate);
            }
        }

        return contacts;
    }

    private IEnumerable<BouncedContact> ParseBouncedContactsFromResponse(string jsonResponse)
    {
        var jsonNode = JsonNode.Parse(jsonResponse);
        var valueArray = jsonNode?["value"]?.AsArray();

        if (valueArray == null || valueArray.Count == 0)
        {
            _logger.LogWarning("No bounced email contacts found in e-shot response");
            return Enumerable.Empty<BouncedContact>();
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
                ContactEmail = item?["Contact"]?["Email"]?.GetValue<string>(),
                SendID = item?["SendID"]?.GetValue<int>() ?? 0,
                CampaignID = item?["CampaignID"]?.GetValue<int>() ?? 0,
                ResponseText = item?["ResponseText"]?.GetValue<string>()
            };

            if (contact.ID > 0 && !string.IsNullOrEmpty(contact.BounceDate))
            {
                contacts.Add(contact);
            }
            else
            {
                _logger.LogWarning("Skipping invalid BouncedContact record: ID={ContactId}, BounceDate={BounceDate}", contact.ID, contact.BounceDate);
            }
        }

        return contacts;
    }

    private IEnumerable<UnsubscribedContact> ParseUnsubscribedContactsFromResponse(string jsonResponse)
    {
        var jsonNode = JsonNode.Parse(jsonResponse);
        var valueArray = jsonNode?["value"]?.AsArray();

        if (valueArray == null || valueArray.Count == 0)
        {
            _logger.LogWarning("No unsubscribed email contacts found in e-shot response");
            return Enumerable.Empty<UnsubscribedContact>();
        }

        var contacts = new List<UnsubscribedContact>();

        foreach (var item in valueArray)
        {
            var contact = new UnsubscribedContact
            {
                ID = item?["ID"]?.GetValue<int>() ?? 0,
                UnsubscribedDate = item?["UnsubscribedDate"]?.GetValue<string>(),
                ContactEmail = item?["Contact"]?["Email"]?.GetValue<string>(),
                SendID = item?["SendID"]?.GetValue<int>() ?? 0,
                CampaignID = item?["CampaignID"]?.GetValue<int>() ?? 0,
                IsGlobalUnsubscribe = item?["IsGlobalUnsubscribe"]?.GetValue<bool>() ?? false,
                IsComplaint = item?["IsComplaint"]?.GetValue<bool>() ?? false
            };

            if (contact.ID > 0 && !string.IsNullOrEmpty(contact.UnsubscribedDate))
            {
                contacts.Add(contact);
            }
            else
            {
                _logger.LogWarning("Skipping invalid UnsubscribedContact record: ID={ContactId}, UnsubscribedDate={UnsubscribedDate}", contact.ID, contact.UnsubscribedDate);
            }
        }

        return contacts;
    }

    private IEnumerable<Send> ParseSendsFromResponse(string jsonResponse)
    {
        var jsonNode = JsonNode.Parse(jsonResponse);
        var valueArray = jsonNode?["value"]?.AsArray();

        if (valueArray == null || valueArray.Count == 0)
        {
            _logger.LogWarning("No Sends found in e-shot response");
            return Enumerable.Empty<Send>();
        }

        var sends = new List<Send>();

        foreach (var item in valueArray)
        {
            var send = new Send
            {
                ID = item?["ID"]?.GetValue<int>() ?? 0,
                Name = item?["Name"]?.GetValue<string>(),
                CampaignID = item?["CampaignID"]?.GetValue<int>() ?? 0,
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
                _logger.LogWarning("Skipping invalid Send record: ID={SendId}, SendCompleteDate={SendCompleteDate}", send.ID, send.SendCompletedDate);
            }
        }

        return sends;
    }

    private IEnumerable<UserAgentInfo> ParseUserAgentInfoFromResponse(string jsonResponse)
    {
        var jsonNode = JsonNode.Parse(jsonResponse);
        var valueArray = jsonNode?["value"]?.AsArray();

        if (valueArray == null || valueArray.Count == 0)
        {
            _logger.LogWarning("No user agent information found in e-shot response");
            return Enumerable.Empty<UserAgentInfo>();
        }

        var userAgentInfos = new List<UserAgentInfo>();
        var seenUserAgents = new HashSet<string>();

        foreach (var item in valueArray)
        {
            var userAgentInfo = new UserAgentInfo
            {
                ID = item?["ID"]?.GetValue<int>() ?? 0,
                CampaignID = item?["CampaignID"]?.GetValue<int>() ?? 0,
                SendID = item?["SendID"]?.GetValue<int>() ?? 0,
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

            if (!seenUserAgents.Contains(key))
            {
                seenUserAgents.Add(key);
                userAgentInfos.Add(userAgentInfo);
            }
        }

        return userAgentInfos;
    }
    #endregion 
}