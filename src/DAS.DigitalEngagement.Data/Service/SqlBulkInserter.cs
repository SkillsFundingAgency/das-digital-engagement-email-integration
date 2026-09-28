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
            cancellationToken,
            ignoreDuplicates: false);
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
            cancellationToken,
            ignoreDuplicates: true);
    }

    private async Task InsertAsync(
        string destinationTable,
        DataTable table,
        int batchSize,
        int timeoutSeconds,
        CancellationToken cancellationToken,
        bool ignoreDuplicates)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationTable);
        ArgumentNullException.ThrowIfNull(table);

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
                    token,
                    ignoreDuplicates),
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
            logger.LogError(ex, "Bulk insert failed for {Table}.", destinationTable);
            throw;
        }
    }

    private async Task InsertAttemptAsync(
        string destinationTable,
        DataTable table,
        int batchSize,
        int timeoutSeconds,
        CancellationToken cancellationToken,
        bool ignoreDuplicates)
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
            $"SELECT TOP (0) * INTO {quotedTemporaryTable} FROM {quotedDestination};",
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

        await using var insertCommand = new SqlCommand(sql, connection, transaction);
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
