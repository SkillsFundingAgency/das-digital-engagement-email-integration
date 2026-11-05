using DAS.DigitalEngagement.Models.PerformanceImport;

namespace DAS.DigitalEngagement.Application.Services.Interfaces;

public interface IPerformanceImportReportService
{
    string CreatePerformanceImportReport(PerformanceImportReportSummary summary);
}
