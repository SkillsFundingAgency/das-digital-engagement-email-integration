namespace DAS.DigitalEngagement.Models.Import
{
    public class BatchResultDetail
    {
        public string? BatchId { get; set; }
        public required BatchStatus Status { get; set; }
        public int RecordsProcessed { get; set; }
        public int RecordsReceived { get; set; }
        public int RecordsFailed { get; set; } = 0;
        public bool IsPartiallyImported { get; set; } = false;
        public string? TokenFromEshot { get; set; }
        public string? Error { get; set; }
        public string? AdditionalInfo { get; set; }
    }
}