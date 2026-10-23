using System.Data;
using DAS.DigitalEngagement.Models.PerformanceImport;
using DAS.DigitalEngagement.CampaignInterest.Data.Service;

namespace DAS.DigitalEngagement.Application.PerformanceImport.Mappers;

public static class PerformanceDataTableMapper
{
    public static DataTable CreateContacts(IReadOnlyCollection<ContactApiRecord> records)
    {
        var table = Table(
            ImportTableNames.Contacts,
            ("ID", typeof(int)),
            ("SubaccountID", typeof(int)),
            ("SalesforceContactID", typeof(string)),
            ("SalesforceLeadID", typeof(string)),
            ("MicrosoftDynamicsContactID", typeof(string)),
            ("MicrosoftDynamicsLeadID", typeof(string)),
            ("SourceID", typeof(int)),
            ("EmailFormat", typeof(string)),
            ("EmailFormatID", typeof(int)),
            ("Email", typeof(string)),
            ("EmailStatusID", typeof(int)),
            ("MobileStatusID", typeof(int)),
            ("MobileNumber", typeof(string)),
            ("MobileRegion", typeof(int)),
            ("Salutation", typeof(string)),
            ("Firstname", typeof(string)),
            ("Lastname", typeof(string)));

        table.Columns.Add("Telephone", typeof(string));
        table.Columns.Add("URN", typeof(string));
        table.Columns.Add("Company", typeof(string));
        table.Columns.Add("CreatedDate", typeof(DateTime));
        table.Columns.Add("CreatedFromSourceID", typeof(string));
        table.Columns.Add("ModifiedDate", typeof(DateTime));
        table.Columns.Add("ModifiedFromSourceID", typeof(string));
        table.Columns.Add("EmailUnsubscribedDate", typeof(DateTime));
        table.Columns.Add("EmailUnsubscribedFromSourceID", typeof(string));
        table.Columns.Add("SmsUnsubscribedDate", typeof(DateTime));
        table.Columns.Add("SmsUnsubscribedFromSourceID", typeof(string));
        table.Columns.Add("EmailHardBounceDate", typeof(DateTime));
        table.Columns.Add("DoubleOptInConfirmationDate", typeof(DateTime));
        table.Columns.Add("InleadsScore", typeof(int));
        table.Columns.Add("CustomerAccountID", typeof(int));
        table.Columns.Add("EmailEngagementStatus", typeof(int));
        table.Columns.Add("HardBouncedDate", typeof(DateTime));
        table.Columns.Add("LastSoftBouncedDate", typeof(DateTime));
        table.Columns.Add("LastSentDate", typeof(DateTime));
        table.Columns.Add("LastDisplayedDate", typeof(DateTime));
        table.Columns.Add("LastClickedDate", typeof(DateTime));
        table.Columns.Add("FirstInleadsVisitDate", typeof(DateTime));
        table.Columns.Add("LastInleadsVisitDate", typeof(DateTime));
        table.Columns.Add("IncludeInUR", typeof(bool));

        foreach (var record in records)
        {
            Add(
                table,
                ("ID", ToInt32(record.Id)),
                ("SubaccountID", ToInt32(record.SubaccountId)),
                ("SalesforceContactID", record.SalesforceContactId),
                ("SalesforceLeadID", record.SalesforceLeadId),
                ("MicrosoftDynamicsContactID", record.MicrosoftDynamicsContactId),
                ("MicrosoftDynamicsLeadID", record.MicrosoftDynamicsLeadId),
                ("SourceID", ToInt32(record.SourceId)),
                ("EmailFormat", record.EmailFormat),
                ("EmailFormatID", ToInt32(record.EmailFormatId)),
                ("Email", record.Email),
                ("EmailStatusID", ToInt32(record.EmailStatusId)),
                ("MobileStatusID", ToInt32(record.MobileStatusId)),
                ("MobileNumber", record.MobileNumber),
                ("MobileRegion", ToInt32(record.MobileRegion)),
                ("Salutation", record.Salutation),
                ("Firstname", record.FirstName),
                ("Lastname", record.LastName),
                ("Telephone", record.Telephone),
                ("URN", record.Urn),
                ("Company", record.Company),
                ("CreatedDate", ToDateTime(record.CreatedDate)),
                ("CreatedFromSourceID", record.CreatedFromSourceId?.ToString()),
                ("ModifiedDate", ToDateTime(record.ModifiedDate)),
                ("ModifiedFromSourceID", record.ModifiedFromSourceId?.ToString()),
                ("EmailUnsubscribedDate", ToDateTime(record.EmailUnsubscribedDate)),
                ("EmailUnsubscribedFromSourceID", record.EmailUnsubscribedFromSourceId?.ToString()),
                ("SmsUnsubscribedDate", ToDateTime(record.SmsUnsubscribedDate)),
                ("SmsUnsubscribedFromSourceID", record.SmsUnsubscribedFromSourceId?.ToString()),
                ("EmailHardBounceDate", ToDateTime(record.EmailHardBounceDate)),
                ("DoubleOptInConfirmationDate", ToDateTime(record.DoubleOptInConfirmationDate)),
                ("InleadsScore", ToInt32(record.InleadsScore)),
                ("CustomerAccountID", ToInt32(record.CustomerAccountId)),
                ("EmailEngagementStatus", ToInt32(record.EmailEngagementStatus)),
                ("HardBouncedDate", ToDateTime(record.HardBouncedDate)),
                ("LastSoftBouncedDate", ToDateTime(record.LastSoftBouncedDate)),
                ("LastSentDate", ToDateTime(record.LastSentDate)),
                ("LastDisplayedDate", ToDateTime(record.LastDisplayedDate)),
                ("LastClickedDate", ToDateTime(record.LastClickedDate)),
                ("FirstInleadsVisitDate", ToDateTime(record.FirstInleadsVisitDate)),
                ("LastInleadsVisitDate", ToDateTime(record.LastInleadsVisitDate)),
                ("IncludeInUR", record.IncludeInUr));
        }

        return table;
    }

    public static DataTable CreateSendContacts(IReadOnlyCollection<SendContactApiRecord> records)
    {
        var table = Table(
            ImportTableNames.SendContacts,
            ("ID", typeof(int)),
            ("SubaccountID", typeof(int)),
            ("SendID", typeof(int)),
            ("ContactID", typeof(int)),
            ("PublicIP", typeof(string)));

        foreach (var record in records)
        {
            Add(
                table,
                ("ID", ToInt32(record.Id)),
                ("SubaccountID", ToInt32(record.SubaccountId)),
                ("SendID", ToInt32(record.SendId)),
                ("ContactID", ToInt32(record.ContactId)),
                ("PublicIP", record.PublicIp));
        }

        return table;
    }

    public static DataTable CreateImportMetadata(IReadOnlyCollection<Send> sends, DateTimeOffset importStart, IReadOnlyDictionary<long, bool>? importCompleteness)
    {
        var table = Table(
            ImportTableNames.CampaignImportMetadata,
            ("SendID", typeof(int)),
            ("CampaignID", typeof(int)),
            ("IsImportComplete", typeof(bool)),
            ("ImportStartDate", typeof(DateTime)),
            ("ImportEndDate", typeof(DateTime)));

        foreach (var send in sends)
        {
            var isComplete = true;
            if (importCompleteness is not null && importCompleteness.TryGetValue(send.Id, out var complete))
            {
                isComplete = complete;
            }

            Add(
                table,
                ("SendID", ToInt32(send.Id)),
                ("CampaignID", ToInt32(send.CampaignId)),
                ("IsImportComplete", isComplete),
                ("ImportStartDate", importStart.UtcDateTime),
                ("ImportEndDate", DateTime.UtcNow));
        }

        return table;
    }

    public static DataTable CreateUserAgents(IReadOnlyCollection<UserAgentApiRecord> records)
    {
        var table = Table(
            ImportTableNames.UserAgents,
            ("ID", typeof(int)),
            ("SubaccountID", typeof(int)),
            ("SendContactID", typeof(int)),
            ("IPAddress", typeof(string)),
            ("ClientName", typeof(string)),
            ("ClientType", typeof(string)),
            ("ClientFamily", typeof(string)),
            ("Device", typeof(string)),
            ("OperatingSystemFamily", typeof(string)),
            ("OperatingSystem", typeof(string)),
            ("DisplayDate", typeof(DateTime)),
            ("IsSuspectedBOT", typeof(bool)));

        foreach (var record in records)
        {
            Add(
                table,
                ("ID", ToInt32(record.Id)),
                ("SubaccountID", ToInt32(record.SubaccountId)),
                ("SendContactID", ToInt32(record.SendContactId)),
                ("IPAddress", record.IpAddress),
                ("ClientName", record.ClientName),
                ("ClientType", record.ClientType),
                ("ClientFamily", record.ClientFamily),
                ("Device", record.Device),
                ("OperatingSystemFamily", record.OperatingSystemFamily),
                ("OperatingSystem", record.OperatingSystem),
                ("DisplayDate", ToDateTime(record.DisplayDate)),
                ("IsSuspectedBOT", record.IsSuspectedBot));
        }
        return table;
    }

    public static DataTable CreateLinks(IReadOnlyCollection<LinkApiRecord> records)
    {
        var table = Table(
            ImportTableNames.Links,
            ("ID", typeof(int)),
            ("SendID", typeof(int)),
            ("URL", typeof(string)),
            ("FriendlyName", typeof(string)),
            ("IsMonitored", typeof(bool)),
            ("ReceivedInMessageFormat", typeof(string)));

        foreach (var record in records)
        {
            Add(
                table,
                ("ID", ToInt32(record.Id)),
                ("SendID", ToInt32(record.SendId)),
                ("URL", record.Url),
                ("FriendlyName", record.FriendlyName),
                ("IsMonitored", record.IsMonitored),
                ("ReceivedInMessageFormat", record.ReceivedInMessageFormat));
        }
        return table;
    }

    public static DataTable CreateDisplayedContacts(IReadOnlyCollection<DisplayedContactApiRecord> records)
    {
        var table = Table(
            ImportTableNames.DisplayedContacts,
            ("ID", typeof(int)),
            ("SubaccountID", typeof(int)),
            ("SendContactID", typeof(int)),
            ("UserAgentID", typeof(int)),
            ("DisplayDate", typeof(DateTime)),
            ("Format", typeof(string)),
            ("TimeInSecondsSpentReadingEmail", typeof(int)),
            ("IsSuspectedBOT", typeof(bool)));

        foreach (var record in records)
        {
            Add(
                table,
                ("ID", ToInt32(record.Id)),
                ("SubaccountID", ToInt32(record.SubaccountId)),
                ("SendContactID", ToInt32(record.SendContactId)),
                ("UserAgentID", ToInt32(record.UserAgentId)),
                ("DisplayDate", ToDateTime(record.DisplayDate)),
                ("Format", record.Format),
                ("TimeInSecondsSpentReadingEmail", record.TimeInSecondsSpentReadingEmail),
                ("IsSuspectedBOT", record.IsSuspectedBot));
        }
        return table;
    }

    public static DataTable CreateClickedContacts(IReadOnlyCollection<ClickedContactApiRecord> records)
    {
        var table = Table(
            ImportTableNames.ClickedContacts,
            ("ID", typeof(int)),
            ("SubaccountID", typeof(int)),
            ("SendContactID", typeof(int)),
            ("LinkID", typeof(int)),
            ("UserAgentID", typeof(int)),
            ("ClickDate", typeof(DateTime)),
            ("FriendlyName", typeof(string)),
            ("IsSuspectedBOT", typeof(bool)));

        foreach (var record in records)
        {
            Add(
                table,
                ("ID", ToInt32(record.Id)),
                ("SubaccountID", ToInt32(record.SubaccountId)),
                ("SendContactID", ToInt32(record.SendContactId)),
                ("LinkID", ToInt32(record.LinkId)),
                ("UserAgentID", ToInt32(record.UserAgentId)),
                ("ClickDate", ToDateTime(record.ClickDate)),
                ("FriendlyName", record.FriendlyName),
                ("IsSuspectedBOT", record.IsSuspectedBot));
        }
        return table;
    }

    public static DataTable CreateBouncedContacts(IReadOnlyCollection<BouncedContactApiRecord> records)
    {
        var table = Table(
            ImportTableNames.BouncedContacts,
            ("ID", typeof(int)),
            ("SubaccountID", typeof(int)),
            ("SendContactID", typeof(int)),
            ("BounceReason", typeof(string)),
            ("BounceType", typeof(string)),
            ("BounceTypeID", typeof(int)),
            ("BounceDate", typeof(DateTime)),
            ("ResponseText", typeof(string)));

        foreach (var record in records)
        {
            Add(
                table,
                ("ID", ToInt32(record.Id)),
                ("SubaccountID", ToInt32(record.SubaccountId)),
                ("SendContactID", ToInt32(record.SendContactId)),
                ("BounceReason", record.BounceReason),
                ("BounceType", record.BounceType),
                ("BounceTypeID", ToInt32(record.BounceTypeId)),
                ("BounceDate", ToDateTime(record.BounceDate)),
                ("ResponseText", record.ResponseText));
        }
        return table;
    }

    public static DataTable CreateUnsubscribedContacts(IReadOnlyCollection<UnsubscribedContactApiRecord> records)
    {
        var table = Table(
            ImportTableNames.UnsubscribedContacts,
            ("ID", typeof(int)),
            ("SubaccountID", typeof(int)),
            ("SendContactID", typeof(int)),
            ("UnsubscribedDate", typeof(DateTime)),
            ("IsGlobalUnsubscribe", typeof(bool)),
            ("IsComplaint", typeof(bool)));

        foreach (var record in records)
        {
            Add(
                table,
                ("ID", ToInt32(record.Id)),
                ("SubaccountID", ToInt32(record.SubaccountId)),
                ("SendContactID", ToInt32(record.SendContactId)),
                ("UnsubscribedDate", ToDateTime(record.UnsubscribedDate)),
                ("IsGlobalUnsubscribe", record.IsGlobalUnsubscribe),
                ("IsComplaint", record.IsComplaint));
        }
        return table;
    }

    private static DataTable Table(string name, params (string Name, Type Type)[] columns)
    {
        var table = new DataTable(name);
        foreach (var column in columns) table.Columns.Add(column.Name, column.Type);
        return table;
    }

    private static void Add(DataTable table, params (string Name, object? Value)[] values)
    {
        var row = table.NewRow();
        foreach (var value in values) row[value.Name] = value.Value ?? DBNull.Value;
        table.Rows.Add(row);
    }

    private static int? ToInt32(long? value) => value.HasValue ? checked((int)value.Value) : null;

    private static DateTime? ToDateTime(DateTimeOffset? value) => value?.UtcDateTime;
}
