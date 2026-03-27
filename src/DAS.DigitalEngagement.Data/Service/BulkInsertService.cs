using DAS.DigitalEngagement.CampaignInterest.Data.Helpers;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Polly;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace DAS.DigitalEngagement.CampaignInterest.Data.Service;

public interface IBulkInsertService
{
    Task BulkInsertAsync<T>(IEnumerable<T> data, string tableName);
}

[ExcludeFromCodeCoverage]
public class BulkInsertService(IDbConnectionFactory factory, ILogger<BulkInsertService> logger) : IBulkInsertService
{
    public async Task BulkInsertAsync<T>(IEnumerable<T> data, string tableName)
    {
        var stopwatch = Stopwatch.StartNew();
        using var connection = (SqlConnection)factory.CreateConnection();
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var table = ConvertToDataTable(data);

            logger.LogInformation("Starting bulk insert into {Table} with {RowCount} rows", tableName, table.Rows.Count);

            // Apply SqlBulkCopyOptions for better performance
            var options = SqlBulkCopyOptions.TableLock | SqlBulkCopyOptions.KeepNulls;


            using var bulkCopy = new SqlBulkCopy(connection, options, (SqlTransaction)transaction)
            {
                DestinationTableName = tableName,
                BatchSize = 5000
            };

            // Map columns
            foreach (DataColumn col in table.Columns)
            {
                bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
            }

            // Add Retry Policy
            await Policy
                .Handle<SqlException>()
                .WaitAndRetryAsync(3, retry => TimeSpan.FromSeconds(2))
                .ExecuteAsync(() => bulkCopy.WriteToServerAsync(table));

            if (transaction != null)
            {
                logger.LogInformation("Committing transaction");
                await transaction.CommitAsync();
            }

            await connection.CloseAsync();
            stopwatch.Stop();

            logger.LogInformation("Completed bulk insert into {Table} in {ElapsedMs} ms", tableName, stopwatch.ElapsedMilliseconds);
        }
        catch (SqlException ex) when (ex.Number == 1205)
        {
            logger.LogWarning(ex, "Deadlock occurred during bulk insert into {Table}, rolling back transaction", tableName);
            await transaction.RollbackAsync();
            throw new InvalidOperationException($"Deadlock occurred during bulk insert into table '{tableName}'", ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Bulk insert failed for table {Table}, rolling back transaction", tableName);
            await transaction.RollbackAsync();
            throw new InvalidOperationException($"Bulk insert failed for table '{tableName}'", ex);
        }
    }

    private static DataTable ConvertToDataTable<T>(IEnumerable<T> data)
    {
        var table = new DataTable();
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Create columns
        foreach (var p in props)
        {
            var type = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
            table.Columns.Add(p.Name, type);
        }

        // Fill rows
        foreach (var item in data)
        {
            var values = props.Select(p => p.GetValue(item) ?? DBNull.Value).ToArray();
            table.Rows.Add(values);
        }

        return table;
    }
}
