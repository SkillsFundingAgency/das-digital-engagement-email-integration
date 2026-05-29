using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAS.DigitalEngagement.Application.Services.Interfaces
{
    public interface IEmailNotificationService
    {
        Task SendMonitoringReportAsync(string integrationName, string reportContent, string blobUrl, CancellationToken cancellationToken = default);
    }
}
