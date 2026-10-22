using DAS.DigitalEngagement.Models.PerformanceImport;

namespace DAS.DigitalEngagement.Application.Handlers.CampaignToStaging
{
    public interface IImportCampaignStagingHandler
    {
        Task<PerformanceImportReportSummary> Handle(CancellationToken cancellationToken = default);
    }
}
