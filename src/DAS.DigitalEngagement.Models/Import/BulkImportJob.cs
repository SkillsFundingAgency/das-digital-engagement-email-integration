using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace DAS.DigitalEngagement.Models.Import
{
    [ExcludeFromCodeCoverage]
    public class BulkImportJob
    {
        public int batchId { get; set; }
        public required string ImportId { get; set; }
        public required string Status { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("BulkImportJob {\n");
            sb.Append("  BatchId: ").Append(batchId).Append("\n");
            sb.Append("  ImportId: ").Append(ImportId).Append("\n");
            sb.Append("  Status: ").Append(Status).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }
    }
}