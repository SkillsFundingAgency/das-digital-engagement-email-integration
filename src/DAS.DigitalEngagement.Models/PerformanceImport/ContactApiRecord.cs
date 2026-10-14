using System.Text.Json.Serialization;

namespace DAS.DigitalEngagement.Models.PerformanceImport;

public sealed class ContactApiRecord
{
    [JsonPropertyName("ID")] public long Id { get; init; }
    [JsonPropertyName("SubaccountID")] public long? SubaccountId { get; init; }
    [JsonPropertyName("SalesforceContactID")] public string? SalesforceContactId { get; init; }
    [JsonPropertyName("SalesforceLeadID")] public string? SalesforceLeadId { get; init; }
    [JsonPropertyName("MicrosoftDynamicsContactID")] public string? MicrosoftDynamicsContactId { get; init; }
    [JsonPropertyName("MicrosoftDynamicsLeadID")] public string? MicrosoftDynamicsLeadId { get; init; }
    [JsonPropertyName("SourceID")] public long? SourceId { get; init; }
    [JsonPropertyName("EmailFormat")] public string? EmailFormat { get; init; }
    [JsonPropertyName("EmailFormatID")] public long? EmailFormatId { get; init; }
    [JsonPropertyName("Email")] public string? Email { get; init; }
    [JsonPropertyName("EmailStatusID")] public long? EmailStatusId { get; init; }
    [JsonPropertyName("MobileStatusID")] public long? MobileStatusId { get; init; }
    [JsonPropertyName("MobileNumber")] public string? MobileNumber { get; init; }
    [JsonPropertyName("MobileRegion")] public long? MobileRegion { get; init; }
    [JsonPropertyName("Salutation")] public string? Salutation { get; init; }
    [JsonPropertyName("Firstname")] public string? FirstName { get; init; }
    [JsonPropertyName("Lastname")] public string? LastName { get; init; }
    [JsonPropertyName("Telephone")] public string? Telephone { get; init; }
    [JsonPropertyName("URN")] public string? Urn { get; init; }
    [JsonPropertyName("Company")] public string? Company { get; init; }
    [JsonPropertyName("CreatedDate")] public DateTimeOffset? CreatedDate { get; init; }
    [JsonPropertyName("CreatedFromSourceID")] public long? CreatedFromSourceId { get; init; }
    [JsonPropertyName("ModifiedDate")] public DateTimeOffset? ModifiedDate { get; init; }
    [JsonPropertyName("ModifiedFromSourceID")] public long? ModifiedFromSourceId { get; init; }
    [JsonPropertyName("EmailUnsubscribedDate")] public DateTimeOffset? EmailUnsubscribedDate { get; init; }
    [JsonPropertyName("EmailUnsubscribedFromSourceID")] public long? EmailUnsubscribedFromSourceId { get; init; }
    [JsonPropertyName("SmsUnsubscribedDate")] public DateTimeOffset? SmsUnsubscribedDate { get; init; }
    [JsonPropertyName("SmsUnsubscribedFromSourceID")] public long? SmsUnsubscribedFromSourceId { get; init; }
    [JsonPropertyName("EmailHardBounceDate")] public DateTimeOffset? EmailHardBounceDate { get; init; }
    [JsonPropertyName("DoubleOptInConfirmationDate")] public DateTimeOffset? DoubleOptInConfirmationDate { get; init; }
    [JsonPropertyName("InleadsScore")] public long? InleadsScore { get; init; }
    [JsonPropertyName("CustomerAccountID")] public long? CustomerAccountId { get; init; }
    [JsonPropertyName("EmailEngagementStatus")] public long? EmailEngagementStatus { get; init; }
    [JsonPropertyName("HardBouncedDate")] public DateTimeOffset? HardBouncedDate { get; init; }
    [JsonPropertyName("LastSoftBouncedDate")] public DateTimeOffset? LastSoftBouncedDate { get; init; }
    [JsonPropertyName("LastSentDate")] public DateTimeOffset? LastSentDate { get; init; }
    [JsonPropertyName("LastDisplayedDate")] public DateTimeOffset? LastDisplayedDate { get; init; }
    [JsonPropertyName("LastClickedDate")] public DateTimeOffset? LastClickedDate { get; init; }
    [JsonPropertyName("FirstInleadsVisitDate")] public DateTimeOffset? FirstInleadsVisitDate { get; init; }
    [JsonPropertyName("LastInleadsVisitDate")] public DateTimeOffset? LastInleadsVisitDate { get; init; }
    [JsonPropertyName("IncludeInUR")] public bool? IncludeInUr { get; init; }
}
