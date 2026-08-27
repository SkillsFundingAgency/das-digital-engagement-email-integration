// using DAS.DigitalEngagement.Application.Handlers.Campaigns;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Data;

namespace DAS.DigitalEngagement.Application.Handlers.CampaignToStaging
{
    public class ImportCampaignStagingHandler(ICampaignStagingService campaignStagingService, ILogger<ImportCampaignStagingHandler> logger) : IImportCampaignStagingHandler
    {
        public async Task Handle(CancellationToken cancellationToken = default)
        {
            // Set subAccountId = null for production. For local/testing, use sub-account 3 for lots of small sends, 5 for one huge send
            var eligibleSends = await campaignStagingService.GetEligibleSendsAsync(subAccountId: null, cancellationToken: cancellationToken);
            if (eligibleSends.Rows.Count == 0)
            {
                logger.LogWarning("No eligible sends found for import");
                return;
            }

            var sendIds = eligibleSends.Rows.Cast<System.Data.DataRow>().Select(row => row.Field<long>("Id")).ToList();
            var eliSendsWithCampaign = await campaignStagingService.GetSendsAndCampaign(sendIds, cancellationToken);
            var insertedImportMetaData =new DataTable();

            // store all sends to sends table using bulk inserter
            try
            {
                // If the service expects a concrete list, materialize it
                //var sendsList = eligibleSends.ToList();
                 insertedImportMetaData = campaignStagingService.PrepareImportMetaData(eliSendsWithCampaign?.Tables[0]);


                int insertedSendCount = await campaignStagingService.BulkInsertSendsAsync(eliSendsWithCampaign?.Tables[0], cancellationToken);
                int insertedCampaignCount = await campaignStagingService.BulkInsertCampaignAsync(eliSendsWithCampaign?.Tables[1], cancellationToken);


                insertedImportMetaData =  campaignStagingService.UpdateImportMetaData(insertedImportMetaData);

                // logger.LogInformation("Bulk inserted {Count} sends into staging table", insertedCount);
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Bulk insert operation was cancelled");
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to bulk insert eligible sends into staging table");
                return;
            }
            finally
            {
                var count = await campaignStagingService.BulkInsertCampaignImportMetadataAsync(insertedImportMetaData, cancellationToken);
            }

            // long campaignId;

            //foreach (var send in eligibleSends)
            //{
            //    logger.LogInformation("Processing Send {SendId} for sub-account {Account}", send.ID, send.Account);

            //    // campaignId = await campaignStagingService.SaveCampaignDetailsAsync(BuildCampaignObject(send), cancellationToken);
            //    //// write Campaign / Send info to db
            //    //if (campaignId == 0)
            //    //{
            //    //    logger.LogError("Failed to save campaign details for Send {SendId}, sub-account {Account}", send.ID, send.Account);
            //    //    continue;
            //    //}

            //    //// write import meta data to db to track import status, start time etc.
            //    //var metadata = BuildCampaignImportMetadataObject(campaignId, send.ID);
            //    //int sendId = await campaignService.UpsertCampaignImportMetadataAsync(metadata, cancellationToken);
            //    //if (sendId == 0)
            //    //{
            //    //    logger.LogError("Failed to upsert campaign import metadata for Send {SendId}, sub-account {Account}", send.ID, send.Account);
            //    //    continue;
            //    //}

            //    //var userAgentInfo = await campaignService.GetUserAgentInfoForSendAsync(send.ID, cancellationToken);

            //    //await ProcessDisplayedAndClickedContactsFromEShot(campaignService, logger, send, userAgentInfo, cancellationToken);

            //    //await ProcessBouncedAndUnScrubsibedContactsFromEShot(campaignService, logger, send, cancellationToken);

            //    // write import meta data to db to track import status, end time, etc.
            //    metadata.IsImportComplete = true;
            //    metadata.ImportEndDate = DateTime.UtcNow;
            //    sendId = await campaignService.UpsertCampaignImportMetadataAsync(metadata, cancellationToken);
            //    if (sendId == 0)
            //    {
            //        logger.LogError("Failed to mark campaign import complete for Send {SendId}, sub-account {Account}", send.ID, send.Account);
            //        continue;
            //    }

            //    logger.LogInformation("Processing complete for Send {SendId}, sub-account {Account}", send.ID, send.Account);
            //}
        }
    }
}
