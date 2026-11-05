namespace DAS.DigitalEngagement.Models.PerformanceImport;

public sealed class PerformanceImportReportSummary
{
    public DateTimeOffset StartedAtUtc { get; init; }
    public DateTimeOffset EndedAtUtc { get; init; }
    public PerformanceImportStatus Status { get; init; } = PerformanceImportStatus.Completed;
    public int EligibleSends { get; init; }
    public int ImportedSendRecords { get; init; }
    public int ImportedCampaignRecords { get; init; }
    public int ImportedSendContactRecords { get; init; }
    public int ImportedContactRecords { get; init; }
    public int ImportedLinkRecords { get; init; }
    public int ImportedUserAgentRecords { get; init; }
    public int ImportedDisplayedContactRecords { get; init; }
    public int ImportedClickedContactRecords { get; init; }
    public int ImportedBouncedContactRecords { get; init; }
    public int ImportedUnsubscribedContactRecords { get; init; }
    public int FailureCount { get; init; }
    public IReadOnlyDictionary<string, long?> ODataAvailableCounts { get; init; } = new Dictionary<string, long?>();
    public IReadOnlyCollection<string> Messages { get; init; } = Array.Empty<string>();
}
