
using Azure.Core;
using Azure.Identity;
using DAS.DigitalEngagement.Application.Repositories.Interfaces;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Data.SqlClient;
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

    public class DataMartRepository(IOptions<ConnectionString> connectionStrings) : IDataMartRepository
    {
        private readonly string _connectionString = connectionStrings.Value.DataMart ?? "";

        public async Task<IList<dynamic>> RetrieveEmployeeRegistrationData(string viewName)
        {

            string query = $"SELECT * FROM  [{viewName}]";

            var results = new List<dynamic>();

            // Acquire Azure AD token for Azure SQL
            var credential = new DefaultAzureCredential();
            var tokenRequest = new TokenRequestContext(new[] { "https://database.windows.net/.default" });
            var token = await credential.GetTokenAsync(tokenRequest);

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                // Assign AAD access token (no User ID/Password in connection string)
                conn.AccessToken = token.Token;
                await conn.OpenAsync();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        // Each row -> ExpandoObject (dynamic)
                        IDictionary<string, object?> row = new ExpandoObject();

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            object? value = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
                            row[reader.GetName(i)] = value;
                        }

                        results.Add((ExpandoObject)row);
                    }
                }
            }

            return results;


        }

        //public async Task<IList<dynamic>> RetrieveEmployeeRegistrationData(string viewName)
        //{

        //    if (string.IsNullOrWhiteSpace(viewName))
        //        throw new ArgumentException("View name cannot be empty.");

        //    var credential = new DefaultAzureCredential();

        //    // Get an access token for Azure SQL
        //    var token = await credential.GetTokenAsync(
        //        new TokenRequestContext(new[] { "https://database.windows.net/.default" }));

        //    await using var connection = new SqlConnection(_connectionString)
        //    {
        //        AccessToken = token.Token
        //    };

        //    await connection.OpenAsync();

        //    var sql = $"SELECT * FROM [{viewName}]"; // Validate viewName or whitelist it!
        //    await using var command = new SqlCommand(sql, connection);
        //    await using var reader = await command.ExecuteReaderAsync();

        //    var results = new List<dynamic>();

        //    while (await reader.ReadAsync())
        //    {
        //        dynamic row = new ExpandoObject();
        //        var dict = (IDictionary<string, object>)row;

        //        for (int i = 0; i < reader.FieldCount; i++)
        //        {
        //            dict[reader.GetName(i)] = reader.GetValue(i);
        //        }

        //        results.Add(row);
        //    }

        //    return results;

        //}


    }
}
