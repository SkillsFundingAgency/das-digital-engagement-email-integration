using System.Data;
using DAS.DigitalEngagement.Models.PerformanceImport;

namespace DAS.DigitalEngagement.Application.PerformanceImport.Mappers;

public static class SendCampaignDataTableMapper
{
    private static class Columns
    {
        public const string Id = "ID";
        public const string Name = "Name";
        public const string SubaccountId = "SubaccountID";
        public const string CampaignId = "CampaignID";
        public const string SendId = "SendID";
        public const string SendTypeId = "SendTypeID";
        public const string MessageDesignId = "MessageDesignID";
        public const string SendType = "SendType";
        public const string Status = "Status";
        public const string SubStatus = "SubStatus";
        public const string SendDate = "SendDate";
        public const string SendCompletedDate = "SendCompletedDate";
        public const string IsOutbox = "IsOutbox";
        public const string CampaignType = "CampaignType";
        public const string CampaignTypeId = "CampaignTypeID";
        public const string MessageType = "MessageType";
        public const string Type = "Type";
        public const string ContactCount = "ContactCount";
        public const string CreatedBy = "CreatedBy";
        public const string CreatedByUserId = "CreatedByUserID";
        public const string ConfirmedByUserId = "ConfirmedByUserID";
        public const string IsArchived = "IsArchived";
        public const string Sequence = "Sequence";
        public const string CreatedDate = "CreatedDate";
        public const string ModifiedBy = "ModifiedBy";
        public const string ModifiedByUserId = "ModifiedByUserID";
        public const string ModifiedDate = "ModifiedDate";
        public const string DeletedDate = "DeletedDate";
        public const string ReportDeletedDate = "ReportDeletedDate";
        public const string IsSetup = "IsSetup";
        public const string FirstSendDate = "FirstSendDate";
        public const string LastSendDate = "LastSendDate";
        public const string TotalSent = "TotalSent";
        public const string CanBeModified = "CanBeModified";
        public const string CanBeReported = "CanBeReported";
        public const string FromEmail = "FromEmail";
        public const string FromName = "FromName";
        public const string ReplyEmail = "ReplyEmail";
        public const string SubjectLine = "SubjectLine";
        public const string IsImportComplete = "IsImportComplete";
        public const string ImportStartDate = "ImportStartDate";
        public const string ImportEndDate = "ImportEndDate";
    }

    public static DataTable CreateSends(IReadOnlyCollection<Send> sends)
    {
        var table = new DataTable("Sends");
        AddColumns(table,
            (Columns.Id, typeof(int)),
            (Columns.Name, typeof(string)),
            (Columns.SubaccountId, typeof(int)),
            (Columns.CampaignId, typeof(int)),
            (Columns.SendTypeId, typeof(int)),
            (Columns.MessageDesignId, typeof(int)),
            (Columns.SendType, typeof(string)),
            (Columns.Status, typeof(string)),
            (Columns.SubStatus, typeof(string)),
            (Columns.SendDate, typeof(DateTime)),
            (Columns.SendCompletedDate, typeof(DateTime)),
            (Columns.IsOutbox, typeof(bool)),
            (Columns.CampaignType, typeof(string)),
            (Columns.MessageType, typeof(string)),
            (Columns.ContactCount, typeof(int)),
            (Columns.CreatedBy, typeof(string)),
            (Columns.CreatedByUserId, typeof(int)),
            (Columns.ConfirmedByUserId, typeof(int)),
            (Columns.IsArchived, typeof(bool)),
            (Columns.Sequence, typeof(int)),
            (Columns.CreatedDate, typeof(DateTime)),
            (Columns.FromEmail, typeof(string)),
            (Columns.FromName, typeof(string)),
            (Columns.ReplyEmail, typeof(string)),
            (Columns.SubjectLine, typeof(string)));

        foreach (var send in sends)
        {
            var row = table.NewRow();
            Set(row, Columns.Id, ToInt32(send.Id));
            Set(row, Columns.Name, send.Name);
            Set(row, Columns.SubaccountId, ToInt32(send.SubaccountId));
            Set(row, Columns.CampaignId, ToInt32(send.CampaignId));
            Set(row, Columns.SendTypeId, ToInt32(send.SendTypeId));
            Set(row, Columns.MessageDesignId, ToInt32(send.MessageDesignId));
            Set(row, Columns.SendType, send.SendType);
            Set(row, Columns.Status, send.Status);
            Set(row, Columns.SubStatus, send.SubStatus);
            Set(row, Columns.SendDate, ToDateTime(send.SendDate));
            Set(row, Columns.SendCompletedDate, ToDateTime(send.SendCompletedDate));
            Set(row, Columns.IsOutbox, send.IsOutbox);
            Set(row, Columns.CampaignType, send.CampaignType);
            Set(row, Columns.MessageType, send.MessageType);
            Set(row, Columns.ContactCount, send.ContactCount);
            Set(row, Columns.CreatedBy, send.CreatedBy);
            Set(row, Columns.CreatedByUserId, ToInt32(send.CreatedByUserId));
            Set(row, Columns.ConfirmedByUserId, ToInt32(send.ConfirmedByUserId));
            Set(row, Columns.IsArchived, send.IsArchived);
            Set(row, Columns.Sequence, send.Sequence);
            Set(row, Columns.CreatedDate, ToDateTime(send.CreatedDate));
            Set(row, Columns.FromEmail, send.FromEmail);
            Set(row, Columns.FromName, send.FromName);
            Set(row, Columns.ReplyEmail, send.ReplyEmail);
            Set(row, Columns.SubjectLine, send.SubjectLine);
            table.Rows.Add(row);
        }

        return table;
    }

    public static DataTable CreateCampaigns(IReadOnlyCollection<Campaign> campaigns)
    {
        var table = new DataTable("Campaigns");
        AddColumns(table,
            (Columns.Id, typeof(int)),
            (Columns.SubaccountId, typeof(int)),
            (Columns.Name, typeof(string)),
            (Columns.CampaignTypeId, typeof(int)),
            (Columns.Type, typeof(string)),
            (Columns.CreatedBy, typeof(string)),
            (Columns.CreatedByUserId, typeof(int)),
            (Columns.CreatedDate, typeof(DateTime)),
            (Columns.ModifiedBy, typeof(string)),
            (Columns.ModifiedByUserId, typeof(int)),
            (Columns.ModifiedDate, typeof(DateTime)),
            (Columns.DeletedDate, typeof(DateTime)),
            (Columns.ReportDeletedDate, typeof(DateTime)),
            (Columns.IsSetup, typeof(bool)),
            (Columns.Status, typeof(string)),
            (Columns.FirstSendDate, typeof(DateTime)),
            (Columns.LastSendDate, typeof(DateTime)),
            (Columns.TotalSent, typeof(int)),
            (Columns.CanBeModified, typeof(bool)),
            (Columns.CanBeReported, typeof(bool)),
            (Columns.SendDate, typeof(DateTime)),
            (Columns.FromEmail, typeof(string)),
            (Columns.FromName, typeof(string)),
            (Columns.ReplyEmail, typeof(string)),
            (Columns.SubjectLine, typeof(string)));

        foreach (var campaign in campaigns)
        {
            var row = table.NewRow();
            Set(row, Columns.Id, ToInt32(campaign.Id));
            Set(row, Columns.SubaccountId, ToInt32(campaign.SubaccountId));
            Set(row, Columns.Name, campaign.Name);
            Set(row, Columns.CampaignTypeId, ToInt32(campaign.CampaignTypeId));
            Set(row, Columns.Type, campaign.Type);
            Set(row, Columns.CreatedBy, campaign.CreatedBy);
            Set(row, Columns.CreatedByUserId, ToInt32(campaign.CreatedByUserId));
            Set(row, Columns.CreatedDate, ToDateTime(campaign.CreatedDate));
            Set(row, Columns.ModifiedBy, campaign.ModifiedBy);
            Set(row, Columns.ModifiedByUserId, ToInt32(campaign.ModifiedByUserId));
            Set(row, Columns.ModifiedDate, ToDateTime(campaign.ModifiedDate));
            Set(row, Columns.DeletedDate, ToDateTime(campaign.DeletedDate));
            Set(row, Columns.ReportDeletedDate, ToDateTime(campaign.ReportDeletedDate));
            Set(row, Columns.IsSetup, campaign.IsSetup);
            Set(row, Columns.Status, campaign.Status);
            Set(row, Columns.FirstSendDate, ToDateTime(campaign.FirstSendDate));
            Set(row, Columns.LastSendDate, ToDateTime(campaign.LastSendDate));
            Set(row, Columns.TotalSent, ToInt32(campaign.TotalSent));
            Set(row, Columns.CanBeModified, campaign.CanBeModified);
            Set(row, Columns.CanBeReported, campaign.CanBeReported);
            Set(row, Columns.SendDate, ToDateTime(campaign.SendDate));
            Set(row, Columns.FromEmail, campaign.FromEmail);
            Set(row, Columns.FromName, campaign.FromName);
            Set(row, Columns.ReplyEmail, campaign.ReplyEmail);
            Set(row, Columns.SubjectLine, campaign.SubjectLine);

            table.Rows.Add(row);
        }

        return table;
    }

    public static DataTable CreateImportMetadata(IEnumerable<SendWithCampaign> sends, DateTimeOffset importStart)
    {
        var table = new DataTable("CampaignImportMetadata");
        AddColumns(table,
            (Columns.SendId, typeof(int)),
            (Columns.CampaignId, typeof(int)),
            (Columns.IsImportComplete, typeof(bool)),
            (Columns.ImportStartDate, typeof(DateTime)),
            (Columns.ImportEndDate, typeof(DateTime)));

        foreach (var send in sends)
        {
            var row = table.NewRow();
            Set(row, Columns.SendId, ToInt32(send.Id));
            Set(row, Columns.CampaignId, ToInt32(send.CampaignId));
            Set(row, Columns.IsImportComplete, true);
            Set(row, Columns.ImportStartDate, importStart.UtcDateTime);
            Set(row, Columns.ImportEndDate, DateTime.UtcNow);
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
