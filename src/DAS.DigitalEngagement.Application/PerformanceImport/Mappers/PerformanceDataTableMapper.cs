using System.Data;
using DAS.DigitalEngagement.Models.PerformanceImport;

namespace DAS.DigitalEngagement.Application.PerformanceImport.Mappers;

public static class PerformanceDataTableMapper
{
    public static DataTable CreateContacts(IReadOnlyCollection<ContactApiRecord> records)
    {
        var table = Table(
            "Contacts",
            ("ID", typeof(int)),
            ("Email", typeof(string)),
            ("Firstname", typeof(string)),
            ("Lastname", typeof(string)));

        foreach (var record in records)
        {
            Add(
                table,
                ("ID", ToInt32(record.Id)),
                ("Email", record.Email),
                ("Firstname", record.FirstName),
                ("Lastname", record.LastName));
        }

        return table;
    }

    public static DataTable CreateSendContacts(IReadOnlyCollection<SendContactApiRecord> records)
    {
        var table = Table(
            "SendContacts",
            ("ID", typeof(int)),
            ("SendID", typeof(int)),
            ("ContactID", typeof(int)),
            ("PublicIP", typeof(string)));

        foreach (var record in records)
        {
            Add(
                table,
                ("ID", ToInt32(record.Id)),
                ("SendID", ToInt32(record.SendId)),
                ("ContactID", ToInt32(record.ContactId)),
                ("PublicIP", record.PublicIp));
        }

        return table;
    }

    public static DataTable CreateImportMetadata(IReadOnlyCollection<Send> sends, DateTimeOffset importStart)
    {
        var table = Table(
            "CampaignImportMetadata",
            ("SendID", typeof(int)),
            ("CampaignID", typeof(int)),
            ("IsImportComplete", typeof(bool)),
            ("ImportStartDate", typeof(DateTime)),
            ("ImportEndDate", typeof(DateTime)));

        foreach (var send in sends)
        {
            Add(
                table,
                ("SendID", ToInt32(send.Id)),
                ("CampaignID", ToInt32(send.CampaignId)),
                ("IsImportComplete", true),
                ("ImportStartDate", importStart.UtcDateTime),
                ("ImportEndDate", DateTime.UtcNow));
        }

        return table;
    }

    public static DataTable CreateUserAgents(IReadOnlyCollection<UserAgentApiRecord> records)
    {
        var table = Table(
            "UserAgents",
            ("ID", typeof(int)),
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
            "Links",
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
            "DisplayedContacts",
            ("ID", typeof(int)),
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
            "ClickedContacts",
            ("ID", typeof(int)),
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
            "BouncedContacts",
            ("ID", typeof(int)),
            ("SendContactID", typeof(int)),
            ("BounceReason", typeof(string)),
            ("BounceType", typeof(string)),
            ("BounceDate", typeof(DateTime)),
            ("ResponseText", typeof(string)));

        foreach (var record in records)
        {
            Add(
                table,
                ("ID", ToInt32(record.Id)),
                ("SendContactID", ToInt32(record.SendContactId)),
                ("BounceReason", record.BounceReason),
                ("BounceType", record.BounceType),
                ("BounceDate", ToDateTime(record.BounceDate)),
                ("ResponseText", record.ResponseText));
        }
        return table;
    }

    public static DataTable CreateUnsubscribedContacts(IReadOnlyCollection<UnsubscribedContactApiRecord> records)
    {
        var table = Table(
            "UnsubscribedContacts",
            ("ID", typeof(int)),
            ("SendContactID", typeof(int)),
            ("UnsubscribeDate", typeof(DateTime)),
            ("IsGlobalUnsubscribe", typeof(bool)),
            ("IsComplaint", typeof(bool)));

        foreach (var record in records)
        {
            Add(
                table,
                ("ID", ToInt32(record.Id)),
                ("SendContactID", ToInt32(record.SendContactId)),
                ("UnsubscribeDate", ToDateTime(record.UnsubscribeDate)),
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
