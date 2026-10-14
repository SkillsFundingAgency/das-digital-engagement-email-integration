using DAS.DigitalEngagement.Models.PerformanceImport;

namespace DAS.DigitalEngagement.Application.PerformanceImport.Services;

public interface IPerformanceImportService
{
    Task ImportAsync(
        IReadOnlyCollection<SendEligibilityRecord> eligibleSends,
        DateTimeOffset importStart,
        CancellationToken cancellationToken = default);
}
