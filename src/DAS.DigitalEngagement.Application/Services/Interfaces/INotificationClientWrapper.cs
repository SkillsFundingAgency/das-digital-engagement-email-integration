using Notify.Models.Responses;

namespace DAS.DigitalEngagement.Application.Services.Interfaces;

public interface INotificationClientWrapper
{
    Task<EmailNotificationResponse> SendEmailAsync(string emailAddress, string templateId, Dictionary<string, dynamic> personalisation);
}