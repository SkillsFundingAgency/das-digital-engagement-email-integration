using DAS.DigitalEngagement.Application.Repositories.Interfaces;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.CampaignInterest.Data.Models;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Data;

namespace DAS.DigitalEngagement.Application.Services;

public class CampaignStagingService(IExternalApiService externalApiService,
    ICampaignImportMetadataRepository campaignImportMetadataRepository,
    ISqlBulkInserter sqlBulkInserter,
    IJsonToDataTableConverter jsonToDataTableConverter,
    ILogger<CampaignStagingService> logger, IOptions<EmailMarketingApi> apiConfig)
    : ICampaignStagingService
{
    private readonly int _pageSize = apiConfig.Value.PageSize;
    private readonly int _importWindowDays = apiConfig.Value.ImportWindowDays;
    private const string ContactProperty = "Contact";
    private const string EmailProperty = "Email";

    public async Task<DataTable?> GetAllSendsFromEShot(int? subAccountId = null, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Retrieving Sends for sub-account {SubAccountId}", subAccountId);

        var endpoint = $"Sends?$select=ID,SendCompletedDate";

        if (subAccountId != null)
        {
            endpoint += $"&$filter={Uri.EscapeDataString($"SubAccountID eq {subAccountId}")}";
        }

        var response = await externalApiService.GetDataAsync(endpoint);

        var sends = jsonToDataTableConverter.ConvertODataPageToDataTable(response);

        logger.LogInformation("Retrieved {SendCount} Sends for sub-account {SubAccountId}", sends?.Rows.Count, subAccountId);

        return sends;
    }

    public async Task<DataTable> GetEligibleSendsAsync(int? subAccountId = null, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Determining eligible sends within {ImportWindowDays} days", _importWindowDays);

        var allSends = await GetAllSendsFromEShot(subAccountId, cancellationToken);

        if (allSends == null || allSends.Rows.Count == 0)
        {
            logger.LogWarning("No sends returned from e-shot API");
            return new DataTable();
        }

        var importedMetadata = await GetAllCampaignImportMetadataAsync(cancellationToken);
        var completedSendIds = new HashSet<int>(importedMetadata.Where(m => m.IsImportComplete).Select(m => m.SendId));

        var cutoffDate = DateTime.UtcNow.AddDays(-_importWindowDays);

        var eligibleSendsTable = allSends.Clone();

        foreach (DataRow send in allSends.Rows)
        {
            if (allSends.Columns.Contains("ID") && allSends.Columns.Contains("SendCompletedDate"))
            {
                var sendID = send["ID"];
                var sendCompletedDateObj = send["SendCompletedDate"];

                if (sendID == null || sendCompletedDateObj == null)
                    continue;

                if (!int.TryParse(sendID.ToString(), out var id))
                    continue;

                if (completedSendIds.Contains(id))
                    continue;

                if (!DateTime.TryParse(sendCompletedDateObj.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var sendCompletedDate))
                {
                    logger.LogWarning("Unable to parse SendCompletedDate '{SendCompletedDate}' for Send {SendId}; skipping", sendCompletedDateObj, sendID);
                    continue;
                }

                if (sendCompletedDate <= cutoffDate)
                {
                    eligibleSendsTable.ImportRow(send);
                }
            }
        }

        logger.LogInformation("Determined {EligibleCount} eligible sends out of {TotalCount} total sends", eligibleSendsTable.Rows.Count, allSends.Rows.Count);

        return eligibleSendsTable;
    }

    public async Task<IEnumerable<CampaignImportMetadata>> GetAllCampaignImportMetadataAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = await campaignImportMetadataRepository.GetAllAsync(cancellationToken);

            if (metadata == null || !metadata.Any())
            {
                logger.LogWarning("No campaign import metadata found in database");
                return Array.Empty<CampaignImportMetadata>();
            }

            logger.LogInformation("Retrieved {Count} campaign import metadata entries", metadata.Count());
            return metadata;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving campaign import metadata");
            return Array.Empty<CampaignImportMetadata>();
        }
    }

    public async Task<DataSet?> GetSendsAndCampaign(List<long> sendIds, CancellationToken cancellationToken)
    {
        var endpoint = $"Sends?$expand=Campaign&$filter=ID in ({string.Join(",", sendIds)})";

        var response = await externalApiService.GetDataAsync(endpoint);

        var dsSendCampaign = jsonToDataTableConverter.ConvertODataPageToDataSet(response);

        logger.LogInformation("Retrieved {SendCount} Sends with campaign data", dsSendCampaign?.Tables[0].Rows.Count ?? 0);

        return dsSendCampaign;
    }

    public DataTable PrepareImportMetaData(DataTable value)
    {
        var importMetaDatat = new DataTable();
        importMetaDatat.Columns.Add("SendID", typeof(int));
        importMetaDatat.Columns.Add("CampaignID", typeof(long));
        importMetaDatat.Columns.Add("IsImportComplete", typeof(bool));
        importMetaDatat.Columns.Add("ImportStartDate", typeof(DateTime));
        importMetaDatat.Columns.Add("ImportEndDate", typeof(DateTime));

        if (value == null || value.Rows.Count == 0)
        {
            return importMetaDatat;
        }

        var sendColumn = "ID";
        var campaignColumn = "CampaignID";

        foreach (DataRow srcRow in value.Rows)
        {
            var newRow = importMetaDatat.NewRow();

            if (sendColumn != null && srcRow[sendColumn] != DBNull.Value && !string.IsNullOrWhiteSpace(srcRow[sendColumn]?.ToString()))
            {
                if (int.TryParse(srcRow[sendColumn].ToString(), out var sendId))
                {
                    newRow["SendID"] = sendId;
                }
                else
                {
                    newRow["SendID"] = DBNull.Value;
                }
            }
            else
            {
                newRow["SendID"] = DBNull.Value;
            }

            if (campaignColumn != null && srcRow[campaignColumn] != DBNull.Value && !string.IsNullOrWhiteSpace(srcRow[campaignColumn]?.ToString()))
            {
                if (long.TryParse(srcRow[campaignColumn].ToString(), out var campaignId))
                {
                    newRow["CampaignID"] = campaignId;
                }
                else
                {
                    newRow["CampaignID"] = DBNull.Value;
                }
            }
            else
            {
                newRow["CampaignID"] = DBNull.Value;
            }

            newRow["IsImportComplete"] = false;
            newRow["ImportStartDate"] = DateTime.UtcNow;
            newRow["ImportEndDate"] = DBNull.Value;

            importMetaDatat.Rows.Add(newRow);
        }

        return importMetaDatat;
    }

    public DataTable UpdateImportMetaData(DataTable importMetaDatat)
    {
        try
        {
            if (importMetaDatat == null)
            {
                logger.LogWarning("UpdateImportMetaData called with null DataTable");
                return new DataTable();
            }

            if (importMetaDatat.Rows.Count == 0)
            {
                logger.LogInformation("UpdateImportMetaData called with empty DataTable");
                return importMetaDatat;
            }

            if (!importMetaDatat.Columns.Contains("IsImportComplete"))
            {
                var col = importMetaDatat.Columns.Add("IsImportComplete", typeof(bool));
                col.AllowDBNull = false;
                col.DefaultValue = false;
            }

            if (!importMetaDatat.Columns.Contains("ImportEndDate"))
            {
                var col = importMetaDatat.Columns.Add("ImportEndDate", typeof(DateTime));
                col.AllowDBNull = true;
            }

            var now = DateTime.UtcNow;
            var updatedCount = 0;

            foreach (DataRow row in importMetaDatat.Rows)
            {
                try
                {
                    row["IsImportComplete"] = true;
                    row["ImportEndDate"] = now;
                    updatedCount++;
                }
                catch (Exception rowEx)
                {
                    logger.LogWarning(rowEx, "Failed to update import metadata for a row; continuing");
                }
            }

            logger.LogInformation("Updated {UpdatedCount} rows setting IsImportComplete=true and ImportEndDate={ImportEndDate}", updatedCount, now);

            return importMetaDatat;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating import metadata");
            return importMetaDatat ?? new DataTable();
        }
    }

    public async Task<int> BulkInsertCampaignAsync(DataTable campaign, CancellationToken cancellationToken)
    {
        var destinationTable = "import.Campaigns";

        await sqlBulkInserter.BulkInsertAsync(
            destinationTable,
            campaign,
            cancellationToken: cancellationToken
        );

        return campaign.Rows.Count;
    }

    public async Task<int> BulkInsertSendsAsync(DataTable sends, CancellationToken cancellationToken = default)
    {
        var destinationTable = "import.Sends";

        await sqlBulkInserter.BulkInsertAsync(
            destinationTable,
            sends,
            cancellationToken: cancellationToken
        );

        return sends.Rows.Count;
    }

    public async Task<int> BulkInsertCampaignImportMetadataAsync(DataTable data, CancellationToken cancellationToken = default)
    {
        var destinationTable = "import.CampaignImportMetadata";

        await sqlBulkInserter.BulkInsertAsync(
            destinationTable,
            data,
            cancellationToken: cancellationToken
        );

        return data.Rows.Count;
    }

    private static string BuildDisplayedContactsEndpoint(int sendId, int skip, int top)
    {
        var filter = Uri.EscapeDataString($"SendID eq {sendId}");
        var expand = Uri.EscapeDataString("Contact($select=Email)");
        var orderBy = Uri.EscapeDataString("ID");

        return $"DisplayedContacts?$expand={expand}&$filter={filter}&$orderby={orderBy}&$skip={skip}&$top={top}";
    }

    private static string BuildClickedLinkContactsEndpoint(int sendId, int skip, int top)
    {
        var filter = Uri.EscapeDataString($"SendID eq {sendId}");
        var expand = Uri.EscapeDataString("Contact($select=Email), Link($select=URL,IsMonitored,ReceivedInMessageFormat)");
        var orderBy = Uri.EscapeDataString("ID");

        return $"ClickedContacts?$expand={expand}&$filter={filter}&$orderby={orderBy}&$skip={skip}&$top={top}";
    }

    private static string BuildBouncedContactsEndpoint(long sendId, int skip, int top)
    {
        var filter = Uri.EscapeDataString($"SendID eq {sendId}");
        var select = Uri.EscapeDataString("ID,BounceReason,BounceType,BounceDate,SendID,CampaignID,ResponseText");
        var expand = Uri.EscapeDataString("Contact($select=Email)");
        var orderBy = Uri.EscapeDataString("ID");

        return $"BouncedContacts?$select={select}&$expand={expand}&$filter={filter}&$orderby={orderBy}&$skip={skip}&$top={top}";
    }

    private static string BuildUnsubscribedContactsEndpoint(int sendId, int skip, int top)
    {
        var filter = Uri.EscapeDataString($"SendID eq {sendId}");
        var select = Uri.EscapeDataString("ID,UnsubscribeDate,SendID,CampaignID,IsGlobalUnsubscribe,IsComplaint");
        var expand = Uri.EscapeDataString("Contact($select=Email)");
        var orderBy = Uri.EscapeDataString("ID");

        return $"UnsubscribedContacts?$expand={expand}&$select={select}&$filter={filter}&$orderby={orderBy}&$skip={skip}&$top={top}";
    }

    private static string BuildUserAgentEndpoint(int sendId, int skip, int top)
    {
        var filter = Uri.EscapeDataString($"SendID eq {sendId}");
        var orderBy = Uri.EscapeDataString("ID");

        return $"UserAgents?$filter={filter}&$orderby={orderBy}&$skip={skip}&$top={top}";
    }

    private static List<T> DeserializeODataValueArray<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<T>();

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("value", out var value) && value.ValueKind == JsonValueKind.Array)
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString
                };

                var list = JsonSerializer.Deserialize<List<T>>(value.GetRawText(), options);
                return list ?? new List<T>();
            }

            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString
                };

                var list = JsonSerializer.Deserialize<List<T>>(doc.RootElement.GetRawText(), options);
                return list ?? new List<T>();
            }
        }
        catch (JsonException)
        {
        }

        return new List<T>();
    }

    private static JsonArray? GetValueArrayFromJsonResponse(string jsonResponse)
    {
        var jsonNode = JsonNode.Parse(jsonResponse);
        var valueArray = jsonNode?["value"]?.AsArray();
        return valueArray;
    }
}