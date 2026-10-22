namespace DAS.DigitalEngagement.Models.PerformanceImport;

public sealed class ImportTotals
{
    public int ImportedSendRecords { get; set; }
    public int ImportedCampaignRecords { get; set; }
    public int ImportedSendContactRecords { get; set; }
    public int ImportedContactRecords { get; set; }
    public int ImportedLinkRecords { get; set; }
    public int ImportedUserAgentRecords { get; set; }
    public int ImportedDisplayedContactRecords { get; set; }
    public int ImportedClickedContactRecords { get; set; }
    public int ImportedBouncedContactRecords { get; set; }
    public int ImportedUnsubscribedContactRecords { get; set; }

    public void AddInteractionCounts(InteractionImportCounts interactionCounts)
    {
        ImportedLinkRecords += interactionCounts.LinkCount;
        ImportedUserAgentRecords += interactionCounts.UserAgentCount;
        ImportedDisplayedContactRecords += interactionCounts.DisplayedContactCount;
        ImportedClickedContactRecords += interactionCounts.ClickedContactCount;
        ImportedBouncedContactRecords += interactionCounts.BouncedContactCount;
        ImportedUnsubscribedContactRecords += interactionCounts.UnsubscribedContactCount;
    }
}
