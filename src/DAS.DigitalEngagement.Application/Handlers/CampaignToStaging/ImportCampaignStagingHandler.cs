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

         

            try
            {
                var importStartDateTime = DateTime.Now;
                await campaignStagingService.ImportSendsAndCampaign(sendIds, importStartDateTime);

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
          
        }
    }
}
