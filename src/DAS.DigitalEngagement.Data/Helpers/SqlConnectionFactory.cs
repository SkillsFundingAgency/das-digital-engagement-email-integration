using Azure.Core;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DAS.DigitalEngagement.CampaignInterest.Data.Helpers;

public interface IDbConnectionFactory
{
    Task<IDbConnection> CreateConnectionAsync();
}

public class SqlConnectionFactory(string connectionString, TokenCredential? tokenCredential = null) : IDbConnectionFactory
{
    private static readonly string[] SqlScopes = ["https://database.windows.net/.default"];

    public async Task<IDbConnection> CreateConnectionAsync()
    {
        if (string.IsNullOrEmpty(connectionString) || string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string cannot be null or empty.");
        }

        var connection = new SqlConnection(connectionString);

        if (tokenCredential != null)
        {
            var token = await tokenCredential.GetTokenAsync(new TokenRequestContext(SqlScopes), default);
            connection.AccessToken = token.Token;
        }

        return connection;
    }
}
