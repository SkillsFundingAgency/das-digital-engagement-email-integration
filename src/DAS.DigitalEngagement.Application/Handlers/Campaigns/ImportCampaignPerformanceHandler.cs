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

        //TODO - FOR LOCAL ONLY, delete before merging - use sub-account 3 for lots of small sends, 5 for one hugh send
        var subAccountId = 5;

        try
        {
            var sends = await _campaignService.GetAllSendsAsync(
                subAccountId: subAccountId, 
                cancellationToken: cancellationToken);

            if (!sends.Any())
            {
                _logger.LogWarning("No sends found");
                return;
            }

            _logger.LogInformation("Retrieved {SendCount} sends for sub-account {SubAccountId}", sends.Count(), subAccountId);

            foreach (var send in sends)
            {
                _logger.LogInformation("Processing Send {SendId} for sub-account {SubAccountId}", send.ID, subAccountId);

                var userAgentInfo = await _campaignService.GetUserAgentInfoForSendAsync(send.ID, cancellationToken);
                _logger.LogInformation("Retrieved {UserAgentCount} unique user agent records for Send {SendId} in sub-account {SubAccountId}", userAgentInfo.Count(), send.ID, subAccountId);

                var displayedContacts = await _campaignService.GetDisplayedContactsForSendAsync(send.ID, userAgentInfo, cancellationToken);
                _logger.LogInformation("Retrieved {ContactCount} displayed contacts for Send {SendId} in sub-account {SubAccountId}", displayedContacts.Count(), send.ID, subAccountId);

                var clickedLinkContacts = await _campaignService.GetClickedLinkContactsForSendAsync(send.ID, userAgentInfo, cancellationToken);
                _logger.LogInformation("Retrieved {ContactCount} clicked link contacts for Send {SendId} in sub-account {SubAccountId}", clickedLinkContacts.Count(), send.ID, subAccountId);

                var bouncedEmailContacts = await _campaignService.GetBouncedEmailContactsForSendAsync(send.ID, cancellationToken);
                _logger.LogInformation("Retrieved {ContactCount} bounced email contacts for Send {SendId} in sub-account {SubAccountId}", bouncedEmailContacts.Count(), send.ID, subAccountId);

                var unsubscribedContacts = await _campaignService.GetUnsubscribedContactsForSendAsync(send.ID, cancellationToken);
                _logger.LogInformation("Retrieved {ContactCount} unsubscribed contacts for Send {SendId} in sub-account {SubAccountId}", unsubscribedContacts.Count(), send.ID, subAccountId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing sub-account {SubAccountId}", subAccountId);
            throw;
        }


        _logger.LogInformation("Campaign performance import completed successfully");

    }
}
