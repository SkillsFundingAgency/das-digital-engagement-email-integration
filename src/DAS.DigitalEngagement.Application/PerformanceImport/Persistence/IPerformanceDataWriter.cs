using DAS.DigitalEngagement.Models.PerformanceImport;

namespace DAS.DigitalEngagement.Application.PerformanceImport.Persistence;

public interface IPerformanceDataWriter
{
    Task WriteContactsAsync(IReadOnlyCollection<ContactApiRecord> records, CancellationToken cancellationToken = default);
    Task WriteSendContactsAsync(IReadOnlyCollection<SendContactApiRecord> records, CancellationToken cancellationToken = default);
    Task WriteLinksAsync(IReadOnlyCollection<LinkApiRecord> records, CancellationToken cancellationToken = default);
    Task WriteUserAgentsAsync(IReadOnlyCollection<UserAgentApiRecord> records, CancellationToken cancellationToken = default);
    Task WriteDisplayedContactsAsync(IReadOnlyCollection<DisplayedContactApiRecord> records, CancellationToken cancellationToken = default);
    Task WriteClickedContactsAsync(IReadOnlyCollection<ClickedContactApiRecord> records, CancellationToken cancellationToken = default);
    Task WriteBouncedContactsAsync(IReadOnlyCollection<BouncedContactApiRecord> records, CancellationToken cancellationToken = default);
    Task WriteUnsubscribedContactsAsync(IReadOnlyCollection<UnsubscribedContactApiRecord> records, CancellationToken cancellationToken = default);
    Task WriteImportCompletionsAsync(IReadOnlyCollection<Send> sends, DateTimeOffset importStart, CancellationToken cancellationToken = default);
}
