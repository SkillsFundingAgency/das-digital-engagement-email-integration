using DAS.DigitalEngagement.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace DAS.DigitalEngagement.Application.Handlers.Campaigns;

public class ImportCampaignPerformanceHandler : IImportCampaignPerformanceHandler
{
    private readonly ICampaignService _campaignService;
    private readonly ILogger<ImportCampaignPerformanceHandler> _logger;

    public ImportCampaignPerformanceHandler(
        ICampaignService campaignService,
        ILogger<ImportCampaignPerformanceHandler> logger)
    {
        _campaignService = campaignService;
        _logger = logger;
    }

    public async Task Handle(CancellationToken cancellationToken = default)
    {
        // Set subAccountId = null for production. For local/testing, use sub-account 3 for lots of small sends, 5 for one hugh send
        var sends = await _campaignService.GetAllSendsAsync(
            subAccountId: null,
            cancellationToken: cancellationToken);

        if (!sends.Any())
        {
            _logger.LogWarning("No sends found");
            return;
        }

        foreach (var send in sends)
        {
            _logger.LogInformation("Processing Send {SendId} for sub-account {Account}", send.ID, send.Account);

            var userAgentInfo = await _campaignService.GetUserAgentInfoForSendAsync(send.ID, cancellationToken);
            
            var displayedContacts = await _campaignService.GetDisplayedContactsForSendAsync(send.ID, userAgentInfo, cancellationToken);
            
            var clickedLinkContacts = await _campaignService.GetClickedLinkContactsForSendAsync(send.ID, userAgentInfo, cancellationToken);
            
            var bouncedEmailContacts = await _campaignService.GetBouncedEmailContactsForSendAsync(send.ID, cancellationToken);
            
            var unsubscribedContacts = await _campaignService.GetUnsubscribedContactsForSendAsync(send.ID, cancellationToken);

            _logger.LogInformation("Processing complete for Send {SendId}, sub-account {Account}", send.ID, send.Account);
        }
    }
}
