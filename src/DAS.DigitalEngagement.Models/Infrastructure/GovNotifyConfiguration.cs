namespace DAS.DigitalEngagement.Models.Infrastructure
{
    public class GovNotifyConfiguration
    {
        public required string MonitoringReportTemplateId { get; set; }
        public required List<string> RecipientEmailAddresses { get; set; } = new();
    }
}
