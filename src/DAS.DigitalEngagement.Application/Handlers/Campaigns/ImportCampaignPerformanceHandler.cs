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
        _logger.LogInformation("Starting campaign performance import");

            // Set subAccountId = null for production. For local/testing, use sub-account 3 for lots of small sends, 5 for one hugh send
            var sends = await _campaignService.GetAllSendsAsync(
                subAccountId: null, 
                cancellationToken: cancellationToken);

            if (!sends.Any())
            {
                _logger.LogWarning("No sends found");
                return;
            }

            _logger.LogInformation("Retrieved {SendCount} sends", sends.Count());

            foreach (var send in sends)
            {
                _logger.LogInformation("Processing Send {SendId} for sub-account {Account}", send.ID, send.Account);

                var userAgentInfo = await _campaignService.GetUserAgentInfoForSendAsync(send.ID, cancellationToken);
                _logger.LogInformation("Retrieved {UserAgentCount} unique user agent records for Send {SendId} in sub-account {Account}", userAgentInfo.Count(), send.ID, send.Account);

                var displayedContacts = await _campaignService.GetDisplayedContactsForSendAsync(send.ID, userAgentInfo, cancellationToken);
                _logger.LogInformation("Retrieved {ContactCount} displayed contacts for Send {SendId} in sub-account {Account}", displayedContacts.Count(), send.ID, send.Account);

                var clickedLinkContacts = await _campaignService.GetClickedLinkContactsForSendAsync(send.ID, userAgentInfo, cancellationToken);
                _logger.LogInformation("Retrieved {ContactCount} clicked link contacts for Send {SendId} in sub-account {Account}", clickedLinkContacts.Count(), send.ID, send.Account);

                var bouncedEmailContacts = await _campaignService.GetBouncedEmailContactsForSendAsync(send.ID, cancellationToken);
                _logger.LogInformation("Retrieved {ContactCount} bounced email contacts for Send {SendId} in sub-account {Account}", bouncedEmailContacts.Count(), send.ID, send.Account);

                var unsubscribedContacts = await _campaignService.GetUnsubscribedContactsForSendAsync(send.ID, cancellationToken);
                _logger.LogInformation("Retrieved {ContactCount} unsubscribed contacts for Send {SendId} in sub-account {Account}", unsubscribedContacts.Count(), send.ID, send.Account);
            }
    }
}
