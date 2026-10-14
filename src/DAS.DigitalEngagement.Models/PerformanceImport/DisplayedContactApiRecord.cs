using System.Text.Json.Serialization;

namespace DAS.DigitalEngagement.Models.PerformanceImport;

public sealed class DisplayedContactApiRecord
{
    [JsonPropertyName("ID")] public long Id { get; init; }
    [JsonPropertyName("DisplayDate")] public DateTimeOffset? DisplayDate { get; init; }
    [JsonPropertyName("Format")] public string? Format { get; init; }
    [JsonPropertyName("SendContactID")] public long SendContactId { get; init; }
    [JsonPropertyName("UserAgentID")] public long? UserAgentId { get; init; }
    [JsonPropertyName("TimeInSecondsSpentReadingEmail")] public int? TimeInSecondsSpentReadingEmail { get; init; }
    [JsonPropertyName("IsSuspectedBOT")] public bool? IsSuspectedBot { get; init; }
}
