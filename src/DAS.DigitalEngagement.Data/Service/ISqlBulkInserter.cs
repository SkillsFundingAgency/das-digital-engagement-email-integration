using System.Data;

namespace DAS.DigitalEngagement.CampaignInterest.Data.Service;

public interface ISqlBulkInserter
{
    Task BulkInsertAsync(
        string destinationTable,
        DataTable table,
        int batchSize = 5000,
        int timeoutSeconds = 300,
        CancellationToken cancellationToken = default);

    Task BulkInsertIgnoringDuplicatesAsync(
        string destinationTable,
        DataTable table,
        int batchSize = 5000,
        int timeoutSeconds = 300,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Query which of the provided IDs already exist in the destination table's ID column.
    /// </summary>
    Task<HashSet<long>> QueryExistingIdsAsync(
        string destinationTable,
        IReadOnlyCollection<long> ids,
        CancellationToken cancellationToken = default);
}
