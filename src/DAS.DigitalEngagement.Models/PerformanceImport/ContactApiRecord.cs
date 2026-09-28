using System.Text.Json.Serialization;

namespace DAS.DigitalEngagement.Models.PerformanceImport;

public sealed class ContactApiRecord
{
    [JsonPropertyName("ID")] public long Id { get; init; }
    [JsonPropertyName("Email")] public string? Email { get; init; }
    [JsonPropertyName("Firstname")] public string? FirstName { get; init; }
    [JsonPropertyName("Lastname")] public string? LastName { get; init; }
}
