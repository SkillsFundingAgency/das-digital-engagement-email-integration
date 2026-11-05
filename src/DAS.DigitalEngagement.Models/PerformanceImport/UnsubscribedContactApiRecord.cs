using System.Text.Json.Serialization;

namespace DAS.DigitalEngagement.Models.PerformanceImport;

public sealed class UnsubscribedContactApiRecord
{
    [JsonPropertyName("ID")] public long Id { get; init; }
    [JsonPropertyName("SubaccountID")] public long? SubaccountId { get; init; }
    [JsonPropertyName("UnsubscribeDate")] public DateTimeOffset? UnsubscribeDate { get; init; }
    [JsonPropertyName("SendContactID")] public long SendContactId { get; init; }
    [JsonPropertyName("IsGlobalUnsubscribe")] public bool? IsGlobalUnsubscribe { get; init; }
    [JsonPropertyName("IsComplaint")] public bool? IsComplaint { get; init; }
}
