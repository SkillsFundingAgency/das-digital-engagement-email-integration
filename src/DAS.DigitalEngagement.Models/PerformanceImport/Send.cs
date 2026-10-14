using System.Text.Json.Serialization;

namespace DAS.DigitalEngagement.Models.PerformanceImport;

public class Send
{
    [JsonPropertyName("ID")] public long Id { get; set; }
    [JsonPropertyName("Name")] public string? Name { get; set; }
    [JsonPropertyName("CampaignID")] public long? CampaignId { get; set; }
    [JsonPropertyName("SubaccountID")] public long? SubaccountId { get; set; }
    [JsonPropertyName("SendTypeID")] public long? SendTypeId { get; set; }
    [JsonPropertyName("MessageDesignID")] public long? MessageDesignId { get; set; }
    [JsonPropertyName("SendType")] public string? SendType { get; set; }
    [JsonPropertyName("Status")] public string? Status { get; set; }
    [JsonPropertyName("SubStatus")] public string? SubStatus { get; set; }
    [JsonPropertyName("SendDate")] public DateTimeOffset? SendDate { get; set; }
    [JsonPropertyName("SendCompletedDate")] public DateTimeOffset? SendCompletedDate { get; set; }
    [JsonPropertyName("IsOutbox")] public bool? IsOutbox { get; set; }
    [JsonPropertyName("CampaignType")] public string? CampaignType { get; set; }
    [JsonPropertyName("MessageType")] public string? MessageType { get; set; }
    [JsonPropertyName("ContactCount")] public int? ContactCount { get; set; }
    [JsonPropertyName("CreatedBy")] public string? CreatedBy { get; set; }
    [JsonPropertyName("CreatedByUserID")] public long? CreatedByUserId { get; set; }
    [JsonPropertyName("ConfirmedByUserID")] public long? ConfirmedByUserId { get; set; }
    [JsonPropertyName("IsArchived")] public bool? IsArchived { get; set; }
    [JsonPropertyName("Sequence")] public int? Sequence { get; set; }
    [JsonPropertyName("CreatedDate")] public DateTimeOffset? CreatedDate { get; set; }
    [JsonPropertyName("FirstSendDate")] public DateTimeOffset? FirstSendDate { get; set; }
    [JsonPropertyName("LastSendDate")] public DateTimeOffset? LastSendDate { get; set; }
    [JsonPropertyName("FromEmail")] public string? FromEmail { get; set; }
    [JsonPropertyName("FromName")] public string? FromName { get; set; }
    [JsonPropertyName("ReplyEmail")] public string? ReplyEmail { get; set; }
    [JsonPropertyName("SubjectLine")] public string? SubjectLine { get; set; }
    [JsonPropertyName("Account")] public string? Account { get; set; }
}
