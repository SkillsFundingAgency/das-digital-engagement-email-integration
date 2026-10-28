using System.Text;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.Models.PerformanceImport;

namespace DAS.DigitalEngagement.Application.Services;

public sealed class PerformanceImportReportService : IPerformanceImportReportService
{
    public string CreatePerformanceImportReport(PerformanceImportReportSummary summary)
    {
        var sb = new StringBuilder();
        sb.AppendLine("################################################################################");
        sb.AppendLine("#################### Performance Import Summary Report #########################");
        sb.AppendLine("################################################################################");
        sb.AppendLine($"Status: {summary.Status}");
        sb.AppendLine($"Start Time: {summary.StartedAtUtc:O}");
        sb.AppendLine($"End Time: {summary.EndedAtUtc:O}");
        sb.AppendLine($"Eligible Sends: {summary.EligibleSends}");
        sb.AppendLine();
        sb.AppendLine("Imported Core Records:");
        sb.AppendLine($"Imported Send Records: {summary.ImportedSendRecords}");
        sb.AppendLine($"Imported Campaign Records: {summary.ImportedCampaignRecords}");
        sb.AppendLine($"Imported SendContact Records: {summary.ImportedSendContactRecords}");
        sb.AppendLine($"Imported Contact Records: {summary.ImportedContactRecords}");
        sb.AppendLine();
        sb.AppendLine("Imported Interaction Records:");
        sb.AppendLine($"Imported Link Records: {summary.ImportedLinkRecords}");
        sb.AppendLine($"Imported UserAgent Records: {summary.ImportedUserAgentRecords}");
        sb.AppendLine($"Imported DisplayedContact Records: {summary.ImportedDisplayedContactRecords}");
        sb.AppendLine($"Imported ClickedContact Records: {summary.ImportedClickedContactRecords}");
        sb.AppendLine($"Imported BouncedContact Records: {summary.ImportedBouncedContactRecords}");
        sb.AppendLine($"Imported UnsubscribedContact Records: {summary.ImportedUnsubscribedContactRecords}");
        sb.AppendLine();
        sb.AppendLine($"Failure Count: {summary.FailureCount}");
        sb.AppendLine();
        sb.AppendLine("API Available Counts (@odata.count when provided):");

        if (summary.ODataAvailableCounts.Count == 0)
        {
            sb.AppendLine("  None available.");
        }
        else
        {
            var orderedKeys = new[]
            {
                PerformanceImportResourceNames.Sends,
                PerformanceImportResourceNames.Campaigns,
                PerformanceImportResourceNames.SendContacts,
                PerformanceImportResourceNames.Contacts,
                PerformanceImportResourceNames.Links,
                PerformanceImportResourceNames.UserAgents,
                PerformanceImportResourceNames.DisplayedContacts,
                PerformanceImportResourceNames.ClickedContacts,
                PerformanceImportResourceNames.BouncedContacts,
                PerformanceImportResourceNames.UnsubscribedContacts
            };

            foreach (var key in orderedKeys)
            {
                if (!summary.ODataAvailableCounts.TryGetValue(key, out var value))
                {
                    continue;
                }

                sb.AppendLine($"  {key}: {(value.HasValue ? value.Value.ToString() : "Not available on this endpoint")}");
            }

            foreach (var entry in summary.ODataAvailableCounts.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
            {
                if (orderedKeys.Contains(entry.Key, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                sb.AppendLine($"  {entry.Key}: {(entry.Value.HasValue ? entry.Value.Value.ToString() : "Not available on this endpoint")}");
            }
        }

        sb.AppendLine();
        if (summary.Messages.Count == 0)
        {
            sb.AppendLine("Messages: None");
        }
        else
        {
            sb.AppendLine("Messages:");
            foreach (var message in summary.Messages)
            {
                sb.AppendLine($"  - {message}");
            }
        }

        sb.AppendLine("################################################################################");
        return sb.ToString();
    }
}
