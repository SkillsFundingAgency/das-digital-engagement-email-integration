using System.Text.Json.Serialization;

namespace DAS.DigitalEngagement.Models.PerformanceImport;

public sealed class ODataResponse<T>
{
    [JsonPropertyName("@odata.count")]
    public long? Count { get; init; }

    [JsonPropertyName("value")]
    public List<T>? Value { get; init; }

    [JsonPropertyName("@odata.nextLink")]
    public string? NextLink { get; init; }
}
