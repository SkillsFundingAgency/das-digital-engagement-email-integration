using System.Data;
using DAS.DigitalEngagement.CampaignInterest.Data.Helpers;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Polly;
using Polly.Retry;

namespace DAS.DigitalEngagement.CampaignInterest.Data.Service;

/// <summary>
/// Writes batches to SQL using bulk copy. Duplicate-safe writes stage rows in a temporary table,
/// deduplicate by ID, and insert only IDs not already in the destination; existing rows are not updated.
/// </summary>
public sealed class SqlBulkInserter(
    ILogger<SqlBulkInserter> logger,
    ISqlConnectionFactory connectionFactory) : ISqlBulkInserter
{
    // Restrict bulk inserts to supported import tables.
    // Update this list when adding new import targets.
    private static readonly HashSet<string> AllowedDestinationTables = new(StringComparer.OrdinalIgnoreCase)
    {
        ImportTableNames.Contacts,
        ImportTableNames.SendContacts,
        ImportTableNames.Links,
        ImportTableNames.UserAgents,
        ImportTableNames.DisplayedContacts,
        ImportTableNames.ClickedContacts,
        ImportTableNames.BouncedContacts,
        ImportTableNames.UnsubscribedContacts,
        ImportTableNames.CampaignImportMetadata,
        ImportTableNames.Sends,
        ImportTableNames.Campaigns
    };
    // Retry allowlisted transient SQL failures up to three times, waiting one second between attempts.
    private const int SqlRetryCount = 3;
    private static readonly TimeSpan SqlRetryDelay = TimeSpan.FromSeconds(1);
    private readonly ResiliencePipeline _sqlRetryPipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = SqlRetryCount,
            Delay = SqlRetryDelay,
            BackoffType = DelayBackoffType.Constant,
            ShouldHandle = new PredicateBuilder()
                .Handle<SqlException>(IsTransientSqlException),
            OnRetry = args =>
            {
                logger.LogWarning(
                    args.Outcome.Exception,
                    "Transient SQL error on attempt {Attempt} of {MaxAttempts}; retrying in {RetryDelaySeconds} seconds.",
                    args.AttemptNumber + 1,
                    SqlRetryCount + 1,
                    SqlRetryDelay.TotalSeconds);
                return default;
            }
        })
        .Build();

    public async Task BulkInsertAsync(
        string destinationTable,
        DataTable table,
        int batchSize = 5000,
        int timeoutSeconds = 300,
        CancellationToken cancellationToken = default)
    {
        await InsertAsync(
            destinationTable,
            table,
            batchSize,
            timeoutSeconds,
            ignoreDuplicates: false,
            cancellationToken: cancellationToken);
    }

    public async Task BulkInsertIgnoringDuplicatesAsync(
        string destinationTable,
        DataTable table,
        int batchSize = 5000,
        int timeoutSeconds = 300,
        CancellationToken cancellationToken = default)
    {
        await InsertAsync(
            destinationTable,
            table,
            batchSize,
            timeoutSeconds,
            ignoreDuplicates: true,
            cancellationToken: cancellationToken);
    }

    private async Task InsertAsync(
        string destinationTable,
        DataTable table,
        int batchSize,
        int timeoutSeconds,
        bool ignoreDuplicates,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationTable);
        ArgumentNullException.ThrowIfNull(table);

        // Enforce an allowlist of destination tables to prevent dynamic writes to arbitrary objects.
        if (!AllowedDestinationTables.Contains(destinationTable))
        {
            throw new ArgumentException($"Destination table '{destinationTable}' is not allowed for bulk inserts.", nameof(destinationTable));
        }

        if (table.Rows.Count == 0)
        {
            return;
        }

        var effectiveBatchSize = Math.Max(1, Math.Min(batchSize, table.Rows.Count));
        var stopwatch = Stopwatch.StartNew();

        try
        {
            logger.LogInformation(
                "Bulk inserting {Rows} rows into {Table}.",
                table.Rows.Count,
                destinationTable);

            await _sqlRetryPipeline.ExecuteAsync(
                async token => await InsertAttemptAsync(
                    destinationTable,
                    table,
                    effectiveBatchSize,
                    timeoutSeconds,
                    ignoreDuplicates,
                    token),
                cancellationToken);

            stopwatch.Stop();
            logger.LogInformation(
                "Bulk insert completed for {Rows} rows into {Table} in {ElapsedMilliseconds} ms.",
                table.Rows.Count,
                destinationTable,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException(
                $"Bulk insert failed for destination table '{destinationTable}'.",
                ex);
        }
    }

    private async Task InsertAttemptAsync(
        string destinationTable,
        DataTable table,
        int batchSize,
        int timeoutSeconds,
        bool ignoreDuplicates,
        CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            if (ignoreDuplicates)
            {
                await InsertIgnoringDuplicateIdsAsync(
                    connection,
                    transaction,
                    destinationTable,
                    table,
                    batchSize,
                    timeoutSeconds,
                    cancellationToken);
            }
            else
            {
                await BulkCopyAsync(
                    connection,
                    transaction,
                    destinationTable,
                    table,
                    batchSize,
                    timeoutSeconds,
                    cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            try
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }
            catch (SqlException rollbackException)
            {
                logger.LogWarning(rollbackException, "Failed to roll back bulk insert transaction for {Table}.", destinationTable);
            }

            throw;
        }
    }

    // Retry timeouts, connectivity failures, deadlocks, and Azure SQL throttling/resource errors;
    // data and constraint errors are intentionally excluded.
    private static bool IsTransientSqlException(SqlException exception) =>
        exception.Number is -2 or 20 or 64 or 233 or 1205 or 10053 or 10054 or 10060 or
            10928 or 10929 or 40197 or 40501 or 40613 or 49918 or 49919 or 49920;

    private static async Task BulkCopyAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string destinationTable,
        DataTable table,
        int batchSize,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        using var bulkCopy = CreateBulkCopy(connection, transaction, destinationTable, table, batchSize, timeoutSeconds);
        await bulkCopy.WriteToServerAsync(table, cancellationToken);
    }

    private static async Task InsertIgnoringDuplicateIdsAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string destinationTable,
        DataTable table,
        int batchSize,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        // Stage the batch, deduplicate incoming IDs, then insert only IDs absent from the target.
        // Existing target rows are left unchanged; this is insert-if-missing, not an update/upsert.
        var temporaryTable = $"#BulkInsert_{Guid.NewGuid():N}";
        var quotedDestination = QuoteMultipartIdentifier(destinationTable);
        var quotedTemporaryTable = QuoteIdentifier(temporaryTable);

        await using (var createTable = new SqlCommand(
            $"SELECT TOP (0) * INTO {quotedTemporaryTable} FROM {quotedDestination};", // NOSONAR: SQL parameters cannot bind identifiers; both identifiers are quoted and escaped.
            connection,
            transaction))
        {
            createTable.CommandTimeout = Math.Max(1, timeoutSeconds);
            await createTable.ExecuteNonQueryAsync(cancellationToken);
        }

        await BulkCopyAsync(connection, transaction, temporaryTable, table, batchSize, timeoutSeconds, cancellationToken);

        var columns = string.Join(", ", table.Columns.Cast<DataColumn>().Select(column => QuoteIdentifier(column.ColumnName)));
        var sql = $"""
            WITH Deduplicated AS
            (
                SELECT {columns},
                       ROW_NUMBER() OVER (PARTITION BY [ID] ORDER BY (SELECT NULL)) AS [RowNumber]
                FROM {quotedTemporaryTable}
            )
            INSERT INTO {quotedDestination} ({columns})
            SELECT {columns}
            FROM Deduplicated AS source
            WHERE source.[RowNumber] = 1
              AND NOT EXISTS
              (
                  SELECT 1
                  FROM {quotedDestination} AS target
                  WHERE target.[ID] = source.[ID]
              );
            DROP TABLE {quotedTemporaryTable};
            """;

        await using var insertCommand = new SqlCommand(sql, connection, transaction); // NOSONAR: SQL parameters cannot bind identifiers; all table and column names are quoted and escaped.
        insertCommand.CommandTimeout = Math.Max(1, timeoutSeconds);
        await insertCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private static SqlBulkCopy CreateBulkCopy(
        SqlConnection connection,
        SqlTransaction transaction,
        string destinationTable,
        DataTable table,
        int batchSize,
        int timeoutSeconds)
    {
        var bulkCopy = new SqlBulkCopy(
            connection,
            SqlBulkCopyOptions.TableLock | SqlBulkCopyOptions.KeepNulls,
            transaction)
        {
            DestinationTableName = destinationTable,
            BatchSize = batchSize,
            BulkCopyTimeout = Math.Max(1, timeoutSeconds)
        };

        foreach (DataColumn column in table.Columns)
        {
            bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
        }

        return bulkCopy;
    }

    public async Task<HashSet<long>> QueryExistingIdsAsync(
        string destinationTable,
        IReadOnlyCollection<long> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids == null || ids.Count == 0) return new HashSet<long>();

        // Enforce allowlist for safety
        if (!AllowedDestinationTables.Contains(destinationTable))
        {
            throw new ArgumentException($"Destination table '{destinationTable}' is not allowed for querying.", nameof(destinationTable));
        }

        var quotedDestination = QuoteMultipartIdentifier(destinationTable);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.OpenAsync(cancellationToken);

        var existing = new HashSet<long>();
        // Keep parameter count under SQL Server's 2100 parameter limit.
        foreach (var idBatch in ids.Distinct().Chunk(1000))
        {
            var parameterNames = new List<string>(idBatch.Length);
            await using var command = new SqlCommand { Connection = connection };

            for (var index = 0; index < idBatch.Length; index++)
            {
                var parameterName = $"@id{index}";
                parameterNames.Add(parameterName);
                command.Parameters.Add(parameterName, SqlDbType.BigInt).Value = idBatch[index];
            }

            command.CommandText = $"SELECT [ID] FROM {quotedDestination} WHERE [ID] IN ({string.Join(", ", parameterNames)});";

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var idValue = reader.GetValue(0);
                if (idValue is DBNull)
                {
                    continue;
                }

                existing.Add(Convert.ToInt64(idValue));
            }
        }

        return existing;
    }

    private static string QuoteMultipartIdentifier(string identifier)
    {
        return string.Join(
            ".",
            identifier.Split('.', StringSplitOptions.RemoveEmptyEntries)
                .Select(QuoteIdentifier));
    }

    private static string QuoteIdentifier(string identifier) =>
        $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]";
}
