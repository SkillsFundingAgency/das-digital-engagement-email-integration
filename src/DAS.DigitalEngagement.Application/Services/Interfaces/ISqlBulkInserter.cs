
using System.Data;

namespace DAS.DigitalEngagement.Application.Services.Interfaces
{
    public interface ISqlBulkInserter
    {
        /// <summary>
        /// Bulk insert the provided DataTable into the destination table. Caller controls transaction semantics.
        /// </summary>
        Task BulkInsertAsync( string destinationTable, DataTable table, int batchSize = 5000, int timeoutSeconds = 300, CancellationToken cancellationToken = default);
    }
}