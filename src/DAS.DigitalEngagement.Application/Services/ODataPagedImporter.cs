using DAS.DigitalEngagement.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Data;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DAS.DigitalEngagement.Application.Services
{
    public sealed class ODataPagedImporter : IODataPagedImporter
    {
        private readonly HttpClient _http;
        private readonly IJsonToDataTableConverter _converter;
        private readonly ISqlBulkInserter _bulkInserter;
        private readonly ILogger<ODataPagedImporter> _logger;
        private readonly int _pageSize;

        public ODataPagedImporter(HttpClient httpClient, IJsonToDataTableConverter converter, ISqlBulkInserter bulkInserter, ILogger<ODataPagedImporter> logger, int pageSize = 5000)
        {
            _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _converter = converter ?? throw new ArgumentNullException(nameof(converter));
            _bulkInserter = bulkInserter ?? throw new ArgumentNullException(nameof(bulkInserter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pageSize = pageSize > 0 ? pageSize : throw new ArgumentOutOfRangeException(nameof(pageSize));
        }

        /// <summary>
        /// Pages the provided OData endpoint and bulk-inserts each page into destinationTable.
        /// endpointTemplate must include placeholders for $skip and $top if required, e.g. "Sends?$orderby=ID&$skip={0}&$top={1}"
        /// </summary>
        public async Task<long> ImportEndpointToTableAsync(string endpointTemplate, string destinationTable, string connectionString, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(endpointTemplate)) throw new ArgumentNullException(nameof(endpointTemplate));
            if (string.IsNullOrWhiteSpace(destinationTable)) throw new ArgumentNullException(nameof(destinationTable));
            if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException(nameof(connectionString));

            _logger.LogInformation("Starting OData import. DestinationTable: {Table}. PageSize: {PageSize}.", destinationTable, _pageSize);

            long totalInserted = 0;
            int skip = 0;

            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var endpoint = string.Format(endpointTemplate, skip, _pageSize);
                    _logger.LogDebug("Requesting OData page. Endpoint: {Endpoint}. Skip: {Skip}.", endpoint, skip);

                    using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
                    // HttpClient is expected to be pre-configured by the caller (auth, default headers, timeouts, etc.)

                    var pageStopwatch = Stopwatch.StartNew();
                    using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
                    pageStopwatch.Stop();

                    _logger.LogDebug("Received HTTP response. StatusCode: {StatusCode}. ElapsedMs: {ElapsedMs}.", response.StatusCode, pageStopwatch.ElapsedMilliseconds);

                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                    var root = JObject.Parse(json);
                    var array = root.Value<JArray>("value");
                    if (array == null || array.Count == 0)
                    {
                        _logger.LogInformation("No records returned from endpoint; paging complete. TotalInserted: {TotalInserted}.", totalInserted);
                        break;
                    }

                    var table = _converter.ConvertODataPageToDataTable(json);
                    _logger.LogDebug("Converted OData page to DataTable. Rows: {Rows}.", table?.Rows.Count ?? 0);

                    if (table.Rows.Count > 0)
                    {
                        var insertStopwatch = Stopwatch.StartNew();
                        await _bulkInserter.BulkInsertAsync(destinationTable, table, batchSize: _pageSize, cancellationToken: cancellationToken).ConfigureAwait(false);
                        insertStopwatch.Stop();

                        totalInserted += table.Rows.Count;
                        _logger.LogInformation("Inserted {Rows} rows into {Table} in {ElapsedMs}ms. TotalInserted: {TotalInserted}.", table.Rows.Count, destinationTable, insertStopwatch.ElapsedMilliseconds, totalInserted);
                    }
                    else
                    {
                        _logger.LogInformation("Page contained no rows after conversion; ending. TotalInserted: {TotalInserted}.", totalInserted);
                        break;
                    }

                    if (array.Count < _pageSize)
                    {
                        _logger.LogInformation("Final page received (fewer than page size). Paging complete. TotalInserted: {TotalInserted}.", totalInserted);
                        break;
                    }

                    skip += _pageSize;
                }

                _logger.LogInformation("OData import finished successfully. TotalInserted: {TotalInserted}.", totalInserted);
                return totalInserted;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("OData import was cancelled. TotalInserted so far: {TotalInserted}.", totalInserted);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OData import failed for endpoint template {EndpointTemplate}. TotalInserted so far: {TotalInserted}.", endpointTemplate, totalInserted);
                throw;
            }
        }
    }
}