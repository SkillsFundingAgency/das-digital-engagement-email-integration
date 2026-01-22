using Azure.Core;
using Azure.Identity;
using DAS.DigitalEngagement.Application.Repositories.Interfaces;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DAS.DigitalEngagement.Application.Repositories
{
    public class DataMartRepository : IDataMartRepository
    {

        private readonly string _connectionString;
        private readonly TokenCredential _tokenCredential;
        private readonly ILogger<DataMartRepository> _logger;
        private readonly Func<DbConnection> _connectionFactory;

        private static readonly string[] SqlScopes = { "https://database.windows.net/.default"};


        public DataMartRepository(
            TokenCredential tokenCredential,
            IOptions<ConnectionString> connectionStrings,
            ILogger<DataMartRepository> logger,
            Func<DbConnection>? connectionFactory = null)
        {
            _connectionString = connectionStrings.Value.DataMart ?? "";
            _tokenCredential = tokenCredential;
            _logger = logger;

            // Default factory creates a SqlConnection using the configured connection string.
            _connectionFactory = connectionFactory ?? (() => new SqlConnection(_connectionString));
        }


        public async Task<IList<dynamic>> RetrieveEmployeeRegistrationData(string? viewName)
        {
            if (string.IsNullOrWhiteSpace(viewName))
                throw new ArgumentException("View name cannot be empty.", nameof(viewName));

            var results = new List<dynamic>();

            // Acquire Azure AD token for Azure SQL (use .default scope)
            var tokenRequest = new TokenRequestContext(SqlScopes);
            var accessToken = await _tokenCredential.GetTokenAsync(tokenRequest, CancellationToken.None);

            await using (var conn = _connectionFactory())
            {
                // If the concrete connection is SqlConnection, set the AccessToken for AAD auth.
                if (conn is SqlConnection sqlConn)
                {
                    sqlConn.AccessToken = accessToken.Token;
                }

                await conn.OpenAsync(CancellationToken.None);
                _logger.LogInformation("Database connection opened successfully.");

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $"SELECT TOP 2 * FROM [ASData_PL].[vw_DAS_EmailIntegration]";

                    using (var reader = await cmd.ExecuteReaderAsync(CancellationToken.None))
                    {
                        while (await reader.ReadAsync())
                        {
                            _logger.LogInformation("Reading a new row from the result set.");

                            IDictionary<string, object?> row = new ExpandoObject();

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                object? value = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
                                row[reader.GetName(i)] = value;
                            }

                            results.Add((ExpandoObject)row);
                            _logger.LogInformation("Retrieved row with data: {RowData}", string.Join(", ", row.Select(kv => $"{kv.Key}={kv.Value}")));
                        }
                    }
                }
            }

            return results;
        }

    }
}
