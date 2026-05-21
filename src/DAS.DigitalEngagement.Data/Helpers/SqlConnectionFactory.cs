using Azure.Core;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DAS.DigitalEngagement.CampaignInterest.Data.Helpers;

public interface IDbConnectionFactory
{
    Task<IDbConnection> CreateConnectionAsync();
}

public class SqlConnectionFactory(string connectionString, TokenCredential? tokenCredential = null, int connectionTimeout = 300) : IDbConnectionFactory
{
    private static readonly string[] SqlScopes = ["https://database.windows.net/.default"];

    public async Task<IDbConnection> CreateConnectionAsync()
    {
        if (string.IsNullOrEmpty(connectionString) || string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string cannot be null or empty.");
        }

        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            ConnectTimeout = connectionTimeout
        };

        var connection = new SqlConnection(builder.ConnectionString);

        if (tokenCredential != null)
        {
            var token = await tokenCredential.GetTokenAsync(new TokenRequestContext(SqlScopes), default);
            connection.AccessToken = token.Token;
        }

        return connection;
    }
}
