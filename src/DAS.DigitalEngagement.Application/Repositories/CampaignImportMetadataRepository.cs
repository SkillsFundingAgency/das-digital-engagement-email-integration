using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DAS.DigitalEngagement.CampaignInterest.Data.Models;
using DAS.DigitalEngagement.Application.Repositories.Interfaces;

namespace DAS.DigitalEngagement.CampaignInterest.Data.Repositories
{
    public class CampaignImportMetadataRepository : ICampaignImportMetadataRepository
    {
        private readonly ILogger<CampaignImportMetadataRepository> _logger;
        private readonly string _connectionString;

        public CampaignImportMetadataRepository(
            ILogger<CampaignImportMetadataRepository> logger,
            IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            _connectionString = configuration.GetSection("ConnectionString")["CampaignsDatabase"]
                    ?? throw new InvalidOperationException("Connection string 'CampaignsDatabase' not found.");
        }

        public async Task<IEnumerable<CampaignImportMetadata>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Retrieving campaign import metadata");

            try
            {
                var results = new List<CampaignImportMetadata>();

                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                var schemaCandidate = "import";
                var tableName = "CampaignImportMetadata";

                var existsInImport = await TableExistsAsync(connection, schemaCandidate, tableName, cancellationToken).ConfigureAwait(false);
                string qualifiedTableName;
                if (existsInImport)
                {
                    qualifiedTableName = $"{schemaCandidate}.{tableName}";
                }
                else
                {
                    var fallbackSchema = "dbo";
                    var existsInFallback = await TableExistsAsync(connection, fallbackSchema, tableName, cancellationToken).ConfigureAwait(false);
                    if (existsInFallback)
                    {
                        qualifiedTableName = $"{fallbackSchema}.{tableName}";
                        _logger.LogWarning("Table import.{Table} not found; falling back to {Schema}.{Table}", tableName, fallbackSchema, tableName);
                    }
                    else
                    {
                        _logger.LogError("Table import.{Table} not found and no fallback table {FallbackSchema}.{Table} exists. Aborting read.", tableName, fallbackSchema, tableName);
                        return Array.Empty<CampaignImportMetadata>();
                    }
                }

                await using var command = connection.CreateCommand();
                command.CommandText = $"SELECT * FROM {qualifiedTableName}";
                command.CommandType = CommandType.Text;

                await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

                if (!reader.HasRows)
                {
                    _logger.LogInformation("No campaign import metadata found");
                    return Array.Empty<CampaignImportMetadata>();
                }

                var modelType = typeof(CampaignImportMetadata);
                var props = modelType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                     .Where(p => p.CanWrite).ToArray();

                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    var model = Activator.CreateInstance<CampaignImportMetadata>();

                    foreach (var prop in props)
                    {
                        if (!ColumnExists(reader, prop.Name)) continue;

                        var value = reader[prop.Name];
                        if (value == DBNull.Value)
                        {
                            prop.SetValue(model, null);
                            continue;
                        }

                        var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                        try
                        {
                            var safeValue = Convert.ChangeType(value, targetType);
                            prop.SetValue(model, safeValue);
                        }
                        catch
                        {
                            if (targetType.IsAssignableFrom(value.GetType()))
                            {
                                prop.SetValue(model, value);
                            }
                        }
                    }

                    results.Add(model);
                }

                _logger.LogInformation("Retrieved {Count} campaign import metadata entries", results.Count);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving campaign import metadata");
                return Array.Empty<CampaignImportMetadata>();
            }
        }

        private static bool ColumnExists(IDataRecord reader, string columnName)
        {
            try
            {
                return reader.GetOrdinal(columnName) >= 0;
            }
            catch (IndexOutOfRangeException)
            {
                return false;
            }
        }

        private static async Task<bool> TableExistsAsync(SqlConnection connection, string schema, string tableName, CancellationToken cancellationToken)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = @"SELECT COUNT(1)
                                FROM INFORMATION_SCHEMA.TABLES
                                WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @tableName";
            var schemaParam = new SqlParameter("@schema", SqlDbType.NVarChar, 128) { Value = schema };
            var tableParam = new SqlParameter("@tableName", SqlDbType.NVarChar, 128) { Value = tableName };
            cmd.Parameters.Add(schemaParam);
            cmd.Parameters.Add(tableParam);

            var result = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            if (result == null || result == DBNull.Value) return false;

            try
            {
                return Convert.ToInt32(result) > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}