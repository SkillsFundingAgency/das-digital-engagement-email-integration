using System.Text.Json.Serialization;

namespace DAS.DigitalEngagement.Models.PerformanceImport;

public sealed class SendContactApiRecord
{
    [JsonPropertyName("ID")] public long Id { get; init; }
    [JsonPropertyName("SendID")] public long SendId { get; init; }
    [JsonPropertyName("ContactID")] public long ContactId { get; init; }
    [JsonPropertyName("PublicIP")] public string? PublicIp { get; init; }
}
