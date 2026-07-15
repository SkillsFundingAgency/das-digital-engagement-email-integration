namespace DAS.DigitalEngagement.Models.Import
{
    public class ImportSummaryResult
    {
        public BatchStatus? Status { get; set; } // e.g., BatchStatus.Completed, BatchStatus.Failed, BatchStatus.Partial
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int TotalRecordsFromDb { get; set; }

        // Dynamically calculates total processed records from completed batches
        public int TotalRecordsProcessed
        {
            get
            {
                return BatchResults
                    .Where(b => b.Status == BatchStatus.Completed)
                    .Sum(b => b.RecordsProcessed);
            }
        }
        public int TotalRecordsReceived
        {
            get
            {
                return BatchResults
                    .Sum(b => b.RecordsReceived);
            }
        }
        public int TotalRecordsFailed
        {
            get
            {
                return BatchResults
                    .Sum(b => b.RecordsFailed);
            }
        }
        public bool IsPartiallyImported
        {
            get
            {
                return BatchResults.Any(b => b.IsPartiallyImported);
            }
        }
        public string? FieldMapping { get; set; }

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
                summary.AppendLine($"    RecordsReceived: {batch.RecordsReceived}, RecordsFailed: {batch.RecordsFailed}, IsPartiallyImported: {batch.IsPartiallyImported}");
                summary.AppendLine($"    AdditionalInfo: {batch.AdditionalInfo}");

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