namespace DAS.DigitalEngagement.Models.Campaigns;

public class Send
{
    public required int ID { get; set; }

    public string? Name { get; set; }

    public int CampaignID { get; set; }

    public string? Status { get; set; }

    public string? SubStatus { get; set; }

    public string? SendDate { get; set; }

    public required string SendCompletedDate { get; set; }

    public string? CampaignType { get; set; }

    public int ContactCount { get; set; }

    public string? CreatedBy { get; set; }

    public string? CreatedDate { get; set; }

    public string? FirstSendDate { get; set; }

    public string? LastSendDate { get; set; }

    public string? FromEmail { get; set; }

    public string? FromName { get; set; }

    public string? ReplyEmail { get; set; }

    public string? SubjectLine { get; set; }

    public string? Account { get; set; }
}

public class DisplayedContact
{
    public required int ID { get; set; }

    public string? DisplayDate { get; set; }

    public string? ContactEmail { get; set; }

    public string? Format { get; set; }

    public int SendID { get; set; }

    public int CampaignID { get; set; }

    public int? TimeInSecondsSpentReadingEmail { get; set; }

    public bool IsSuspectedBOT { get; set; }

    public string? Device { get; set; }

    public string? ClientName { get; set; }

    public string? OperatingSystem { get; set; }

    public string? OperatingSystemFamily { get; set; }

    public string? IPAddress { get; set; }

    public string? ClientType { get; set; }

    public string? ClientFamily { get; set; }
}

public class UserAgentInfo
{
    public required int ID { get; set; }
    public int CampaignID { get; set; }
    public int SendID { get; set; }
    public string? IPAddress { get; set; }
    public string? ClientName { get; set; }
    public string? ClientType { get; set; }
    public string? ClientFamily { get; set; }
    public string? Device { get; set; }
    public string? OperatingSystemFamily { get; set; }
    public string? OperatingSystem { get; set; }
}

public class ClickedLinkContact
{
    public required int ID { get; set; }

    public string? ClickedDate { get; set; }

    public string? ContactEmail { get; set; }

    public int SendID { get; set; }

    public int CampaignID { get; set; }

    public string? FriendlyName { get; set; }

    public int LinkID { get; set; }

    public string? URL { get; set; }

    public bool IsMonitored { get; set; }

    public string? ReceivedInMessageFormat { get; set; }

    public bool IsSuspectedBOT { get; set; }

    public string? Device { get; set; }

    public string? ClientName { get; set; }

    public string? OperatingSystem { get; set; }

    public string? OperatingSystemFamily { get; set; }

    public string? IPAddress { get; set; }

    public string? ClientType { get; set; }

    public string? ClientFamily { get; set; }
}

public class BouncedContact
{
    public required int ID { get; set; }

    public string? BounceReason { get; set; }

    public string? BounceType { get; set; }

    public string? BounceDate { get; set; }

    public string? ContactEmail { get; set; }

    public int SendID { get; set; }

    public int CampaignID { get; set; }

    public string? ResponseText { get; set; }
}

public class UnsubscribedContact
{
    public required int ID { get; set; }

    public string? UnsubscribedDate { get; set; }

    public string? ContactEmail { get; set; }

    public int SendID { get; set; }

    public int CampaignID { get; set; }

    public bool IsGlobalUnsubscribe { get; set; }

    public bool IsComplaint { get; set; }
}