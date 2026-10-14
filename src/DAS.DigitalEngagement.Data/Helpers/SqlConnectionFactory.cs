using Azure.Core;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DAS.DigitalEngagement.CampaignInterest.Data.Helpers;

public interface IDbConnectionFactory
{
    Task<IDbConnection> CreateConnectionAsync();
}

public interface ISqlConnectionFactory
{
    Task<SqlConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
}

public class SqlConnectionFactory : IDbConnectionFactory, ISqlConnectionFactory
{
    private static readonly string[] SqlScopes = ["https://database.windows.net/.default"];
    private readonly string _connectionString;
    private readonly TokenCredential? _tokenCredential;
    private readonly int _connectionTimeout;

    public SqlConnectionFactory(
        string connectionString,
        TokenCredential? tokenCredential = null,
        int connectionTimeout = 300)
    {
        _connectionString = connectionString;
        _tokenCredential = tokenCredential;
        _connectionTimeout = connectionTimeout;
    }

    public async Task<SqlConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("Connection string cannot be null or empty.");
        }

        var builder = new SqlConnectionStringBuilder(_connectionString)
        {
            ConnectTimeout = _connectionTimeout
        };

        var connection = new SqlConnection(builder.ConnectionString);
        if (_tokenCredential is not null)
        {
            var token = await _tokenCredential.GetTokenAsync(
                new TokenRequestContext(SqlScopes),
                cancellationToken);
            connection.AccessToken = token.Token;
        }

        return connection;
    }

    async Task<IDbConnection> IDbConnectionFactory.CreateConnectionAsync()
    {
        return await CreateConnectionAsync();
    }
}
