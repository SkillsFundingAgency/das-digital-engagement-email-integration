using Azure;
using Azure.Core;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Data;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DAS.DigitalEngagement.Application.Services
{
    public sealed class ODataPagedImporter : IODataPagedImporter
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl;
        private readonly string _apiKey;
        private readonly IJsonToDataTableConverter _converter;
        private readonly ISqlBulkInserter _bulkInserter;
        private readonly ILogger<ODataPagedImporter> _logger;
        private readonly int _pageSize;
        private readonly string _connectionString;

        public ODataPagedImporter(HttpClient httpClient,
            IJsonToDataTableConverter converter,
            ISqlBulkInserter bulkInserter,
            ILogger<ODataPagedImporter> logger,
            IConfiguration configuration,
            int pageSize = 20)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _converter = converter ?? throw new ArgumentNullException(nameof(converter));
            _bulkInserter = bulkInserter ?? throw new ArgumentNullException(nameof(bulkInserter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pageSize = pageSize > 0 ? pageSize : throw new ArgumentOutOfRangeException(nameof(pageSize));

            // Connection string: prefer GetConnectionString, fall back to common keys
            _connectionString = configuration["ConnectionString:CampaignsDatabase"];

            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException("Connection string 'CampaignsDatabase' not found. Set 'ConnectionStrings:CampaignsDatabase' or use AddDbContext/Configure in configuration.");
            }

            // API base URL: try several common keys/sections
            _apiUrl = configuration["EmailMarketingApi:ApiBaseUrl"];


            // API key: try several common keys/sections
            _apiKey = configuration["EmailMarketingApi:ApiKey"];

            if (string.IsNullOrWhiteSpace(_apiUrl))
            {
                throw new InvalidOperationException("API base URL not found in configuration. Expected keys: 'ApiBaseUrl' or 'EmailMarketingApi:ApiBaseUrl'.");
            }

            // apiKey may be optional for some endpoints; don't throw if missing, but log
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _logger.LogWarning("API key not found in configuration. If the API requires authentication, set 'ApiKey' or 'EmailMarketingApi:ApiKey'.");
            }

            // ensure base url has no trailing slash to simplify concatenation later
            _apiUrl = _apiUrl.TrimEnd('/');

            // Optionally set base address on HttpClient (keeps callers flexible)
            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri(_apiUrl);
            }
        }



        public async Task<long> ImportEndpointToTableAsync(string endpointTemplate, string[] destinationTable, CancellationToken cancellationToken = default)

        {
            const int pageSize = 10;

            // Build manual paging endpoint (uses $top/$skip). We'll switch to server-provided @odata.nextLink if present.
            string BuildManualPageEndpoint(int currentSkip)
            {
                var sb = new StringBuilder(endpointTemplate);

                sb.Append($"&$top={pageSize}&$skip={currentSkip}");
                return sb.ToString();
            }


            var nextUrl = BuildManualPageEndpoint(0);

            var pagesProcessed = 0;
            var itemsProcessed = 0;
            var expandedCount = 0;
            var fallbackCount = 0;
            var totalInserted = 0;
            var childDataTable = new DataTable();
            var importMetaData = new DataTable();


            var sw = System.Diagnostics.Stopwatch.StartNew();

            // Track whether server-provided @odata.nextLink should be used verbatim (absolute or relative)
            var usingServerNextLink = false;
            var skip = 0;

            while (!string.IsNullOrEmpty(nextUrl))
            {
                cancellationToken.ThrowIfCancellationRequested();
                pagesProcessed++;
                using var request = new HttpRequestMessage(HttpMethod.Get, nextUrl);

                if (!string.IsNullOrWhiteSpace(_apiKey))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Token", _apiKey);
                }
                var pageStopwatch = Stopwatch.StartNew();
                using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                pageStopwatch.Stop();
                if (string.IsNullOrWhiteSpace(json))
                {
                    _logger.LogWarning("Empty response from e-shot for endpoint {Endpoint} (page {Page})", nextUrl, pagesProcessed);
                    break;
                }

                try
                {



                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    var items = ExtractItemsFromRoot(root);

                    var dsResult = _converter.ConvertODataPageToDataSet(json);
                    _logger.LogDebug("Converted OData page to DataSet. Tables: {Tables}.", dsResult?.Tables.Count ?? 0);

                    var index = 0;
                    DataTable table = dsResult.Tables[index];

                    importMetaData.Merge(PrepareImportMetaData(table));

                    if (table.Rows.Count > 0)
                    {
                        var insertStopwatch = Stopwatch.StartNew();
                        await _bulkInserter.BulkInsertAsync(destinationTable[index], table, batchSize: pageSize, cancellationToken: cancellationToken).ConfigureAwait(false);

                        

                        insertStopwatch.Stop();

                        totalInserted += table.Rows.Count;
                    }
                    else
                    {
                        _logger.LogInformation("Page contained no rows after conversion; ending. TotalInserted: {TotalInserted}.", totalInserted);
                        break;
                    }

                    childDataTable.Merge(dsResult.Tables[index + 1]);


                    // Try to resolve a server-provided nextLink first (normalized via ResolveNextLink).
                    var resolvedNext = ResolveNextLink(root);
                    if (!string.IsNullOrEmpty(resolvedNext))
                    {
                        usingServerNextLink = true;
                        nextUrl = resolvedNext;
                    }
                    else if (usingServerNextLink)
                    {
                        // Server previously provided nextLink but now didn't -> end paging.
                        nextUrl = null;
                    }
                    else
                    {
                        // Manual paging: use $top/$skip continuation
                        var currentPageCount = items.ToList()?.Count ?? 0;

                        if (currentPageCount == 0)
                        {
                            // no items -> done
                            break;
                        }

                        if (currentPageCount < pageSize)
                        {
                            // last page
                            break;
                        }

                        // more pages expected
                        skip += pageSize;
                        nextUrl = BuildManualPageEndpoint(skip);
                    }


                }
                catch (JsonException ex)
                {
                    // Truncate JSON snippet for logs to avoid huge entries
                    var snippet = json.Length <= 1024 ? json : json.Substring(0, 1024) + "...(truncated)";
                    _logger.LogError(ex, "Failed to parse JSON from endpoint {Endpoint} (page {Page}). Response length: {Length}. Snippet: {Snippet}", nextUrl, pagesProcessed, json.Length, snippet);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error while processing endpoint {Endpoint} (page {Page})", nextUrl, pagesProcessed);
                    throw;
                }
            }


            
            await _bulkInserter.BulkInsertAsync( destinationTable[1],RemoveAllDuplicateRows( childDataTable) , batchSize: childDataTable.Rows.Count, cancellationToken: cancellationToken);
            UpdateImportMetaData(importMetaData);
            await _bulkInserter.BulkInsertAsync("import.CampaignImportMetadata", importMetaData, batchSize: importMetaData.Rows.Count, cancellationToken: cancellationToken);

            return totalInserted;


        }


        private string? ResolveNextLink(JsonElement root)
        {
            if (root.ValueKind != JsonValueKind.Object) return null;

            if (root.TryGetProperty("@odata.nextLink", out var nextLinkProp) &&
                nextLinkProp.ValueKind == JsonValueKind.String)
            {
                var nextLinkRaw = nextLinkProp.GetString();
                if (string.IsNullOrWhiteSpace(nextLinkRaw)) return null;

                // Try parse as absolute URI first for robust handling
                if (Uri.TryCreate(nextLinkRaw, UriKind.Absolute, out var nextUri))
                {
                    var baseUrl = _apiUrl;
                    if (!string.IsNullOrEmpty(baseUrl) && Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
                    {
                        // Compare authority (scheme + host + port) to determine if nextUri belongs to same API host
                        if (string.Equals(baseUri.GetLeftPart(UriPartial.Authority), nextUri.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase))
                        {
                            // Use the path and query portion as a relative endpoint compatible with existing requests
                            var relative = nextUri.PathAndQuery;
                            if (relative.StartsWith("/")) relative = relative[1..];
                            return relative;
                        }
                    }

                    // If it's absolute but not the same host as configured base, return absolute and let externalApiService handle it
                    return nextUri.ToString();
                }

                // If not an absolute URI, normalize relative value (remove leading slash to match initialEndpoint format)
                return nextLinkRaw.TrimStart('/');
            }

            return null;
        }

     
        private static IEnumerable<JsonElement>? ExtractItemsFromRoot(JsonElement root)
        {
            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("value", out var valueElement) &&
                valueElement.ValueKind == JsonValueKind.Array)
            {
                return valueElement.EnumerateArray();
            }

            if (root.ValueKind == JsonValueKind.Array)
            {
                return root.EnumerateArray();
            }

            return null;
        }

        private static DataTable RemoveAllDuplicateRows(DataTable table)
        {
         
                var newTable = table.Clone();

                foreach (var row in table.AsEnumerable()
                             .GroupBy(r => r["ID"])
                             .Select(g => g.First()))
                {
                    newTable.ImportRow(row);
                }

    

            return newTable;
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
                    _logger.LogWarning("UpdateImportMetaData called with null DataTable");
                    return new DataTable();
                }

                if (importMetaDatat.Rows.Count == 0)
                {
                    _logger.LogInformation("UpdateImportMetaData called with empty DataTable");
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
                        _logger.LogWarning(rowEx, "Failed to update import metadata for a row; continuing");
                    }
                }

                _logger.LogInformation("Updated {UpdatedCount} rows setting IsImportComplete=true and ImportEndDate={ImportEndDate}", updatedCount, now);

                return importMetaDatat;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating import metadata");
                return importMetaDatat ?? new DataTable();
            }
        }
    }
}