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
    Task<CampaignImportMetadata?> GetByIdAsync(int sendId);
    Task<IEnumerable<CampaignImportMetadata>> GetAllAsync();
    Task<IEnumerable<CampaignImportMetadata>> GetByIdsAsync(IEnumerable<int> sendIds);
    Task<int> UpsertAsync(CampaignImportMetadata campaignImportMetadata);
}

[ExcludeFromCodeCoverage]
public class CampaignImportMetadataRepository(IDbConnectionFactory factory, ILogger<CampaignImportMetadataRepository> logger) : ICampaignImportMetadataRepository
{
    /// <summary>
    /// Fetches the CampaignImportMetadata for a specific sendId using the stored procedure dbo.Usp_CampaignImportMetadata_Get.
    /// </summary>
    /// <param name="sendId">The ID of the send for which to fetch the metadata.</param>
    /// <returns>The CampaignImportMetadata for the specified sendId, or null if not found.</returns>
    public async Task<CampaignImportMetadata?> GetByIdAsync(int sendId)
    {
        const string storedProcedure = "dbo.Usp_CampaignImportMetadata_Get";

        logger.LogInformation("Fetching CampaignImportMetadata by SendId {SendId} using stored procedure {StoredProcedure}", sendId, storedProcedure);

        using var connection = (SqlConnection)await factory.CreateConnectionAsync();
        await connection.OpenAsync();

        var result = await connection.QuerySingleOrDefaultAsync<CampaignImportMetadata>(storedProcedure, new { SendIds = sendId.ToString() }, commandType: CommandType.StoredProcedure);

        await connection.CloseAsync();
        return result;
    }

    public async Task<IEnumerable<CampaignImportMetadata>> GetAllAsync()
    {
        const string storedProcedure = "dbo.Usp_CampaignImportMetadata_Get";

        logger.LogInformation("Fetching all CampaignImportMetadata using stored procedure {StoredProcedure}", storedProcedure);

        using var connection = (SqlConnection)await factory.CreateConnectionAsync();
        await connection.OpenAsync();

        var result = await connection.QueryAsync<CampaignImportMetadata>(
            storedProcedure, 
            commandType: CommandType.StoredProcedure);

        await connection.CloseAsync();
        return result;
    }

    public async Task<IEnumerable<CampaignImportMetadata>> GetByIdsAsync(IEnumerable<int> sendIds)
    {
        const string storedProcedure = "dbo.Usp_CampaignImportMetadata_Get";
        string idList = string.Join(",", sendIds);

        logger.LogInformation("Fetching {Count} CampaignImportMetadata by sendIds using stored procedure {StoredProcedure}", sendIds.Count(), storedProcedure);

        using var connection = (SqlConnection)await factory.CreateConnectionAsync();
        await connection.OpenAsync();

        var result = await connection.QueryAsync<CampaignImportMetadata>(storedProcedure, new { SendIds = idList }, commandType: CommandType.StoredProcedure);

        await connection.CloseAsync();
        return result;
    }

    public async Task<int> UpsertAsync(CampaignImportMetadata campaignImportMetadata)
    {
        ArgumentNullException.ThrowIfNull(campaignImportMetadata);

        const string storedProcedure = "dbo.Usp_CampaignImportMetadata_Upsert";

        logger.LogInformation("Upserting CampaignImportMetadata for SendId {SendId} using stored procedure {StoredProcedure}", campaignImportMetadata.SendId, storedProcedure);

        using var connection = (SqlConnection)await factory.CreateConnectionAsync();
        await connection.OpenAsync();

        var rowId = await connection.ExecuteAsync(storedProcedure, new
        {
            campaignImportMetadata.SendId,
            campaignImportMetadata.CampaignId,
            campaignImportMetadata.IsImportComplete,
            campaignImportMetadata.ImportStartDate,
            campaignImportMetadata.ImportEndDate
        }, commandType: CommandType.StoredProcedure);

        await connection.CloseAsync();

        logger.LogInformation("Upserted CampaignImportMetadata for SendId {SendId}, Campaign Import Metadata Id: {RowsId}", campaignImportMetadata.SendId, rowId);

        return rowId;
    }
}
