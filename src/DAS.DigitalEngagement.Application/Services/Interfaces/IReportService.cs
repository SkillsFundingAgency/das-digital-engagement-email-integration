using DAS.DigitalEngagement.Models.Import;

namespace DAS.DigitalEngagement.Application.Services.Interfaces
{
    public interface IReportService
    {
        string CreateImportSummaryReport(ImportSummaryResult summary);
        Task SaveReportToBlobAsync(string reportContent, string fileName);
        Task<string> SaveReportToBlobInternalAsync(string reportContent, string fileName);
    }
}