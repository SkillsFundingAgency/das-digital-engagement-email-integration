using DAS.DigitalEngagement.Models.Import;

namespace DAS.DigitalEngagement.Application.Services.Interfaces
{
    public interface IReportService
    {
        string CreateImportSummaryReport(ImportSummaryResult summary);
        Task SaveReportToBlobAndNotifyAsync(string reportContent, string fileName, string integrationName);
        Task<string> SaveReportToBlobInternalAsync(string reportContent, string fileName);
    }
}