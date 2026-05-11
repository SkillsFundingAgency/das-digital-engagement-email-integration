using System.Diagnostics.CodeAnalysis;

namespace DAS.DigitalEngagement.Models.Infrastructure
{
    [ExcludeFromCodeCoverage]
    public class ApplicationConfiguration
    {
        public required ConnectionString ConnectionString { get; set; }
        public Functions? Functions { get; set; }
        public required EmailMarketingApi EmailMarketingApi { get; set; }
        public required IList<DataMartSettings> DataMart { get; set; } = new List<DataMartSettings>();
        public required GovNotifyConfiguration GovNotifyConfiguration { get; set; }
    }
}
