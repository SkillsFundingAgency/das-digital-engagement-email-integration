using DAS.DigitalEngagement.Application.PerformanceImport.Services;
using DAS.DigitalEngagement.Models.PerformanceImport;
using Microsoft.Extensions.Logging;

namespace DAS.DigitalEngagement.Application.Handlers.CampaignToStaging
{
    public class ImportCampaignStagingHandler(
        ISendEligibilityService sendEligibilityService,
        IPerformanceImportService performanceImportService,
        ILogger<ImportCampaignStagingHandler> logger) : IImportCampaignStagingHandler
    {
        public async Task<PerformanceImportReportSummary> Handle(CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Performance import handler started.");

            var eligibleSends = await sendEligibilityService.GetEligibleSendRecordsAsync(cancellationToken);
            logger.LogInformation("Eligibility check returned {EligibleSendCount} sends.", eligibleSends.Count);

            if (eligibleSends.Count == 0)
            {
                logger.LogInformation("No eligible sends found; performance import will finish without processing.");
                return new PerformanceImportReportSummary
                {
                    StartedAtUtc = DateTimeOffset.UtcNow,
                    EndedAtUtc = DateTimeOffset.UtcNow,
                    Status = PerformanceImportStatus.Completed,
                    EligibleSends = 0,
                    Messages = ["No eligible sends found; nothing imported."]
                };
            }

            var summary = await performanceImportService.ImportAsync(
                eligibleSends,
                DateTimeOffset.UtcNow,
                cancellationToken);

            logger.LogInformation("Performance import handler completed for {EligibleSendCount} sends.", eligibleSends.Count);
            return summary;
          
        }
    }
}
