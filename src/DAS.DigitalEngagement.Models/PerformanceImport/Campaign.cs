using System.Text.Json.Serialization;

namespace DAS.DigitalEngagement.Models.PerformanceImport;

public sealed class Campaign
{
    [JsonPropertyName("@odata.type")] public string? ODataType { get; init; }
    [JsonPropertyName("ID")] public long? Id { get; init; }
    [JsonPropertyName("SubaccountID")] public long? SubaccountId { get; init; }
    [JsonPropertyName("Name")] public string? Name { get; init; }
    [JsonPropertyName("CampaignTypeID")] public long? CampaignTypeId { get; init; }
    [JsonPropertyName("Type")] public string? Type { get; init; }
    [JsonPropertyName("CreatedBy")] public string? CreatedBy { get; init; }
    [JsonPropertyName("CreatedByUserID")] public long? CreatedByUserId { get; init; }
    [JsonPropertyName("CreatedDate")] public DateTimeOffset? CreatedDate { get; init; }
    [JsonPropertyName("ModifiedBy")] public string? ModifiedBy { get; init; }
    [JsonPropertyName("ModifiedByUserID")] public long? ModifiedByUserId { get; init; }
    [JsonPropertyName("ModifiedDate")] public DateTimeOffset? ModifiedDate { get; init; }
    [JsonPropertyName("DeletedDate")] public DateTimeOffset? DeletedDate { get; init; }
    [JsonPropertyName("ReportDeletedDate")] public DateTimeOffset? ReportDeletedDate { get; init; }
    [JsonPropertyName("IsSetup")] public bool? IsSetup { get; init; }
    [JsonPropertyName("Status")] public string? Status { get; init; }
    [JsonPropertyName("FirstSendDate")] public DateTimeOffset? FirstSendDate { get; init; }
    [JsonPropertyName("LastSendDate")] public DateTimeOffset? LastSendDate { get; init; }
    [JsonPropertyName("TotalSent")] public long? TotalSent { get; init; }
    [JsonPropertyName("CanBeModified")] public bool? CanBeModified { get; init; }
    [JsonPropertyName("CanBeReported")] public bool? CanBeReported { get; init; }
    [JsonPropertyName("SendDate")] public DateTimeOffset? SendDate { get; init; }
    [JsonPropertyName("FromEmail")] public string? FromEmail { get; init; }
    [JsonPropertyName("FromName")] public string? FromName { get; init; }
    [JsonPropertyName("ReplyEmail")] public string? ReplyEmail { get; init; }
    [JsonPropertyName("SubjectLine")] public string? SubjectLine { get; init; }
}
