namespace DAS.DigitalEngagement.Models.Infrastructure
{
    public class GovNotifyConfiguration
    {
        public required string ApiKey { get; set; }
        public required string MonitoringReportTemplateId { get; set; }
        public required List<string> RecipientEmailAddresses { get; set; } = new();
    }
}
