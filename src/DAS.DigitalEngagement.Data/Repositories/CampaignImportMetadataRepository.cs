using Dapper;
using DAS.DigitalEngagement.CampaignInterest.Data.Helpers;
using DAS.DigitalEngagement.CampaignInterest.Data.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace DAS.DigitalEngagement.CampaignInterest.Data.Repositories;

public interface ICampaignImportMetadataRepository
{
    Task<CampaignImportMetadata?> GetByIdAsync(long campaignId);
    Task<IEnumerable<CampaignImportMetadata>> GetAllAsync();
    Task<IEnumerable<CampaignImportMetadata>> GetByIdsAsync(IEnumerable<long> campaignIds);
    Task<int> UpsertAsync(CampaignImportMetadata campaignImportMetadata);
}

public class CampaignImportMetadataRepository(IDbConnectionFactory factory, ILogger<CampaignImportMetadataRepository> logger) : ICampaignImportMetadataRepository
{
    public async Task<CampaignImportMetadata?> GetByIdAsync(long campaignId)
    {
        const string storedProcedure = "dbo.Usp_CampaignImportMetadata_Get";

        logger.LogInformation("Fetching CampaignImportMetadata by CampaignId {CampaignId} using stored procedure {StoredProcedure}", 
            campaignId, storedProcedure);

        using var connection = (SqlConnection)factory.CreateConnection();
        await connection.OpenAsync();

        var result = await connection.QuerySingleOrDefaultAsync<CampaignImportMetadata>(
            storedProcedure, 
            new { Id = campaignId.ToString() }, 
            commandType: CommandType.StoredProcedure);

        await connection.CloseAsync();
        return result;
    }

    public async Task<IEnumerable<CampaignImportMetadata>> GetAllAsync()
    {
        const string storedProcedure = "dbo.Usp_CampaignImportMetadata_Get";

        logger.LogInformation("Fetching all CampaignImportMetadata using stored procedure {StoredProcedure}", storedProcedure);

        using var connection = (SqlConnection)factory.CreateConnection();
        await connection.OpenAsync();

        var result = await connection.QueryAsync<CampaignImportMetadata>(
            storedProcedure, 
            commandType: CommandType.StoredProcedure);

        await connection.CloseAsync();
        return result;
    }

    public async Task<IEnumerable<CampaignImportMetadata>> GetByIdsAsync(IEnumerable<long> campaignIds)
    {
        const string storedProcedure = "dbo.Usp_CampaignImportMetadata_Get";
        string idList = string.Join(",", campaignIds);

        logger.LogInformation("Fetching {Count} CampaignImportMetadata by campaignIds using stored procedure {StoredProcedure}", 
            campaignIds.Count(), storedProcedure);

        using var connection = (SqlConnection)factory.CreateConnection();
        await connection.OpenAsync();

        var result = await connection.QueryAsync<CampaignImportMetadata>(
            storedProcedure, 
            new { Ids = idList }, 
            commandType: CommandType.StoredProcedure);

        await connection.CloseAsync();
        return result;
    }

    [ExcludeFromCodeCoverage]
    public async Task<int> UpsertAsync(CampaignImportMetadata campaignImportMetadata)
    {
        ArgumentNullException.ThrowIfNull(campaignImportMetadata);

        const string storedProcedure = "dbo.Usp_CampaignImportMetadata_Upsert";

        logger.LogInformation("Upserting CampaignImportMetadata for CampaignId {CampaignId} using stored procedure {StoredProcedure}",
            campaignImportMetadata.CampaignId, storedProcedure);

        using var connection = (SqlConnection)factory.CreateConnection();
        await connection.OpenAsync();

        var rowsAffected = await connection.ExecuteAsync(storedProcedure, new
        {
            campaignImportMetadata.CampaignId,
            campaignImportMetadata.IsImportComplete,
            campaignImportMetadata.ImportStartDate,
            campaignImportMetadata.ImportEndDate
        }, commandType: CommandType.StoredProcedure);

        await connection.CloseAsync();

        logger.LogInformation("Upserted CampaignImportMetadata for CampaignId {CampaignId}, rows affected: {RowsAffected}",
            campaignImportMetadata.CampaignId, rowsAffected);

        return rowsAffected;
    }
}