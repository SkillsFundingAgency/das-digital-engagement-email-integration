using System.Text.Json.Serialization;

namespace DAS.DigitalEngagement.Models.PerformanceImport;

public sealed class ClickedContactApiRecord
{
    [JsonPropertyName("ID")] public long Id { get; init; }
    [JsonPropertyName("ClickDate")] public DateTimeOffset? ClickDate { get; init; }
    [JsonPropertyName("LinkID")] public long? LinkId { get; init; }
    [JsonPropertyName("SendContactID")] public long SendContactId { get; init; }
    [JsonPropertyName("UserAgentID")] public long? UserAgentId { get; init; }
    [JsonPropertyName("FriendlyName")] public string? FriendlyName { get; init; }
    [JsonPropertyName("IsSuspectedBOT")] public bool? IsSuspectedBot { get; init; }
}
