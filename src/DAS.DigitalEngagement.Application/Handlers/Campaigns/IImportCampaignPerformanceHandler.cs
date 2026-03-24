namespace DAS.DigitalEngagement.Application.Handlers.Campaigns;

public interface IImportCampaignPerformanceHandler
{
    Task Handle(CancellationToken cancellationToken = default);
}
