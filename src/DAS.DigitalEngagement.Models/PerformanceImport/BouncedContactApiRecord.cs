using System.Text.Json.Serialization;

namespace DAS.DigitalEngagement.Models.PerformanceImport;

public sealed class BouncedContactApiRecord
{
    [JsonPropertyName("ID")] public long Id { get; init; }
    [JsonPropertyName("SubaccountID")] public long? SubaccountId { get; init; }
    [JsonPropertyName("BounceReason")] public string? BounceReason { get; init; }
    [JsonPropertyName("BounceType")] public string? BounceType { get; init; }
    [JsonPropertyName("BounceTypeID")] public long? BounceTypeId { get; init; }
    [JsonPropertyName("BounceDate")] public DateTimeOffset? BounceDate { get; init; }
    [JsonPropertyName("SendContactID")] public long SendContactId { get; init; }
    [JsonPropertyName("ResponseText")] public string? ResponseText { get; init; }
}
