using DAS.DigitalEngagement.Models.Import;

namespace DAS.DigitalEngagement.Application.Services.Interfaces
{
    public interface IReportService
    {
        string CreateImportSummaryReport(ImportSummaryResult summary);
        Task SaveReportToBlob(string reportContent, string fileName);
        Task SaveReportToBlobAndNotifyAsync(string reportContent, string fileName, string integrationName);
    }
}