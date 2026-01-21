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
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace DAS.DigitalEngagement.Application.Repositories
{
    public class DataMartRepository : IDataMartRepository
    {

        private readonly string _connectionString;
        private readonly TokenCredential _tokenCredential;
        private readonly ILogger<DataMartRepository> _logger;

        public DataMartRepository(
            TokenCredential tokenCredential,
            IOptions<ConnectionString> connectionStrings,
            ILogger<DataMartRepository> logger)
        {
            _connectionString = connectionStrings.Value.DataMart ?? "";
            _tokenCredential = tokenCredential;
            _logger = logger;
        }


        public async Task<IList<dynamic>> RetrieveEmployeeRegistrationData(string? viewName)
        {
            if (string.IsNullOrWhiteSpace(viewName))
                throw new ArgumentException("View name cannot be empty.", nameof(viewName));

         

            var results = new List<dynamic>();

            // Acquire Azure AD token for Azure SQL (use .default scope)
            var tokenRequest = new TokenRequestContext(new[] { "https://database.windows.net/.default" });
            var accessToken = await _tokenCredential.GetTokenAsync(tokenRequest, CancellationToken.None);

            await using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("SELECT TOP 10 * FROM [ASData_PL].[vw_DAS_EmailIntegration], conn"))
            {
                // Assign AAD access token (no User ID/Password in connection string)
                conn.AccessToken = accessToken.Token;
                await conn.OpenAsync();
                _logger.LogInformation("Database connection opened successfully.");

                using (var reader = await cmd.ExecuteReaderAsync())
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

            return results;
        }

    }
}
