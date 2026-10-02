using System.Data;
using DAS.DigitalEngagement.Application.PerformanceImport.Mappers;
using DAS.DigitalEngagement.CampaignInterest.Data.Service;
using DAS.DigitalEngagement.Models.PerformanceImport;

namespace DAS.DigitalEngagement.Application.PerformanceImport.Persistence;

public sealed class PerformanceDataWriter(
    ISqlBulkInserter bulkInserter) : IPerformanceDataWriter
{
    public Task WriteContactsAsync(IReadOnlyCollection<ContactApiRecord> records, CancellationToken cancellationToken = default) =>
        WriteIfAnyAsync("import.Contacts", PerformanceDataTableMapper.CreateContacts(records), ignoreDuplicates: true, cancellationToken);

    public Task WriteSendContactsAsync(IReadOnlyCollection<SendContactApiRecord> records, CancellationToken cancellationToken = default) =>
        WriteIfAnyAsync("import.SendContacts", PerformanceDataTableMapper.CreateSendContacts(records), ignoreDuplicates: true, cancellationToken);

    public Task WriteLinksAsync(IReadOnlyCollection<LinkApiRecord> records, CancellationToken cancellationToken = default) =>
        WriteIfAnyAsync("import.Links", PerformanceDataTableMapper.CreateLinks(records), ignoreDuplicates: true, cancellationToken);

    public Task WriteUserAgentsAsync(IReadOnlyCollection<UserAgentApiRecord> records, CancellationToken cancellationToken = default) =>
        WriteIfAnyAsync("import.UserAgents", PerformanceDataTableMapper.CreateUserAgents(records), ignoreDuplicates: true, cancellationToken);

    public Task WriteDisplayedContactsAsync(IReadOnlyCollection<DisplayedContactApiRecord> records, CancellationToken cancellationToken = default) =>
        WriteIfAnyAsync("import.DisplayedContacts", PerformanceDataTableMapper.CreateDisplayedContacts(records), ignoreDuplicates: true, cancellationToken);

    public Task WriteClickedContactsAsync(IReadOnlyCollection<ClickedContactApiRecord> records, CancellationToken cancellationToken = default) =>
        WriteIfAnyAsync("import.ClickedContacts", PerformanceDataTableMapper.CreateClickedContacts(records), ignoreDuplicates: true, cancellationToken);

    public Task WriteBouncedContactsAsync(IReadOnlyCollection<BouncedContactApiRecord> records, CancellationToken cancellationToken = default) =>
        WriteIfAnyAsync("import.BouncedContacts", PerformanceDataTableMapper.CreateBouncedContacts(records), ignoreDuplicates: true, cancellationToken);

    public Task WriteUnsubscribedContactsAsync(IReadOnlyCollection<UnsubscribedContactApiRecord> records, CancellationToken cancellationToken = default) =>
        WriteIfAnyAsync("import.UnsubscribedContacts", PerformanceDataTableMapper.CreateUnsubscribedContacts(records), ignoreDuplicates: true, cancellationToken);

    public Task WriteImportCompletionsAsync(IReadOnlyCollection<Send> sends, DateTimeOffset importStart, CancellationToken cancellationToken = default) =>
        WriteIfAnyAsync("import.CampaignImportMetadata", PerformanceDataTableMapper.CreateImportMetadata(sends, importStart), ignoreDuplicates: false, cancellationToken);

    private async Task WriteIfAnyAsync(
        string tableName,
        DataTable table,
        bool ignoreDuplicates,
        CancellationToken cancellationToken)
    {
        if (table.Rows.Count == 0)
        {
            return;
        }

        var insertTask = ignoreDuplicates
            ? bulkInserter.BulkInsertIgnoringDuplicatesAsync(
                tableName,
                table,
                batchSize: table.Rows.Count,
                cancellationToken: cancellationToken)
            : bulkInserter.BulkInsertAsync(
                tableName,
                table,
                batchSize: table.Rows.Count,
                cancellationToken: cancellationToken);

        await insertTask;
    }
}
