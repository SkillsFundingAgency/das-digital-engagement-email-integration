using Dapper;
using DAS.DigitalEngagement.CampaignInterest.Data.Helpers;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Polly;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace DAS.DigitalEngagement.CampaignInterest.Data.Service;

public interface IBulkInsertService
{
    Task BulkInsertAsync<T>(IEnumerable<T> data, string tableName);
}

[ExcludeFromCodeCoverage]
public class BulkInsertService(IDbConnectionFactory factory, ILogger<BulkInsertService> logger) : IBulkInsertService
{
    private const int MaxParameters = 2000; // safely under SQL Server's hard limit of 2100

    // SQL error numbers that are permanent and should not be retried
    private static readonly HashSet<int> NonRetryableErrors = [544, 8003];

    public async Task BulkInsertAsync<T>(IEnumerable<T> data, string tableName)
    {
        var stopwatch = Stopwatch.StartNew();
        var items = data.ToList();

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Starting bulk insert into {Table} with {RowCount} rows", tableName, items.Count);
        }

        if (items.Count == 0)
        {
            logger.LogWarning("No rows to insert into {Table}", tableName);
            return;
        }

        // Exclude identity/database-generated columns — they cannot be explicitly inserted.
        // Covers three conventions:
        //   1. [DatabaseGenerated(DatabaseGeneratedOption.Identity)] explicitly present
        //   2. [Key] on a numeric property (EF auto-increment convention)
        //   3. Property named "Id" with a numeric type (bare POCO convention)
        var props = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => !IsIdentityProperty(p))
            .ToList();

        var columns = props.Select(p => p.Name).ToList();

        // Dynamically calculate chunk size to stay within SQL Server's 2100 parameter limit
        var chunkSize = Math.Max(1, MaxParameters / columns.Count);

        var chunks = items
            .Select((item, index) => (item, index))
            .GroupBy(x => x.index / chunkSize)
            .Select(g => g.Select(x => x.item).ToList())
            .ToList();

        logger.LogInformation(
            "Split {RowCount} rows into {ChunkCount} chunks of up to {ChunkSize} for table {Table} ({ColumnCount} columns)",
            items.Count, chunks.Count, chunkSize, tableName, columns.Count);

        foreach (var (chunk, index) in chunks.Select((c, i) => (c, i)))
        {
            logger.LogInformation("Inserting chunk {ChunkIndex}/{ChunkCount} ({RowCount} rows) into {Table}", index + 1, chunks.Count, chunk.Count, tableName);
            await InsertChunkAsync(chunk, tableName, columns, props);

            if (index < chunks.Count - 1)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        stopwatch.Stop();
        logger.LogInformation("Completed bulk insert into {Table} in {ElapsedMs} ms", tableName, stopwatch.ElapsedMilliseconds);
    }

    private async Task InsertChunkAsync<T>(List<T> chunk, string tableName, List<string> columns, List<PropertyInfo> props)
    {
        await Policy
            .Handle<SqlException>(ex => !NonRetryableErrors.Contains(ex.Number))
            .Or<Exception>(ex => ex is not SqlException && ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase))
            .WaitAndRetryAsync(
                retryCount: 5,
                sleepDurationProvider: retry => TimeSpan.FromSeconds(Math.Pow(2, retry)),
                onRetry: (ex, timeSpan, retry, _) => logger.LogWarning(ex, "Insert chunk transient failure (attempt {Retry}), retrying in {Delay}s", retry, timeSpan.TotalSeconds))
            .ExecuteAsync(async () =>
            {
                using var connection = (SqlConnection)await factory.CreateConnectionAsync();
                await connection.OpenAsync();
                using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    var sql = BuildInsertSql(tableName, columns, chunk.Count);
                    var parameters = BuildParameters(chunk, props);

                    await connection.ExecuteAsync(sql, parameters, transaction: transaction, commandTimeout: 300);
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Bulk insert failed for table {Table}, rolling back transaction, Exception: {ExectptionMessage}", tableName, ex.Message);
                    if (connection.State == ConnectionState.Open)
                    {
                        await transaction.RollbackAsync();
                    }
                    SqlConnection.ClearPool(connection);
                    throw;
                }
            });
    }

    private static string BuildInsertSql(string tableName, List<string> columns, int rowCount)
    {
        var sb = new StringBuilder();
        sb.Append($"INSERT INTO {tableName} (");
        sb.Append(string.Join(", ", columns.Select(c => $"[{c}]")));
        sb.Append(") VALUES ");

        var rows = new List<string>();
        for (int i = 0; i < rowCount; i++)
        {
            var paramNames = columns.Select(c => $"@{c}_{i}");
            rows.Add($"({string.Join(", ", paramNames)})");
        }

        sb.Append(string.Join(", ", rows));
        return sb.ToString();
    }

    private static DynamicParameters BuildParameters<T>(List<T> chunk, List<PropertyInfo> props)
    {
        var parameters = new DynamicParameters();
        for (int i = 0; i < chunk.Count; i++)
        {
            foreach (var prop in props)
            {
                parameters.Add($"{prop.Name}_{i}", prop.GetValue(chunk[i]));
            }
        }
        return parameters;
    }

    /// <summary>
    /// Returns true if the property represents a database-generated identity column that
    /// must not be included in INSERT statements.
    /// Detects three common patterns:
    ///   1. Explicit [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    ///   2. [Key] on a numeric type (EF auto-increment convention)
    ///   3. Property named "Id" with a numeric type (bare POCO convention)
    /// </summary>
    private static bool IsIdentityProperty(PropertyInfo p)
    {
        // Explicit attribute always wins
        var dbGenAttr = p.GetCustomAttribute<DatabaseGeneratedAttribute>();
        if (dbGenAttr?.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity)
        {
            return true;
        }

        // [Key] on a numeric type — EF treats this as an identity column by convention
        var isNumeric = p.PropertyType == typeof(int) || p.PropertyType == typeof(long) || p.PropertyType == typeof(short) || p.PropertyType == typeof(byte);
        if (isNumeric && p.GetCustomAttribute<KeyAttribute>() != null)
        {
            return true;
        }

        // Bare POCO convention: property literally named "Id" with a numeric type
        if (isNumeric && string.Equals(p.Name, "Id", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
