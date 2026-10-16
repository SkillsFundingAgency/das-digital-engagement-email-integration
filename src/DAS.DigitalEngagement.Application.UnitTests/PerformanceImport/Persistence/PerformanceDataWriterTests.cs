using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using DAS.DigitalEngagement.Application.PerformanceImport.Persistence;
using DAS.DigitalEngagement.CampaignInterest.Data.Service;
using DAS.DigitalEngagement.Models.PerformanceImport;
using Moq;

namespace DAS.DigitalEngagement.EmailIntegration.UnitTests.PerformanceImport.Persistence;

[TestFixture]
public class PerformanceDataWriterTests
{
    private Mock<ISqlBulkInserter> _bulkInserterMock = null!;

    [SetUp]
    public void SetUp()
    {
        _bulkInserterMock = new Mock<ISqlBulkInserter>();
        _bulkInserterMock
            .Setup(x => x.BulkInsertIgnoringDuplicatesAsync(
                It.IsAny<string>(),
                It.IsAny<DataTable>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _bulkInserterMock
            .Setup(x => x.BulkInsertAsync(
                It.IsAny<string>(),
                It.IsAny<DataTable>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Test]
    public async Task WriteContactsAsync_WritesContactsTableUsingIgnoreDuplicates()
    {
        var sut = CreateSut();
        var records = new List<ContactApiRecord>
        {
            new()
            {
                Id = 1,
                SubaccountId = 2,
                Email = "test@example.com",
                IncludeInUr = true
            }
        };

        await sut.WriteContactsAsync(records);

        _bulkInserterMock.Verify(x => x.BulkInsertIgnoringDuplicatesAsync(
            "import.Contacts",
            It.Is<DataTable>(t => t.Rows.Count == 1 && t.Rows[0]["Email"].Equals("test@example.com")),
            records.Count,
            300,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task WriteSendContactsAsync_WritesSendContactsTableUsingIgnoreDuplicates()
    {
        var sut = CreateSut();
        var records = new List<SendContactApiRecord>
        {
            new()
            {
                Id = 1,
                SubaccountId = 2,
                SendId = 10,
                ContactId = 20,
                PublicIp = "127.0.0.1"
            }
        };

        await sut.WriteSendContactsAsync(records);

        _bulkInserterMock.Verify(x => x.BulkInsertIgnoringDuplicatesAsync(
            "import.SendContacts",
            It.Is<DataTable>(t => t.Rows.Count == 1 && t.Rows[0]["SendID"].Equals(10) && t.Rows[0]["ContactID"].Equals(20)),
            records.Count,
            300,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task WriteLinksAsync_WritesLinksTableUsingIgnoreDuplicates()
    {
        var sut = CreateSut();
        var records = new List<LinkApiRecord>
        {
            new()
            {
                Id = 1,
                SendId = 10,
                Url = "https://example.com",
                FriendlyName = "Example"
            }
        };

        await sut.WriteLinksAsync(records);

        _bulkInserterMock.Verify(x => x.BulkInsertIgnoringDuplicatesAsync(
            "import.Links",
            It.Is<DataTable>(t => t.Rows.Count == 1 && t.Rows[0]["URL"].Equals("https://example.com")),
            records.Count,
            300,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task WriteUserAgentsAsync_WritesUserAgentsTableUsingIgnoreDuplicates()
    {
        var sut = CreateSut();
        var records = new List<UserAgentApiRecord>
        {
            new()
            {
                Id = 1,
                SubaccountId = 2,
                SendContactId = 20,
                ClientName = "Chrome"
            }
        };

        await sut.WriteUserAgentsAsync(records);

        _bulkInserterMock.Verify(x => x.BulkInsertIgnoringDuplicatesAsync(
            "import.UserAgents",
            It.Is<DataTable>(t => t.Rows.Count == 1 && t.Rows[0]["ClientName"].Equals("Chrome")),
            records.Count,
            300,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task WriteDisplayedContactsAsync_WritesDisplayedContactsTableUsingIgnoreDuplicates()
    {
        var sut = CreateSut();
        var records = new List<DisplayedContactApiRecord>
        {
            new()
            {
                Id = 1,
                SubaccountId = 2,
                SendContactId = 20,
                DisplayDate = DateTimeOffset.UtcNow,
                Format = "HTML"
            }
        };

        await sut.WriteDisplayedContactsAsync(records);

        _bulkInserterMock.Verify(x => x.BulkInsertIgnoringDuplicatesAsync(
            "import.DisplayedContacts",
            It.Is<DataTable>(t => t.Rows.Count == 1 && t.Rows[0]["Format"].Equals("HTML")),
            records.Count,
            300,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task WriteClickedContactsAsync_WritesClickedContactsTableUsingIgnoreDuplicates()
    {
        var sut = CreateSut();
        var records = new List<ClickedContactApiRecord>
        {
            new()
            {
                Id = 1,
                SubaccountId = 2,
                SendContactId = 20,
                LinkId = 30,
                FriendlyName = "Example"
            }
        };

        await sut.WriteClickedContactsAsync(records);

        _bulkInserterMock.Verify(x => x.BulkInsertIgnoringDuplicatesAsync(
            "import.ClickedContacts",
            It.Is<DataTable>(t => t.Rows.Count == 1 && t.Rows[0]["FriendlyName"].Equals("Example")),
            records.Count,
            300,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task WriteBouncedContactsAsync_WritesBouncedContactsTableUsingIgnoreDuplicates()
    {
        var sut = CreateSut();
        var records = new List<BouncedContactApiRecord>
        {
            new()
            {
                Id = 1,
                SubaccountId = 2,
                SendContactId = 20,
                BounceReason = "Reason",
                BounceType = "Type",
                BounceTypeId = 3
            }
        };

        await sut.WriteBouncedContactsAsync(records);

        _bulkInserterMock.Verify(x => x.BulkInsertIgnoringDuplicatesAsync(
            "import.BouncedContacts",
            It.Is<DataTable>(t => t.Rows.Count == 1 && t.Rows[0]["BounceTypeID"].Equals(3)),
            records.Count,
            300,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task WriteUnsubscribedContactsAsync_WritesUnsubscribedContactsTableUsingIgnoreDuplicates()
    {
        var sut = CreateSut();
        var records = new List<UnsubscribedContactApiRecord>
        {
            new()
            {
                Id = 1,
                SubaccountId = 2,
                SendContactId = 20,
                IsGlobalUnsubscribe = true,
                IsComplaint = false
            }
        };

        await sut.WriteUnsubscribedContactsAsync(records);

        _bulkInserterMock.Verify(x => x.BulkInsertIgnoringDuplicatesAsync(
            "import.UnsubscribedContacts",
            It.Is<DataTable>(t => t.Rows.Count == 1 && t.Rows[0]["IsGlobalUnsubscribe"].Equals(true)),
            records.Count,
            300,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task WriteImportCompletionsAsync_WritesCampaignImportMetadataUsingBulkInsert()
    {
        var sut = CreateSut();
        var importStart = DateTimeOffset.Parse("2025-10-14T12:24:56.723Z");
        var sends = new List<Send>
        {
            new()
            {
                Id = 1,
                CampaignId = 2
            }
        };
        var importCompleteness = new Dictionary<long, bool>
        {
            [2] = true
        };

        await sut.WriteImportCompletionsAsync(sends, importStart, importCompleteness);

        _bulkInserterMock.Verify(x => x.BulkInsertAsync(
            "import.CampaignImportMetadata",
            It.Is<DataTable>(t => t.Rows.Count == 1 && t.Rows[0]["SendID"].Equals(1) && t.Rows[0]["CampaignID"].Equals(2)),
            sends.Count,
            300,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task WriteContactsAsync_WhenEmpty_DoesNotCallInserter()
    {
        var sut = CreateSut();

        await sut.WriteContactsAsync(Array.Empty<ContactApiRecord>());

        _bulkInserterMock.Verify(x => x.BulkInsertIgnoringDuplicatesAsync(
            It.IsAny<string>(),
            It.IsAny<DataTable>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private PerformanceDataWriter CreateSut() => new(_bulkInserterMock.Object);
}