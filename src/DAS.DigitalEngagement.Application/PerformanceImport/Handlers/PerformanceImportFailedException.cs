using DAS.DigitalEngagement.Models.PerformanceImport;

namespace DAS.DigitalEngagement.Application.PerformanceImport.Handlers;

public sealed class PerformanceImportFailedException : Exception
{
    public PerformanceImportReportSummary Summary { get; }

    public PerformanceImportFailedException(
        PerformanceImportReportSummary summary,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        Summary = summary;
    }
}
