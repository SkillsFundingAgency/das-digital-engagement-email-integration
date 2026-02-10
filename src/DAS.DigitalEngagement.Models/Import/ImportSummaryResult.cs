namespace DAS.DigitalEngagement.Models.Import
{
    public class ImportSummaryResult
    {
        public  string? Status { get; set; } // e.g., "Completed", "Failed", "Partial"
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int TotalRecordsFromDb { get; set; }

        // Dynamically calculates total processed records from completed batches
        public int TotalRecordsProcessed
        {
            get
            {
                return BatchResults
                    .Where(b => b.Status == "Completed")
                    .Sum(b => b.RecordsProcessed);
            }
        }

        public List<BatchResultDetail> BatchResults { get; set; } = new();
        public List<string> Messages { get; set; } = new();

        public override string ToString()
        {
            var summary = new System.Text.StringBuilder();
            summary.AppendLine($"Status: {Status}");
            summary.AppendLine($"StartTime: {StartTime:O}");
            summary.AppendLine($"EndTime: {EndTime:O}");
            summary.AppendLine($"TotalRecordsFromDb: {TotalRecordsFromDb}");
            summary.AppendLine($"TotalRecordsProcessed: {TotalRecordsProcessed}");
            summary.AppendLine("BatchResults:");
            foreach (var batch in BatchResults)
            {
                summary.AppendLine($"  - Status: {batch.Status}, RecordsProcessed: {batch.RecordsProcessed}");
            }
            summary.AppendLine("Messages:");
            foreach (var msg in Messages)
            {
                summary.AppendLine($"  - {msg}");
            }
            return summary.ToString();
        }
    }
}