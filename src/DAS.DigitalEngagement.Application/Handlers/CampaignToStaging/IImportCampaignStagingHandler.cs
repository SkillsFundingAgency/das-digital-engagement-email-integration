namespace DAS.DigitalEngagement.Application.Handlers.CampaignToStaging
{
    public interface IImportCampaignStagingHandler
    {
        Task Handle(CancellationToken cancellationToken = default);
    }
}
