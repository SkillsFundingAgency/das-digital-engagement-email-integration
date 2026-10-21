using DAS.DigitalEngagement.Application.PerformanceImport.Api;
using DAS.DigitalEngagement.Application.PerformanceImport.Persistence;
using DAS.DigitalEngagement.CampaignInterest.Data.Service;
using DAS.DigitalEngagement.Models.PerformanceImport;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace DAS.DigitalEngagement.Application.PerformanceImport.Services;

/// <summary>
/// Coordinates performance imports with separate limits for API filter requests and SQL writes.
/// Streams paginated data into bounded SQL batches, writes sends and campaigns before dependent
/// performance data, and marks a group complete only after its required data has been saved.
/// </summary>
public sealed class PerformanceImportService(
    IPerformanceDataClient dataClient,
    IPerformanceDataWriter dataWriter,
    ISqlBulkInserter bulkInserter,
    IOptions<EmailMarketingApi> apiConfig,
    ILogger<PerformanceImportService> logger) : IPerformanceImportService
{
    private readonly int _sqlWriteBatchSize = apiConfig.Value.SqlWriteBatchSize > 0
        ? apiConfig.Value.SqlWriteBatchSize
        : throw new ArgumentOutOfRangeException(nameof(apiConfig));
    private readonly int _sendContactFilterBatchSize = apiConfig.Value.SendContactFilterBatchSize > 0
        ? apiConfig.Value.SendContactFilterBatchSize
        : throw new ArgumentOutOfRangeException(nameof(apiConfig));
    private readonly int _apiFilterBatchSize = apiConfig.Value.ApiFilterBatchSize > 0
        ? apiConfig.Value.ApiFilterBatchSize
        : throw new ArgumentOutOfRangeException(nameof(apiConfig));

    // Orchestrates the import in bounded send batches.
    public async Task ImportAsync(
        IReadOnlyCollection<SendEligibilityRecord> eligibleSends,
        DateTimeOffset importStart,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eligibleSends);
        if (eligibleSends.Count == 0)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var stopwatch = Stopwatch.StartNew();
        var batchNumber = 0;
        // Limit each eligible-send batch to the configured API filter size.
        var totalBatchCount = (eligibleSends.Count + _apiFilterBatchSize - 1) / _apiFilterBatchSize;
        logger.LogInformation(
            "Performance import started for {EligibleSendCount} sends across {BatchCount} batches.",
            eligibleSends.Count,
            totalBatchCount);

        // Campaigns may be shared by sends; fetch each campaign ID once per import run.
        var importedCampaignIds = new HashSet<long>();
        // Collect failures from outer batches so we can continue processing later batches and report all failures at the end.
        var outerFailedBatches = new List<Exception>();
        foreach (var eligibleSendBatch in eligibleSends.Chunk(_apiFilterBatchSize))
        {
            batchNumber++;
            var sendIds = eligibleSendBatch.Select(send => send.Id).ToArray();

            try
            {
                logger.LogInformation(
                    "Processing performance import batch {BatchNumber} of {BatchCount} with {SendCount} sends.",
                    batchNumber,
                    totalBatchCount,
                    sendIds.Length);

                var sends = await ToListAsync(
                    dataClient.GetSendsByIdsAsync(sendIds, cancellationToken),
                    cancellationToken);

                var candidateCampaignIds = sends
                    .Where(send => send.CampaignId.HasValue)
                    .Select(send => send.CampaignId!.Value)
                    .Distinct()
                    .ToArray();
                var campaignIds = candidateCampaignIds
                    .Where(campaignId => !importedCampaignIds.Contains(campaignId))
                    .ToArray();
                var campaigns = await ToListAsync(
                    dataClient.GetCampaignsAsync(campaignIds, cancellationToken),
                    cancellationToken);

                logger.LogInformation(
                    "Batch {BatchNumber} loaded {SendCount} sends and {CampaignCount} new campaigns.",
                    batchNumber,
                    sends.Count,
                    campaigns.Count);

                // Write campaigns and sends before their dependent performance records.
                await WriteCampaignsAsync(campaigns, cancellationToken);
                await WriteSendsAsync(sends, cancellationToken);

                importedCampaignIds.UnionWith(campaignIds);

                await ImportSendPerformanceBatchAsync(sends, importStart, cancellationToken);

                logger.LogInformation(
                    "Completed performance import batch {BatchNumber} of {BatchCount}.",
                    batchNumber,
                    totalBatchCount);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (AggregateException aggregate)
            {
                // Collect inner exceptions and continue with next outer batch.
                outerFailedBatches.AddRange(aggregate.InnerExceptions);
                logger.LogWarning(aggregate, "One or more SendContacts groups failed in outer batch {BatchNumber}; continuing with next batch.", batchNumber);
            }
            catch (Exception ex)
            {
                outerFailedBatches.Add(ex);
                logger.LogWarning(ex, "Send performance import failed for outer batch {BatchNumber}; continuing with next batch.", batchNumber);
            }
        }
        if (outerFailedBatches.Count > 0)
        {
            throw new AggregateException(
                $"Performance import failed for {outerFailedBatches.Count} outer batch(es). Failed batches were logged; successful batches were completed.",
                outerFailedBatches);
        }

        stopwatch.Stop();
        logger.LogInformation(
            "Performance import completed for {EligibleSendCount} sends in {ElapsedSeconds} seconds.",
            eligibleSends.Count,
            stopwatch.Elapsed.TotalSeconds);
    }

    private async Task ImportSendPerformanceBatchAsync(
        IReadOnlyCollection<Send> sends,
        DateTimeOffset importStart,
        CancellationToken cancellationToken)
    {
        var totalSendContactCount = 0;
        var totalContactRecordCount = 0;
        var batchNumber = 0;
        var failedBatches = new List<Exception>();

        // Keep each Send ID group bounded, record completion only after its send contacts, contacts,
        // and downstream interaction data have been written.
        // A failed group is logged without preventing later groups from running.
        foreach (var sendBatch in sends.Chunk(_sendContactFilterBatchSize))
        {
            batchNumber++;
            try
            {
                var result = await ImportSendContactGroupAsync(sendBatch, batchNumber, importStart, cancellationToken);
                totalSendContactCount += result.SendContactCount;
                totalContactRecordCount += result.ContactCount;
            }
            catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                failedBatches.Add(LogSendContactGroupFailure(batchNumber, sendBatch, exception));
            }
        }

        logger.LogInformation(
            "Processed {SendContactCount} send contacts for {SendCount} sends and wrote {ContactCount} contacts.",
            totalSendContactCount,
            sends.Count,
            totalContactRecordCount);

        if (failedBatches.Count > 0)
        {
            throw new AggregateException(
                $"Performance import failed for {failedBatches.Count} SendContacts batch(es). Failed batches were logged; successful batches were completed.",
                failedBatches);
        }
    }

    private async Task<(int SendContactCount, int ContactCount)> ImportSendContactGroupAsync(
        Send[] sendBatch,
        int batchNumber,
        DateTimeOffset importStart,
        CancellationToken cancellationToken)
    {
        var sendIds = sendBatch.Select(send => send.Id).ToArray();

        var sendContactCount = 0;
        var contactCount = 0;

        await foreach (var sendContactPage in dataClient.GetSendContactPagesAsync(sendIds, cancellationToken)
            .WithCancellation(cancellationToken))
        {
            await WriteSendContactsAsync(sendContactPage, cancellationToken);

            sendContactCount += sendContactPage.Count;
            contactCount += await ImportContactDataAsync(sendContactPage, cancellationToken);
        }

        await ImportSendInteractionDataAsync(sendIds, batchNumber, cancellationToken);

        // Write completion metadata only after the send contacts, contacts, and interaction datasets have been processed.
        var importCompleteness = sendBatch.ToDictionary(s => s.Id, s => true);
        await dataWriter.WriteImportCompletionsAsync(sendBatch, importStart, importCompleteness, cancellationToken);

        logger.LogInformation(
            "Completed SendContacts batch {BatchNumber} for {SendCount} sends: {SendContactCount} send contacts, {ContactCount} contacts.",
            batchNumber,
            sendBatch.Length,
            sendContactCount,
            contactCount);

        return (sendContactCount, contactCount);
    }

    private async Task ImportSendInteractionDataAsync(
        long[] sendIds,
        int batchNumber,
        CancellationToken cancellationToken)
    {
        // These IDs are already within the API filter limit; stream event results into SQL-sized writes.
        // Run event imports in bounded parallelism to reduce time without overloading dependencies.
        var maxConcurrentEventImports = Math.Max(1, apiConfig.Value.MaxConcurrentEventImports);
        using var gate = new SemaphoreSlim(maxConcurrentEventImports, maxConcurrentEventImports);

        async Task RunBoundedAsync(Func<Task<int>> importOperation, Action<int> logOperation)
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                var count = await importOperation();
                logOperation(count);
            }
            finally
            {
                gate.Release();
            }
        }

        var tasks = new[]
        {
            RunBoundedAsync(
                () => WriteApiRecordsAsync(
                    dataClient.GetLinksAsync(sendIds, cancellationToken),
                    (records, token) => dataWriter.WriteLinksAsync(records, token),
                    _sqlWriteBatchSize,
                    cancellationToken),
                count => logger.LogInformation(
                    "Processed send batch {BatchNumber} of {SendCount}: wrote {LinkCount} links.",
                    batchNumber,
                    sendIds.Length,
                    count)),

            RunBoundedAsync(
                () => WriteApiRecordsAsync(
                    dataClient.GetUserAgentsAsync(sendIds, cancellationToken),
                    (records, token) => dataWriter.WriteUserAgentsAsync(records, token),
                    _sqlWriteBatchSize,
                    cancellationToken),
                count => logger.LogInformation(
                    "Processed send batch {BatchNumber} of {SendCount}: wrote {UserAgentCount} user agents.",
                    batchNumber,
                    sendIds.Length,
                    count)),

            RunBoundedAsync(
                () => WriteApiRecordsAsync(
                    dataClient.GetDisplayedContactsAsync(sendIds, cancellationToken),
                    (records, token) => dataWriter.WriteDisplayedContactsAsync(records, token),
                    _sqlWriteBatchSize,
                    cancellationToken),
                count => logger.LogInformation(
                    "Processed send batch {BatchNumber} of {SendCount}: wrote {DisplayedContactCount} displayed contacts.",
                    batchNumber,
                    sendIds.Length,
                    count)),

            RunBoundedAsync(
                () => WriteApiRecordsAsync(
                    dataClient.GetClickedContactsAsync(sendIds, cancellationToken),
                    (records, token) => dataWriter.WriteClickedContactsAsync(records, token),
                    _sqlWriteBatchSize,
                    cancellationToken),
                count => logger.LogInformation(
                    "Processed send batch {BatchNumber} of {SendCount}: wrote {ClickedContactCount} clicked contacts.",
                    batchNumber,
                    sendIds.Length,
                    count)),

            RunBoundedAsync(
                () => WriteApiRecordsAsync(
                    dataClient.GetBouncedContactsAsync(sendIds, cancellationToken),
                    (records, token) => dataWriter.WriteBouncedContactsAsync(records, token),
                    _sqlWriteBatchSize,
                    cancellationToken),
                count => logger.LogInformation(
                    "Processed send batch {BatchNumber} of {SendCount}: wrote {BouncedContactCount} bounced contacts.",
                    batchNumber,
                    sendIds.Length,
                    count)),

            RunBoundedAsync(
                () => WriteApiRecordsAsync(
                    dataClient.GetUnsubscribedContactsAsync(sendIds, cancellationToken),
                    (records, token) => dataWriter.WriteUnsubscribedContactsAsync(records, token),
                    _sqlWriteBatchSize,
                    cancellationToken),
                count => logger.LogInformation(
                    "Processed send batch {BatchNumber} of {SendCount}: wrote {UnsubscribedContactCount} unsubscribed contacts.",
                    batchNumber,
                    sendIds.Length,
                    count))
        };

        await Task.WhenAll(tasks);
    }

    private async Task<int> ImportContactDataAsync(
        IReadOnlyCollection<SendContactApiRecord> sendContacts,
        CancellationToken cancellationToken)
    {
        var contactIds = sendContacts.Select(record => record.ContactId).Distinct().ToArray();
        var contactCount = 0;

        // Fetch distinct contacts in API-sized groups. Check the DB first and only request missing IDs from the API.
        foreach (var contactIdBatch in contactIds.Chunk(_apiFilterBatchSize))
        {
            // Check which IDs already exist in our staging table to avoid unnecessary API calls.
            var existingIds = await bulkInserter.QueryExistingIdsAsync(ImportTableNames.Contacts, contactIdBatch, cancellationToken);
            var missingIds = contactIdBatch.Where(id => !existingIds.Contains(id)).ToArray();
            if (missingIds.Length == 0)
            {
                continue;
            }

            contactCount += await WriteApiRecordsAsync(
                dataClient.GetContactsAsync(missingIds, cancellationToken),
                async (records, token) =>
                {
                    await dataWriter.WriteContactsAsync(records, token);
                },
                _sqlWriteBatchSize,
                cancellationToken);
        }

        return contactCount;
    }

    private InvalidOperationException LogSendContactGroupFailure(
        int batchNumber,
        Send[] sendBatch,
        Exception exception)
    {
        // Log the Send IDs in this failed group and continue; report failures together after all groups run.
        var sendIdList = string.Join(",", sendBatch.Select(send => send.Id));
        logger.LogError(
            exception,
            "Performance import failed for SendContacts batch {BatchNumber} containing {SendCount} Sends (IDs: {SendIds}). Error: {ErrorMessage}. Continuing with the next batch.",
            batchNumber,
            sendBatch.Length,
            sendIdList,
            exception.Message);

        return new InvalidOperationException(
            $"SendContacts batch {batchNumber} failed for Send IDs [{sendIdList}]: {exception.Message}",
            exception);
    }

    private static async Task<int> WriteApiRecordsAsync<T>(
        IAsyncEnumerable<T> source,
        Func<IReadOnlyCollection<T>, CancellationToken, Task> writer,
        int writeBatchSize,
        CancellationToken cancellationToken)
    {
        // Buffer streamed records to SQL write size instead of materializing the full API response.
        var batch = new List<T>(writeBatchSize);
        var totalCount = 0;

        await foreach (var record in source.WithCancellation(cancellationToken))
        {
            batch.Add(record);
            if (batch.Count < writeBatchSize)
            {
                continue;
            }

            await writer(batch, cancellationToken);
            totalCount += batch.Count;
            batch.Clear();
        }

        if (batch.Count > 0)
        {
            await writer(batch, cancellationToken);
            totalCount += batch.Count;
        }

        return totalCount;
    }

    private async Task WriteSendsAsync(
        IReadOnlyCollection<Send> sends,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var sendBatch in sends.Chunk(_sqlWriteBatchSize))
        {
            await dataWriter.WriteSendsAsync(sendBatch, cancellationToken);
        }
    }

    private async Task WriteSendContactsAsync(
        IReadOnlyCollection<SendContactApiRecord> sendContacts,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var sendContactBatch in sendContacts.Chunk(_sqlWriteBatchSize))
        {
            await dataWriter.WriteSendContactsAsync(sendContactBatch, cancellationToken);
        }
    }

    private async Task WriteCampaignsAsync(
        IReadOnlyCollection<Campaign> campaigns,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var campaignBatch in campaigns.Chunk(_sqlWriteBatchSize))
        {
            await dataWriter.WriteCampaignsAsync(campaignBatch, cancellationToken);
        }
    }

    private static async Task<List<T>> ToListAsync<T>(
        IAsyncEnumerable<T> source,
        CancellationToken cancellationToken)
    {
        // Materialize only the bounded Send and Campaign ID lookup responses.
        var records = new List<T>();
        await foreach (var record in source.WithCancellation(cancellationToken))
        {
            records.Add(record);
        }

        return records;
    }

}
