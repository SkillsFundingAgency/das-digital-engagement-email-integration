namespace DAS.DigitalEngagement.Models.PerformanceImport;

public sealed class ImportTotals
{
    private int _importedSendRecords;
    private int _importedCampaignRecords;
    private int _importedSendContactRecords;
    private int _importedContactRecords;
    private int _importedLinkRecords;
    private int _importedUserAgentRecords;
    private int _importedDisplayedContactRecords;
    private int _importedClickedContactRecords;
    private int _importedBouncedContactRecords;
    private int _importedUnsubscribedContactRecords;

    public int ImportedSendRecords => _importedSendRecords;
    public int ImportedCampaignRecords => _importedCampaignRecords;
    public int ImportedSendContactRecords => _importedSendContactRecords;
    public int ImportedContactRecords => _importedContactRecords;
    public int ImportedLinkRecords => _importedLinkRecords;
    public int ImportedUserAgentRecords => _importedUserAgentRecords;
    public int ImportedDisplayedContactRecords => _importedDisplayedContactRecords;
    public int ImportedClickedContactRecords => _importedClickedContactRecords;
    public int ImportedBouncedContactRecords => _importedBouncedContactRecords;
    public int ImportedUnsubscribedContactRecords => _importedUnsubscribedContactRecords;

    public void AddImportedSendRecords(int count) => Interlocked.Add(ref _importedSendRecords, count);
    public void AddImportedCampaignRecords(int count) => Interlocked.Add(ref _importedCampaignRecords, count);
    public void AddImportedSendContactRecords(int count) => Interlocked.Add(ref _importedSendContactRecords, count);
    public void AddImportedContactRecords(int count) => Interlocked.Add(ref _importedContactRecords, count);
    public void AddImportedLinkRecords(int count) => Interlocked.Add(ref _importedLinkRecords, count);
    public void AddImportedUserAgentRecords(int count) => Interlocked.Add(ref _importedUserAgentRecords, count);
    public void AddImportedDisplayedContactRecords(int count) => Interlocked.Add(ref _importedDisplayedContactRecords, count);
    public void AddImportedClickedContactRecords(int count) => Interlocked.Add(ref _importedClickedContactRecords, count);
    public void AddImportedBouncedContactRecords(int count) => Interlocked.Add(ref _importedBouncedContactRecords, count);
    public void AddImportedUnsubscribedContactRecords(int count) => Interlocked.Add(ref _importedUnsubscribedContactRecords, count);

}
