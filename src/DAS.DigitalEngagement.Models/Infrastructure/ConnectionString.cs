using System.Diagnostics.CodeAnalysis;

namespace DAS.DigitalEngagement.Models.Infrastructure
{
    [ExcludeFromCodeCoverage]
    public class ConnectionString
    {
        public required string DataMart { get; set; }
    }
}
