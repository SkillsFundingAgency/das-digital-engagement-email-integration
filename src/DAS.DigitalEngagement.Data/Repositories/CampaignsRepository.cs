using Dapper;
using DAS.DigitalEngagement.CampaignInterest.Data.Helpers;
using DAS.DigitalEngagement.CampaignInterest.Data.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace DAS.DigitalEngagement.CampaignInterest.Data.Repositories;

public interface ICampaignsRepository
{
    Task<long> UpsertAsync(Campaigns campaign);
    Task<Campaigns?> GetByIdAsync(long id);
    Task<IEnumerable<Campaigns>> GetAllAsync();
    Task<IEnumerable<Campaigns>> GetByIdsAsync(IEnumerable<long> ids);
}

[ExcludeFromCodeCoverage]
public class CampaignsRepository(IDbConnectionFactory factory, ILogger<CampaignsRepository> logger) : ICampaignsRepository
{
    public async Task<long> UpsertAsync(Campaigns campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);

        const string storedProcedure = "dbo.Usp_Campaigns_Upsert";
        logger.LogInformation("Upserting Campaign with CampaignId {CampaignId} using stored procedure {StoredProcedure}", campaign.Id, storedProcedure);

        using var connection = (SqlConnection)await factory.CreateConnectionAsync();
        await connection.OpenAsync();

        var campaignId = await connection.ExecuteScalarAsync<long>(storedProcedure, new
        {
            campaign.ExternalCampaignId,
            campaign.ExternalSendId,
            campaign.CampaignName,
            campaign.SendName,
            campaign.Type,
            campaign.CreatedBy,
            campaign.CreatedOn,
            campaign.ModifiedBy,
            campaign.ModifiedOn,
            campaign.FirstSendDate,
            campaign.LastSendDate,
            campaign.FromEmailAddress,
            campaign.FromName,
            campaign.ReplyEmailAddress,
            campaign.Subject,
            campaign.SubStatus,
            campaign.ContactCount,
            campaign.Account
        }, commandType: CommandType.StoredProcedure);

        await connection.CloseAsync();
        logger.LogInformation("Upserted Campaign with CampaignId {CampaignId}", campaignId);
        return campaignId;
    }

    public async Task<Campaigns?> GetByIdAsync(long id)
    {
        const string storedProcedure = "dbo.Usp_Campaigns_Get";
        logger.LogInformation("Fetching Campaign by Id {Id} using stored procedure {StoredProcedure}", id, storedProcedure);

        using var connection = (SqlConnection)await factory.CreateConnectionAsync();
        await connection.OpenAsync();

        var result = await connection.QuerySingleOrDefaultAsync<Campaigns>(storedProcedure, new { Id = id.ToString() }, commandType: CommandType.StoredProcedure);

        await connection.CloseAsync();
        return result;
    }

    public async Task<IEnumerable<Campaigns>> GetAllAsync()
    {
        const string storedProcedure = "dbo.Usp_Campaigns_Get";
        logger.LogInformation("Fetching all Campaigns using stored procedure {StoredProcedure}", storedProcedure);

        using var connection = (SqlConnection)await factory.CreateConnectionAsync();
        await connection.OpenAsync();

        var result = await connection.QueryAsync<Campaigns>(storedProcedure, commandType: CommandType.StoredProcedure);

        await connection.CloseAsync();
        return result;
    }

    public async Task<IEnumerable<Campaigns>> GetByIdsAsync(IEnumerable<long> ids)
    {
        const string storedProcedure = "dbo.Usp_Campaigns_Get";
        string idList = string.Join(",", ids);

        logger.LogInformation("Fetching {Count} Campaigns by Ids using stored procedure {StoredProcedure}", ids.Count(), storedProcedure);

        using var connection = (SqlConnection)await factory.CreateConnectionAsync();
        await connection.OpenAsync();

        var result = await connection.QueryAsync<Campaigns>(storedProcedure, new { Ids = idList }, commandType: CommandType.StoredProcedure);

        await connection.CloseAsync();
        return result;
    }
}
