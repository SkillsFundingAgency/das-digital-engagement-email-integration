using System.Diagnostics.CodeAnalysis;

namespace DAS.DigitalEngagement.Models.Infrastructure
{
    [ExcludeFromCodeCoverage]
    public class DataMartSettings
    {
        public required string ViewName { get; set; }
        public required string ObjectName { get; set; }
        public required string FieldMapping { get; set; }
        public required int[] TemplatedUploadId { get; set; }

    }
}
