using DAS.DigitalEngagement.Application.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DAS.DigitalEngagement.Application.Services
{
    public sealed class SqlBulkInserter : ISqlBulkInserter
    {
        private readonly ILogger<SqlBulkInserter> _logger;
        private readonly string _connectionString;

        public SqlBulkInserter(ILogger<SqlBulkInserter> logger, IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            _connectionString = configuration.GetSection("ConnectionString")["CampaignsDatabase"]
                ?? throw new InvalidOperationException("Connection string 'CampaignsDatabase' not found.");
        }

        public async Task BulkInsertAsync(string destinationTable, DataTable table, int batchSize = 5000, int timeoutSeconds = 300, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(destinationTable)) throw new ArgumentNullException(nameof(destinationTable));
            if (table == null) throw new ArgumentNullException(nameof(table));

            var effectiveBatchSize = Math.Min(batchSize < 1 ? 1 : batchSize, Math.Max(1, table.Rows.Count));
         
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

            var options = SqlBulkCopyOptions.TableLock | SqlBulkCopyOptions.KeepNulls;

            using var bulk = new SqlBulkCopy(conn, options, null)
            {
                DestinationTableName = destinationTable,
                BatchSize = effectiveBatchSize,
                BulkCopyTimeout = timeoutSeconds
            };

            var parts = destinationTable.Split(new[] { '.' }, 2);
            var schemaName = parts.Length == 2 ? parts[0].Trim() : "dbo";
            var actualTableName = parts.Length == 2 ? parts[1].Trim() : destinationTable;

            if (schemaName.StartsWith("[") && schemaName.EndsWith("]"))
                schemaName = schemaName[1..^1];
            if (actualTableName.StartsWith("[") && actualTableName.EndsWith("]"))
                actualTableName = actualTableName[1..^1];

            var destinationColumns = await GetDestinationColumnsAsync(_connectionString, actualTableName, schemaName, cancellationToken).ConfigureAwait(false);
            var destinationSet = new HashSet<string>(destinationColumns, StringComparer.OrdinalIgnoreCase);

            var mappedCount = 0;
            var unmapped = new List<string>();

            foreach (DataColumn col in table.Columns)
            {
                if (destinationSet.Contains(col.ColumnName))
                {
                    bulk.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                    mappedCount++;
                }
                else
                {
                    unmapped.Add(col.ColumnName);
                }
            }

            if (unmapped.Count > 0)
                _logger.LogWarning("Some source columns were not mapped to destination table {Table}. UnmappedCount: {UnmappedCount}. ExampleUnmapped: {UnmappedExample}",
                    destinationTable, unmapped.Count, unmapped.Count > 0 ? unmapped[0] : null);

            _logger.LogDebug("Column mappings applied. Mapped: {MappedCount}. SourceColumns: {SourceColumns}. DestinationColumns: {DestinationColumnsCount}.",
                mappedCount, table.Columns.Count, destinationColumns.Count);

            try
            {
                var sw = Stopwatch.StartNew();
                _logger.LogInformation("Starting bulk write to {Table} for {Rows} rows.", destinationTable, table.Rows.Count);
                await bulk.WriteToServerAsync(table, cancellationToken).ConfigureAwait(false);
                sw.Stop();
                _logger.LogInformation("Bulk insert complete into {Table}. Rows: {Rows}. ElapsedMs: {ElapsedMs}.", destinationTable, table.Rows.Count, sw.ElapsedMilliseconds);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Bulk insert cancelled for {Table}. RowsProcessed (best-effort): {Rows}", destinationTable, table.Rows.Count);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk insert failed for {Table}. SourceRows: {Rows}. MappedColumns: {MappedCount}.", destinationTable, table.Rows.Count, mappedCount);
                throw;
            }
        }

        private static async Task<List<string>> GetDestinationColumnsAsync(string connectionString, string tableName, string schemaName = "dbo", CancellationToken cancellationToken = default)
        {
            var columns = new List<string>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            const string sql = @"
                SELECT COLUMN_NAME
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = @TableName
                  AND TABLE_SCHEMA = @SchemaName
                ORDER BY ORDINAL_POSITION";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@TableName", tableName);
            command.Parameters.AddWithValue("@SchemaName", schemaName);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                columns.Add(reader.GetString(0));
            }

            return columns;
        }
    }
}