using DAS.DigitalEngagement.Application.PerformanceImport.Services;
using Microsoft.Extensions.Logging;

namespace DAS.DigitalEngagement.Application.Handlers.CampaignToStaging
{
    public class ImportCampaignStagingHandler(
        ISendEligibilityService sendEligibilityService,
        IPerformanceImportService performanceImportService,
        ILogger<ImportCampaignStagingHandler> logger) : IImportCampaignStagingHandler
    {
        public async Task Handle(CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Performance import handler started.");

            var eligibleSends = await sendEligibilityService.GetEligibleSendRecordsAsync(cancellationToken);
            logger.LogInformation("Eligibility check returned {EligibleSendCount} sends.", eligibleSends.Count);

            if (eligibleSends.Count == 0)
            {
                logger.LogInformation("No eligible sends found; performance import will finish without processing.");
                return;
            }

            await performanceImportService.ImportAsync(
                eligibleSends,
                DateTimeOffset.UtcNow,
                cancellationToken);

            logger.LogInformation("Performance import handler completed for {EligibleSendCount} sends.", eligibleSends.Count);
          
        }
    }
}
