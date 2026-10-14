using DAS.DigitalEngagement.CampaignInterest.Data.Models;
using DAS.DigitalEngagement.CampaignInterest.Data.Repositories.Interfaces;
using DAS.DigitalEngagement.CampaignInterest.Data.Helpers;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace DAS.DigitalEngagement.CampaignInterest.Data.Repositories;

public sealed class CampaignImportMetadataRepository(
	ILogger<CampaignImportMetadataRepository> logger,
	ISqlConnectionFactory connectionFactory) : ICampaignImportMetadataRepository
{
	private const string Query = """
		SELECT Id, SendID, CampaignID, IsImportComplete, ImportStartDate, ImportEndDate
		FROM import.CampaignImportMetadata;
		""";

	public async Task<IEnumerable<CampaignImportMetadata>> GetAllAsync(CancellationToken cancellationToken = default)
	{
		var results = new List<CampaignImportMetadata>();
		await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
		await connection.OpenAsync(cancellationToken);
		await using var command = new SqlCommand(Query, connection);
		await using var reader = await command.ExecuteReaderAsync(cancellationToken);

		while (await reader.ReadAsync(cancellationToken))
		{
			results.Add(new CampaignImportMetadata
			{
				Id = reader.GetInt32(reader.GetOrdinal("Id")),
				SendId = reader.GetInt32(reader.GetOrdinal("SendID")),
				CampaignId = reader.IsDBNull(reader.GetOrdinal("CampaignID")) ? null : reader.GetInt32(reader.GetOrdinal("CampaignID")),
				IsImportComplete = reader.GetBoolean(reader.GetOrdinal("IsImportComplete")),
				ImportStartDate = reader.GetDateTime(reader.GetOrdinal("ImportStartDate")),
				ImportEndDate = reader.IsDBNull(reader.GetOrdinal("ImportEndDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ImportEndDate"))
			});
		}

		logger.LogInformation("Retrieved {Count} campaign import metadata entries", results.Count);
		return results;
	}
}
