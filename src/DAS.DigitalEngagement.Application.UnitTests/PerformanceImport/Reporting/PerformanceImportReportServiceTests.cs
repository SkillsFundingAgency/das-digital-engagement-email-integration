using System;
using System.Collections.Generic;
using DAS.DigitalEngagement.Application.Services;
using DAS.DigitalEngagement.Models.PerformanceImport;

namespace DAS.DigitalEngagement.Application.UnitTests.PerformanceImport.Reporting;

[TestFixture]
public class PerformanceImportReportServiceTests
{
    [Test]
    public void CreatePerformanceImportReport_WhenNoCountsOrMessages_OutputsNoneSections()
    {
        var sut = new PerformanceImportReportService();
        var summary = new PerformanceImportReportSummary
        {
            Status = PerformanceImportStatus.Completed,
            StartedAtUtc = new DateTimeOffset(2025, 1, 1, 8, 0, 0, TimeSpan.Zero),
            EndedAtUtc = new DateTimeOffset(2025, 1, 1, 9, 0, 0, TimeSpan.Zero),
            EligibleSends = 10,
            ImportedSendRecords = 9,
            ImportedCampaignRecords = 8,
            ImportedSendContactRecords = 7,
            ImportedContactRecords = 6,
            ImportedLinkRecords = 5,
            ImportedUserAgentRecords = 4,
            ImportedDisplayedContactRecords = 3,
            ImportedClickedContactRecords = 2,
            ImportedBouncedContactRecords = 1,
            ImportedUnsubscribedContactRecords = 0,
            FailureCount = 1,
            ODataAvailableCounts = new Dictionary<string, long?>(),
            Messages = Array.Empty<string>()
        };

        var report = sut.CreatePerformanceImportReport(summary);

        Assert.Multiple(() =>
        {
            Assert.That(report, Does.Contain("Status: Completed"));
            Assert.That(report, Does.Contain("Eligible Sends: 10"));
            Assert.That(report, Does.Contain("Failure Count: 1"));
            Assert.That(report, Does.Contain("  None available."));
            Assert.That(report, Does.Contain("Messages: None"));
        });
    }

    [Test]
    public void CreatePerformanceImportReport_WhenCountsAndMessagesProvided_OutputsKnownThenUnknownCounts()
    {
        var sut = new PerformanceImportReportService();
        var summary = new PerformanceImportReportSummary
        {
            ODataAvailableCounts = new Dictionary<string, long?>
            {
                [PerformanceImportResourceNames.Campaigns] = 22,
                [PerformanceImportResourceNames.Sends] = 11,
                [PerformanceImportResourceNames.UserAgents] = null,
                ["ZCustom"] = 99,
                ["AlphaCustom"] = 1
            },
            Messages = new[] { "First message", "Second message" }
        };

        var report = sut.CreatePerformanceImportReport(summary);

        var sendsIndex = report.IndexOf("  Sends: 11", StringComparison.Ordinal);
        var campaignsIndex = report.IndexOf("  Campaigns: 22", StringComparison.Ordinal);
        var userAgentsIndex = report.IndexOf("  UserAgents: Not available on this endpoint", StringComparison.Ordinal);
        var alphaCustomIndex = report.IndexOf("  AlphaCustom: 1", StringComparison.Ordinal);
        var zCustomIndex = report.IndexOf("  ZCustom: 99", StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(sendsIndex, Is.GreaterThan(-1));
            Assert.That(campaignsIndex, Is.GreaterThan(sendsIndex));
            Assert.That(userAgentsIndex, Is.GreaterThan(campaignsIndex));
            Assert.That(alphaCustomIndex, Is.GreaterThan(userAgentsIndex));
            Assert.That(zCustomIndex, Is.GreaterThan(alphaCustomIndex));
            Assert.That(report, Does.Contain("Messages:"));
            Assert.That(report, Does.Contain("  - First message"));
            Assert.That(report, Does.Contain("  - Second message"));
        });
    }
}