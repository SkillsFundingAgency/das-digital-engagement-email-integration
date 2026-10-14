using System.Text.Json.Serialization;

namespace DAS.DigitalEngagement.Models.PerformanceImport;

public sealed class LinkApiRecord
{
    [JsonPropertyName("ID")] public long Id { get; init; }
    [JsonPropertyName("URL")] public string? Url { get; init; }
    [JsonPropertyName("FriendlyName")] public string? FriendlyName { get; init; }
    [JsonPropertyName("IsMonitored")] public bool? IsMonitored { get; init; }
    [JsonPropertyName("ReceivedInMessageFormat")] public string? ReceivedInMessageFormat { get; init; }
    [JsonPropertyName("SendID")] public long SendId { get; init; }
}
