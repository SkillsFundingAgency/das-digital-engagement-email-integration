using System.Text.Json.Serialization;

namespace DAS.DigitalEngagement.Models.PerformanceImport;

public sealed class SendEligibilityRecord
{
    [JsonPropertyName("ID")]
    public long Id { get; init; }

    [JsonPropertyName("SendCompletedDate")]
    public DateTimeOffset? SendCompletedDate { get; init; }
}
