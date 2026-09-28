using System.Data;
using DAS.DigitalEngagement.Models.PerformanceImport;

namespace DAS.DigitalEngagement.Application.PerformanceImport.Mappers;

public static class SendCampaignDataTableMapper
{
    public static DataTable CreateSends(IReadOnlyCollection<Send> sends)
    {
        var table = new DataTable("Sends");
        AddColumns(table,
            ("ID", typeof(int)),
            ("Name", typeof(string)),
            ("SubaccountID", typeof(int)),
            ("CampaignID", typeof(int)),
            ("SendTypeID", typeof(int)),
            ("MessageDesignID", typeof(int)),
            ("SendType", typeof(string)),
            ("Status", typeof(string)),
            ("SubStatus", typeof(string)),
            ("SendDate", typeof(DateTime)),
            ("SendCompletedDate", typeof(DateTime)),
            ("IsOutbox", typeof(bool)),
            ("CampaignType", typeof(string)),
            ("MessageType", typeof(string)),
            ("ContactCount", typeof(int)),
            ("CreatedBy", typeof(string)),
            ("CreatedByUserID", typeof(int)),
            ("ConfirmedByUserID", typeof(int)),
            ("IsArchived", typeof(bool)),
            ("Sequence", typeof(int)),
            ("CreatedDate", typeof(DateTime)),
            ("FromEmail", typeof(string)),
            ("FromName", typeof(string)),
            ("ReplyEmail", typeof(string)),
            ("SubjectLine", typeof(string)));

        foreach (var send in sends)
        {
            var row = table.NewRow();
            Set(row, "ID", ToInt32(send.Id));
            Set(row, "Name", send.Name);
            Set(row, "SubaccountID", ToInt32(send.SubaccountId));
            Set(row, "CampaignID", ToInt32(send.CampaignId));
            Set(row, "SendTypeID", ToInt32(send.SendTypeId));
            Set(row, "MessageDesignID", ToInt32(send.MessageDesignId));
            Set(row, "SendType", send.SendType);
            Set(row, "Status", send.Status);
            Set(row, "SubStatus", send.SubStatus);
            Set(row, "SendDate", ToDateTime(send.SendDate));
            Set(row, "SendCompletedDate", ToDateTime(send.SendCompletedDate));
            Set(row, "IsOutbox", send.IsOutbox);
            Set(row, "CampaignType", send.CampaignType);
            Set(row, "MessageType", send.MessageType);
            Set(row, "ContactCount", send.ContactCount);
            Set(row, "CreatedBy", send.CreatedBy);
            Set(row, "CreatedByUserID", ToInt32(send.CreatedByUserId));
            Set(row, "ConfirmedByUserID", ToInt32(send.ConfirmedByUserId));
            Set(row, "IsArchived", send.IsArchived);
            Set(row, "Sequence", send.Sequence);
            Set(row, "CreatedDate", ToDateTime(send.CreatedDate));
            Set(row, "FromEmail", send.FromEmail);
            Set(row, "FromName", send.FromName);
            Set(row, "ReplyEmail", send.ReplyEmail);
            Set(row, "SubjectLine", send.SubjectLine);
            table.Rows.Add(row);
        }

        return table;
    }

    public static DataTable CreateCampaigns(IReadOnlyCollection<Campaign> campaigns)
    {
        var table = new DataTable("Campaigns");
        AddColumns(table,
            ("ID", typeof(int)),
            ("SubaccountID", typeof(int)),
            ("Name", typeof(string)),
            ("CampaignTypeID", typeof(int)),
            ("Type", typeof(string)),
            ("CreatedBy", typeof(string)),
            ("CreatedByUserID", typeof(int)),
            ("CreatedDate", typeof(DateTime)),
            ("ModifiedBy", typeof(string)),
            ("ModifiedByUserID", typeof(int)),
            ("ModifiedDate", typeof(DateTime)),
            ("DeletedDate", typeof(DateTime)),
            ("ReportDeletedDate", typeof(DateTime)),
            ("IsSetup", typeof(bool)),
            ("Status", typeof(string)),
            ("FirstSendDate", typeof(DateTime)),
            ("LastSendDate", typeof(DateTime)),
            ("TotalSent", typeof(int)),
            ("CanBeModified", typeof(bool)),
            ("CanBeReported", typeof(bool)),
            ("SendDate", typeof(DateTime)),
            ("FromEmail", typeof(string)),
            ("FromName", typeof(string)),
            ("ReplyEmail", typeof(string)),
            ("SubjectLine", typeof(string)));

        foreach (var campaign in campaigns)
        {
            var row = table.NewRow();
            Set(row, "ID", ToInt32(campaign.Id));
            Set(row, "SubaccountID", ToInt32(campaign.SubaccountId));
            Set(row, "Name", campaign.Name);
            Set(row, "CampaignTypeID", ToInt32(campaign.CampaignTypeId));
            Set(row, "Type", campaign.Type);
            Set(row, "CreatedBy", campaign.CreatedBy);
            Set(row, "CreatedByUserID", ToInt32(campaign.CreatedByUserId));
            Set(row, "CreatedDate", ToDateTime(campaign.CreatedDate));
            Set(row, "ModifiedBy", campaign.ModifiedBy);
            Set(row, "ModifiedByUserID", ToInt32(campaign.ModifiedByUserId));
            Set(row, "ModifiedDate", ToDateTime(campaign.ModifiedDate));
            Set(row, "DeletedDate", ToDateTime(campaign.DeletedDate));
            Set(row, "ReportDeletedDate", ToDateTime(campaign.ReportDeletedDate));
            Set(row, "IsSetup", campaign.IsSetup);
            Set(row, "Status", campaign.Status);
            Set(row, "FirstSendDate", ToDateTime(campaign.FirstSendDate));
            Set(row, "LastSendDate", ToDateTime(campaign.LastSendDate));
            Set(row, "TotalSent", ToInt32(campaign.TotalSent));
            Set(row, "CanBeModified", campaign.CanBeModified);
            Set(row, "CanBeReported", campaign.CanBeReported);
            Set(row, "SendDate", ToDateTime(campaign.SendDate));
            Set(row, "FromEmail", campaign.FromEmail);
            Set(row, "FromName", campaign.FromName);
            Set(row, "ReplyEmail", campaign.ReplyEmail);
            Set(row, "SubjectLine", campaign.SubjectLine);

            table.Rows.Add(row);
        }

        return table;
    }

    public static DataTable CreateImportMetadata(IEnumerable<SendWithCampaign> sends, DateTimeOffset importStart)
    {
        var table = new DataTable("CampaignImportMetadata");
        AddColumns(table,
            ("SendID", typeof(int)),
            ("CampaignID", typeof(int)),
            ("IsImportComplete", typeof(bool)),
            ("ImportStartDate", typeof(DateTime)),
            ("ImportEndDate", typeof(DateTime)));

        foreach (var send in sends)
        {
            var row = table.NewRow();
            Set(row, "SendID", ToInt32(send.Id));
            Set(row, "CampaignID", ToInt32(send.CampaignId));
            Set(row, "IsImportComplete", true);
            Set(row, "ImportStartDate", importStart.UtcDateTime);
            Set(row, "ImportEndDate", DateTime.UtcNow);
            table.Rows.Add(row);
        }

        return table;
    }

    private static void AddColumns(DataTable table, params (string Name, Type Type)[] columns)
    {
        foreach (var column in columns)
        {
            table.Columns.Add(column.Name, column.Type);
        }
    }

    private static void Set(DataRow row, string columnName, object? value)
    {
        row[columnName] = value ?? DBNull.Value;
    }

    private static int? ToInt32(long? value) => value.HasValue ? checked((int)value.Value) : null;

    private static DateTime? ToDateTime(DateTimeOffset? value) => value?.UtcDateTime;

}
