using Notify.Client;
using Notify.Models.Responses;
using DAS.DigitalEngagement.Application.Services.Interfaces;

namespace DAS.DigitalEngagement.Application.Services;

public class NotificationClientWrapper : INotificationClientWrapper
{
    private readonly NotificationClient _notificationClient;

    public NotificationClientWrapper(string apiKey)
    {
        _notificationClient = new NotificationClient(apiKey);
    }

    public async Task<EmailNotificationResponse> SendEmailAsync(string emailAddress, string templateId, Dictionary<string, dynamic> personalisation)
    {
        return await _notificationClient.SendEmailAsync(emailAddress, templateId, personalisation);
    }
}