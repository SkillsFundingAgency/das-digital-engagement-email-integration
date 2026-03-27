using Microsoft.Data.SqlClient;
using System.Data;

namespace DAS.DigitalEngagement.CampaignInterest.Data.Helpers;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}

public class SqlConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public IDbConnection CreateConnection()
    {
        return new SqlConnection(connectionString);
    }
}
