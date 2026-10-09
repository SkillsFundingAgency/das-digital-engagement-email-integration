using NServiceBus;
using SFA.DAS.Notifications.Messages.Commands;
using DAS.DigitalEngagement.Application.Services.Interfaces;

namespace DAS.DigitalEngagement.Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IMessageSession _messageSession;

        public NotificationService(IMessageSession messageSession)
        {
            _messageSession = messageSession;
        }

        public Task Send(SendEmailCommand email)
        {
            return _messageSession.Send(email);
        }
    }
}
