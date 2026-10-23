namespace DAS.DigitalEngagement.Models.PerformanceImport;

public sealed class InteractionImportCounts
{
    public int LinkCount { get; set; }
    public int UserAgentCount { get; set; }
    public int DisplayedContactCount { get; set; }
    public int ClickedContactCount { get; set; }
    public int BouncedContactCount { get; set; }
    public int UnsubscribedContactCount { get; set; }

    public static InteractionImportCounts Empty => new();

    public void Add(InteractionImportCounts other)
    {
        LinkCount += other.LinkCount;
        UserAgentCount += other.UserAgentCount;
        DisplayedContactCount += other.DisplayedContactCount;
        ClickedContactCount += other.ClickedContactCount;
        BouncedContactCount += other.BouncedContactCount;
        UnsubscribedContactCount += other.UnsubscribedContactCount;
    }
}
