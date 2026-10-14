using System.Text.Json.Serialization;

namespace DAS.DigitalEngagement.Models.PerformanceImport;

public sealed class UserAgentApiRecord
{
    [JsonPropertyName("ID")] public long Id { get; init; }
    [JsonPropertyName("SendContactID")] public long SendContactId { get; init; }
    [JsonPropertyName("DisplayDate")] public DateTimeOffset? DisplayDate { get; init; }
    [JsonPropertyName("IPAddress")] public string? IpAddress { get; init; }
    [JsonPropertyName("ClientName")] public string? ClientName { get; init; }
    [JsonPropertyName("ClientType")] public string? ClientType { get; init; }
    [JsonPropertyName("ClientFamily")] public string? ClientFamily { get; init; }
    [JsonPropertyName("Device")] public string? Device { get; init; }
    [JsonPropertyName("OperatingSystemFamily")] public string? OperatingSystemFamily { get; init; }
    [JsonPropertyName("OperatingSystem")] public string? OperatingSystem { get; init; }
    [JsonPropertyName("IsSuspectedBOT")] public bool? IsSuspectedBot { get; init; }
}
