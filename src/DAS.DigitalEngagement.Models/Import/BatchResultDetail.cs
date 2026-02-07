namespace DAS.DigitalEngagement.Models.Import
{
    public class BatchResultDetail
    {
        public  string? BatchId { get; set; }
        public required string Status { get; set; } // e.g., "Completed", "Failed", "Partial"
        public int RecordsProcessed { get; set; }
        public string? TokenFromEshot { get; set; }
        public string? Error { get; set; }
    }
}