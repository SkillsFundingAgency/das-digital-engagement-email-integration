namespace DAS.DigitalEngagement.Models.Import
{
    public enum ImportStatus
    {
        Queued = 0,
        Processing = 1,
        Completed = 2,
        Failed = 3,
        ValidationFailed = 4
    }
}