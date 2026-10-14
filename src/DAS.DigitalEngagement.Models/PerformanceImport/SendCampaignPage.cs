namespace DAS.DigitalEngagement.Models.PerformanceImport;

public sealed class SendCampaignPage
{
    public required List<SendWithCampaign> Sends { get; init; }
    public string? NextLink { get; init; }
}
