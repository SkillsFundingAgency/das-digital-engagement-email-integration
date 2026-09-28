using System.Text.Json.Serialization;

namespace DAS.DigitalEngagement.Models.PerformanceImport;

public sealed class SendWithCampaign : Send
{
    [JsonPropertyName("Campaign")]
    public Campaign? Campaign { get; init; }
}
