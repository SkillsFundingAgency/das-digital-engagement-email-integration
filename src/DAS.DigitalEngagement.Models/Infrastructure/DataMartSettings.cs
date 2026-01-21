using System.Diagnostics.CodeAnalysis;

namespace DAS.DigitalEngagement.Models.Infrastructure
{
    [ExcludeFromCodeCoverage]
    public class DataMartSettings
    {
        public string? ViewName { get; set; }
        public string? ObjectName { get; set; }
        public string? ConfigFileLocation { get; set; }
        public string? Config { get; set; }

    }
}
