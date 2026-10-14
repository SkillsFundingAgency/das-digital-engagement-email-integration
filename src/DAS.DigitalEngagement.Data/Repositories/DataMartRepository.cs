using Azure.Core;
using DAS.DigitalEngagement.CampaignInterest.Data.Repositories.Interfaces;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Data.Common;
using System.Dynamic;

namespace DAS.DigitalEngagement.CampaignInterest.Data.Repositories;

public sealed class DataMartRepository : IDataMartRepository
{
    private static readonly string[] SqlScopes = ["https://database.windows.net/.default"];
    private readonly TokenCredential _tokenCredential;
    private readonly string _connectionString;
    private readonly ILogger<DataMartRepository> _logger;
    private readonly Func<DbConnection> _connectionFactory;

    public DataMartRepository(
        TokenCredential tokenCredential,
        IOptions<ConnectionString> connectionStrings,
        ILogger<DataMartRepository> logger,
        Func<DbConnection>? connectionFactory = null)
    {
        _tokenCredential = tokenCredential;
        _connectionString = connectionStrings.Value.DataMart ?? string.Empty;
        _logger = logger;
        _connectionFactory = connectionFactory ?? (() => new SqlConnection(_connectionString));
    }

    public async Task<IList<dynamic>> RetrieveEmployeeRegistrationData()
    {
        var results = new List<dynamic>();
        var accessToken = await _tokenCredential.GetTokenAsync(new TokenRequestContext(SqlScopes), CancellationToken.None);

        await using var connection = _connectionFactory();
        if (connection is SqlConnection sqlConnection)
        {
            sqlConnection.AccessToken = accessToken.Token;
        }

        await connection.OpenAsync(CancellationToken.None);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM [ASData_PL].[vw_DAS_EmailIntegration] ORDER BY EMAIL";
        command.CommandTimeout = 120;

        await using var reader = await command.ExecuteReaderAsync(CancellationToken.None);
        while (await reader.ReadAsync())
        {
            IDictionary<string, object?> row = new ExpandoObject();
            for (var index = 0; index < reader.FieldCount; index++)
            {
                row[reader.GetName(index)] = await reader.IsDBNullAsync(index) ? null : reader.GetValue(index);
            }

            results.Add((ExpandoObject)row);
        }

        _logger.LogInformation("Retrieved {Count} employee registration records", results.Count);
        return results;
    }
}
