using SFA.DAS.Notifications.Messages.Commands;

namespace DAS.DigitalEngagement.Application.Services.Interfaces
{
    public interface INotificationService
    {
        Task Send(SendEmailCommand email);
    }
}
