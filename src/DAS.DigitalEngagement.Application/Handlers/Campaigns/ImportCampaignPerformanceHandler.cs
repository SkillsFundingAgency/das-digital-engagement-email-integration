using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.CampaignInterest.Data.Models;
using DAS.DigitalEngagement.Models.Campaigns;
using Microsoft.Extensions.Logging;

namespace DAS.DigitalEngagement.Application.Handlers.Campaigns;

public class ImportCampaignPerformanceHandler(ICampaignService campaignService, ILogger<ImportCampaignPerformanceHandler> logger) : IImportCampaignPerformanceHandler
{
    public async Task Handle(CancellationToken cancellationToken = default)
    {
        // Set subAccountId = null for production. For local/testing, use sub-account 3 for lots of small sends, 5 for one huge send
        var eligibleSends = await campaignService.GetEligibleSendsAsync(subAccountId: null, cancellationToken: cancellationToken);
        if (!eligibleSends.Any())
        {
            logger.LogWarning("No eligible sends found for import");
            return;
        }

        long campaignId;

        foreach (var send in eligibleSends)
        {
            logger.LogInformation("Processing Send {SendId} for sub-account {Account}", send.ID, send.Account);

            campaignId = await campaignService.SaveCampaignDetailsAsync(BuildCampaignObject(send), cancellationToken);
            // write Campaign / Send info to db
            if (campaignId == 0)
            {
                logger.LogError("Failed to save campaign details for Send {SendId}, sub-account {Account}", send.ID, send.Account);
                continue;
            }

            // write import meta data to db to track import status, start time etc.
            var metadata = BuildCampaignImportMetadataObject(campaignId);
            int sendId = await campaignService.UpsertCampaignImportMetadataAsync(metadata, cancellationToken);
            if (sendId == 0)
            {
                logger.LogError("Failed to upsert campaign import metadata for Send {SendId}, sub-account {Account}", send.ID, send.Account);
                continue;
            }

            var userAgentInfo = await campaignService.GetUserAgentInfoForSendAsync(send.ID, cancellationToken);

            await ProcessDisplayedAndClickedContactsFromEShot(campaignService, logger, send, userAgentInfo, cancellationToken);

            await ProcessBouncedAndUnScrubsibedContactsFromEShot(campaignService, logger, send, cancellationToken);

            // write import meta data to db to track import status, end time, etc.
            metadata.IsImportComplete = true;
            metadata.ImportEndDate = DateTime.UtcNow;
            sendId = await campaignService.UpsertCampaignImportMetadataAsync(metadata, cancellationToken);
            if (sendId == 0)
            {
                logger.LogError("Failed to mark campaign import complete for Send {SendId}, sub-account {Account}", send.ID, send.Account);
                continue;
            }

            logger.LogInformation("Processing complete for Send {SendId}, sub-account {Account}", send.ID, send.Account);
        }
    }

    private static async Task ProcessDisplayedAndClickedContactsFromEShot(
        ICampaignService campaignService,
        ILogger<ImportCampaignPerformanceHandler> logger,
        Send send,
        IEnumerable<UserAgentInfo> userAgentInfo,
        CancellationToken cancellationToken = default)
    {
        // Process displayed contacts
        if (await campaignService.GetDisplayedContactsFromEShot(send.ID, userAgentInfo, cancellationToken))
        {
            logger.LogInformation("Successfully imported displayed contacts for Send {SendId}, sub-account {Account}", send.ID, send.Account);
        }
        else
        {
            logger.LogError("Failed to import displayed contacts for Send {SendId}, sub-account {Account}", send.ID, send.Account);
        }

        // Process clicked link contacts
        if (await campaignService.GetClickedLinkContactsFromEShot(send.ID, userAgentInfo, cancellationToken))
        {
            logger.LogInformation("Successfully imported clicked link contacts for Send {SendId}, sub-account {Account}", send.ID, send.Account);
        }
        else
        {
            logger.LogError("Failed to import clicked link contacts for Send {SendId}, sub-account {Account}", send.ID, send.Account);
        }
    }

    private static async Task ProcessBouncedAndUnScrubsibedContactsFromEShot(
        ICampaignService campaignService,
        ILogger<ImportCampaignPerformanceHandler> logger,
        Send send,
        CancellationToken cancellationToken = default)
    {
        // Process bounced contacts
        if (await campaignService.GetBouncedEmailContactsFromEShot(send.ID, cancellationToken))
        {
            logger.LogInformation("Successfully imported bounced email contacts for Send {SendId}, sub-account {Account}", send.ID, send.Account);
        }
        else
        {
            logger.LogError("Failed to import bounced email contacts for Send {SendId}, sub-account {Account}", send.ID, send.Account);
        }

        // Process unsubscribed contacts
        if (await campaignService.GetUnsubscribedContactsFromEShot(send.ID, cancellationToken))
        {
            logger.LogInformation("Successfully imported unsubscribed contacts for Send {SendId}, sub-account {Account}", send.ID, send.Account);
        }
        else
        {
            logger.LogError("Failed to import unsubscribed contacts for Send {SendId}, sub-account {Account}", send.ID, send.Account);
        }
    }

    private static CampaignInterest.Data.Models.Campaigns BuildCampaignObject(Send send)
    {
        return new CampaignInterest.Data.Models.Campaigns
        {
            ExternalCampaignId = send.CampaignID,
            CampaignName = send.CampaignName,
            ExternalSendId = send.ID,
            SendName = send.Name,
            Type = send.CampaignType,
            Account = send.Account,
            FirstSendDate = send.SendDate != null ? DateTime.Parse(send.SendDate, System.Globalization.CultureInfo.InvariantCulture) : default,
            LastSendDate = send.SendCompletedDate != null ? DateTime.Parse(send.SendCompletedDate, System.Globalization.CultureInfo.InvariantCulture) : default,
            SubStatus = send.SubStatus,
            ContactCount = send.ContactCount,
            FromEmailAddress = send.FromEmail,
            FromName = send.FromName,
            ReplyEmailAddress = send.ReplyEmail,
            Subject = send.SubjectLine,
            CreatedBy = send.CreatedBy,
            CreatedOn = send.CreatedDate != null ? DateTime.Parse(send.CreatedDate, System.Globalization.CultureInfo.InvariantCulture) : default
        };
    }

    private static CampaignImportMetadata BuildCampaignImportMetadataObject(long campaignId)
    {
        return new CampaignImportMetadata
        {
            CampaignId = campaignId,
            IsImportComplete = false,
            ImportStartDate = DateTime.UtcNow
        };
    }
}
