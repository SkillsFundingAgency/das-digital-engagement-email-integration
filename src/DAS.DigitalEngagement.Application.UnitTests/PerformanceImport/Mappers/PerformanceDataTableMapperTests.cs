using System;
using System.Collections.Generic;
using DAS.DigitalEngagement.Application.PerformanceImport.Mappers;
using DAS.DigitalEngagement.Models.PerformanceImport;

namespace DAS.DigitalEngagement.Application.UnitTests.PerformanceImport.Mappers;

[TestFixture]
public class PerformanceDataTableMapperTests
{
    [Test]
    public void CreateContacts_AddsExpectedColumnsAndValues()
    {
        var table = PerformanceDataTableMapper.CreateContacts([
            new ContactApiRecord
            {
                Id = 1,
                SubaccountId = 123,
                SalesforceContactId = "SC-1",
                SalesforceLeadId = "SL-2",
                MicrosoftDynamicsContactId = "DC-3",
                MicrosoftDynamicsLeadId = "DL-4",
                SourceId = 10,
                EmailFormat = "HTML",
                EmailFormatId = 1,
                Email = "test@example.com",
                EmailStatusId = 0,
                MobileStatusId = 3,
                MobileNumber = "07123456789",
                MobileRegion = 44,
                Salutation = "Ms",
                FirstName = "Test",
                LastName = "User",
                Telephone = "0123456789",
                Urn = "URN-1",
                Company = "Example Ltd",
                CreatedDate = DateTimeOffset.Parse("2025-09-16T14:01:00.127Z"),
                CreatedFromSourceId = 15,
                ModifiedDate = DateTimeOffset.Parse("2026-09-16T10:51:13.137Z"),
                ModifiedFromSourceId = 2,
                EmailUnsubscribedDate = null,
                EmailUnsubscribedFromSourceId = null,
                SmsUnsubscribedDate = null,
                SmsUnsubscribedFromSourceId = null,
                EmailHardBounceDate = null,
                DoubleOptInConfirmationDate = null,
                InleadsScore = 0,
                CustomerAccountId = 4,
                EmailEngagementStatus = 3,
                HardBouncedDate = null,
                LastSoftBouncedDate = null,
                LastSentDate = DateTimeOffset.Parse("2026-09-02T13:31:03Z"),
                LastDisplayedDate = DateTimeOffset.Parse("2026-09-02T12:33:16.847Z"),
                LastClickedDate = DateTimeOffset.Parse("2026-09-02T12:32:30.357Z"),
                FirstInleadsVisitDate = null,
                LastInleadsVisitDate = null,
                IncludeInUr = true
            }]);

        Assert.That(table.Columns.Contains("SubaccountID"), Is.True);
        Assert.That(table.Columns.Contains("SalesforceContactID"), Is.True);
        Assert.That(table.Columns.Contains("ModifiedFromSourceID"), Is.True);
        Assert.That(table.Columns.Contains("IncludeInUR"), Is.True);
        Assert.That(table.Rows.Count, Is.EqualTo(1));
        Assert.That(table.Rows[0]["SubaccountID"], Is.EqualTo(123));
        Assert.That(table.Rows[0]["SalesforceContactID"], Is.EqualTo("SC-1"));
        Assert.That(table.Rows[0]["Email"], Is.EqualTo("test@example.com"));
        Assert.That(table.Rows[0]["CreatedFromSourceID"], Is.EqualTo("15"));
        Assert.That(table.Rows[0]["ModifiedFromSourceID"], Is.EqualTo("2"));
        Assert.That(table.Rows[0]["LastSentDate"], Is.EqualTo(DateTime.Parse("2026-09-02T13:31:03Z").ToUniversalTime()));
        Assert.That(table.Rows[0]["IncludeInUR"], Is.EqualTo(true));
    }

    [Test]
    public void CreateSendContacts_AddsSubaccountIdColumnAndValue()
    {
        var table = PerformanceDataTableMapper.CreateSendContacts([
            new SendContactApiRecord
            {
                Id = 1,
                SubaccountId = 123,
                SendId = 10,
                ContactId = 20,
                PublicIp = "127.0.0.1"
            }]);

        Assert.That(table.Columns.Contains("SubaccountID"), Is.True);
        Assert.That(table.Columns.Contains("SendID"), Is.True);
        Assert.That(table.Rows.Count, Is.EqualTo(1));
        Assert.That(table.Rows[0]["SubaccountID"], Is.EqualTo(123));
        Assert.That(table.Rows[0]["SendID"], Is.EqualTo(10));
        Assert.That(table.Rows[0]["ContactID"], Is.EqualTo(20));
        Assert.That(table.Rows[0]["PublicIP"], Is.EqualTo("127.0.0.1"));
    }

    [Test]
    public void CreateLinks_MapsLinkValues()
    {
        var table = PerformanceDataTableMapper.CreateLinks([
            new LinkApiRecord
            {
                Id = 1,
                SendId = 10,
                Url = "https://example.com",
                FriendlyName = "Example",
                IsMonitored = true,
                ReceivedInMessageFormat = "html"
            }]);

        Assert.That(table.Columns.Contains("SubaccountID"), Is.False);
        Assert.That(table.Rows.Count, Is.EqualTo(1));
        Assert.That(table.Rows[0]["ID"], Is.EqualTo(1));
        Assert.That(table.Rows[0]["SendID"], Is.EqualTo(10));
        Assert.That(table.Rows[0]["URL"], Is.EqualTo("https://example.com"));
        Assert.That(table.Rows[0]["FriendlyName"], Is.EqualTo("Example"));
        Assert.That(table.Rows[0]["IsMonitored"], Is.EqualTo(true));
        Assert.That(table.Rows[0]["ReceivedInMessageFormat"], Is.EqualTo("html"));
    }

    [Test]
    public void CreateImportMetadata_UsesDefaultCompletion_WhenNoCompletenessMapProvided()
    {
        var importStart = DateTimeOffset.Parse("2025-10-14T12:24:56.723Z");

        var table = PerformanceDataTableMapper.CreateImportMetadata([
            new Send
            {
                Id = 12,
                CampaignId = 34
            }], importStart, null);

        Assert.That(table.Columns.Contains("SendID"), Is.True);
        Assert.That(table.Columns.Contains("CampaignID"), Is.True);
        Assert.That(table.Columns.Contains("IsImportComplete"), Is.True);
        Assert.That(table.Columns.Contains("ImportStartDate"), Is.True);
        Assert.That(table.Columns.Contains("ImportEndDate"), Is.True);
        Assert.That(table.Rows.Count, Is.EqualTo(1));
        Assert.That(table.Rows[0]["SendID"], Is.EqualTo(12));
        Assert.That(table.Rows[0]["CampaignID"], Is.EqualTo(34));
        Assert.That(table.Rows[0]["IsImportComplete"], Is.EqualTo(true));
        Assert.That(table.Rows[0]["ImportStartDate"], Is.EqualTo(importStart.UtcDateTime));
        Assert.That(table.Rows[0]["ImportEndDate"], Is.InstanceOf<DateTime>());
    }

    [Test]
    public void CreateImportMetadata_UsesCompletenessMap_WhenProvided()
    {
        var importStart = DateTimeOffset.Parse("2025-10-14T12:24:56.723Z");

        var table = PerformanceDataTableMapper.CreateImportMetadata([
            new Send
            {
                Id = 12,
                CampaignId = 34
            },
            new Send
            {
                Id = 13,
                CampaignId = 35
            }], importStart, new Dictionary<long, bool>
        {
            [12] = true,
            [13] = false
        });

        Assert.That(table.Rows.Count, Is.EqualTo(2));
        Assert.That(table.Rows[0]["SendID"], Is.EqualTo(12));
        Assert.That(table.Rows[0]["IsImportComplete"], Is.EqualTo(true));
        Assert.That(table.Rows[1]["SendID"], Is.EqualTo(13));
        Assert.That(table.Rows[1]["IsImportComplete"], Is.EqualTo(false));
    }

    [Test]
    public void CreateUserAgents_AddsExpectedColumnsAndValues()
    {
        var table = PerformanceDataTableMapper.CreateUserAgents([
            new UserAgentApiRecord
            {
                Id = 7,
                SubaccountId = 123,
                SendContactId = 10,
                IpAddress = "127.0.0.1",
                ClientName = "Chrome",
                ClientType = "Browser",
                ClientFamily = "Chromium",
                Device = "Desktop",
                OperatingSystemFamily = "Windows",
                OperatingSystem = "Windows 11",
                DisplayDate = DateTimeOffset.Parse("2025-10-14T12:24:56.723Z"),
                IsSuspectedBot = false
            }]);

        Assert.That(table.Columns.Contains("ID"), Is.True);
        Assert.That(table.Columns.Contains("SubaccountID"), Is.True);
        Assert.That(table.Columns.Contains("SendContactID"), Is.True);
        Assert.That(table.Columns.Contains("DisplayDate"), Is.True);
        Assert.That(table.Columns.Contains("IsSuspectedBOT"), Is.True);
        Assert.That(table.Rows.Count, Is.EqualTo(1));
        Assert.That(table.Rows[0]["ID"], Is.EqualTo(7));
        Assert.That(table.Rows[0]["SubaccountID"], Is.EqualTo(123));
        Assert.That(table.Rows[0]["SendContactID"], Is.EqualTo(10));
        Assert.That(table.Rows[0]["IPAddress"], Is.EqualTo("127.0.0.1"));
        Assert.That(table.Rows[0]["ClientName"], Is.EqualTo("Chrome"));
        Assert.That(table.Rows[0]["IsSuspectedBOT"], Is.EqualTo(false));
    }

    [Test]
    public void CreateDisplayedContacts_AddsExpectedColumnsAndValues()
    {
        var table = PerformanceDataTableMapper.CreateDisplayedContacts([
            new DisplayedContactApiRecord
            {
                Id = 8,
                SubaccountId = 123,
                SendContactId = 11,
                UserAgentId = 7,
                DisplayDate = DateTimeOffset.Parse("2025-10-14T12:24:56.723Z"),
                Format = "HTML",
                TimeInSecondsSpentReadingEmail = 12,
                IsSuspectedBot = true
            }]);

        Assert.That(table.Columns.Contains("ID"), Is.True);
        Assert.That(table.Columns.Contains("SubaccountID"), Is.True);
        Assert.That(table.Columns.Contains("UserAgentID"), Is.True);
        Assert.That(table.Columns.Contains("IsSuspectedBOT"), Is.True);
        Assert.That(table.Rows.Count, Is.EqualTo(1));
        Assert.That(table.Rows[0]["ID"], Is.EqualTo(8));
        Assert.That(table.Rows[0]["SubaccountID"], Is.EqualTo(123));
        Assert.That(table.Rows[0]["SendContactID"], Is.EqualTo(11));
        Assert.That(table.Rows[0]["UserAgentID"], Is.EqualTo(7));
        Assert.That(table.Rows[0]["Format"], Is.EqualTo("HTML"));
        Assert.That(table.Rows[0]["TimeInSecondsSpentReadingEmail"], Is.EqualTo(12));
        Assert.That(table.Rows[0]["IsSuspectedBOT"], Is.EqualTo(true));
    }

    [Test]
    public void CreateClickedContacts_AddsExpectedColumnsAndValues()
    {
        var table = PerformanceDataTableMapper.CreateClickedContacts([
            new ClickedContactApiRecord
            {
                Id = 9,
                SubaccountId = 123,
                SendContactId = 12,
                LinkId = 99,
                UserAgentId = 7,
                ClickDate = DateTimeOffset.Parse("2025-10-14T12:24:56.723Z"),
                FriendlyName = "Example",
                IsSuspectedBot = false
            }]);

        Assert.That(table.Columns.Contains("ID"), Is.True);
        Assert.That(table.Columns.Contains("SubaccountID"), Is.True);
        Assert.That(table.Columns.Contains("LinkID"), Is.True);
        Assert.That(table.Columns.Contains("IsSuspectedBOT"), Is.True);
        Assert.That(table.Rows.Count, Is.EqualTo(1));
        Assert.That(table.Rows[0]["ID"], Is.EqualTo(9));
        Assert.That(table.Rows[0]["SubaccountID"], Is.EqualTo(123));
        Assert.That(table.Rows[0]["SendContactID"], Is.EqualTo(12));
        Assert.That(table.Rows[0]["LinkID"], Is.EqualTo(99));
        Assert.That(table.Rows[0]["UserAgentID"], Is.EqualTo(7));
        Assert.That(table.Rows[0]["FriendlyName"], Is.EqualTo("Example"));
        Assert.That(table.Rows[0]["IsSuspectedBOT"], Is.EqualTo(false));
    }

    [Test]
    public void CreateBouncedContacts_AddsSubaccountIdColumnAndValue()
    {
        var table = PerformanceDataTableMapper.CreateBouncedContacts([
            new BouncedContactApiRecord
            {
                Id = 1,
                SubaccountId = 123,
                SendContactId = 10,
                BounceReason = "Reason",
                BounceType = "Type",
                BounceTypeId = 2,
                BounceDate = DateTimeOffset.UtcNow,
                ResponseText = "Response"
            }]);

        Assert.That(table.Columns.Contains("SubaccountID"), Is.True);
        Assert.That(table.Columns.Contains("BounceTypeID"), Is.True);
        Assert.That(table.Rows.Count, Is.EqualTo(1));
        Assert.That(table.Rows[0]["SubaccountID"], Is.EqualTo(123));
        Assert.That(table.Rows[0]["SendContactID"], Is.EqualTo(10));
        Assert.That(table.Rows[0]["BounceReason"], Is.EqualTo("Reason"));
        Assert.That(table.Rows[0]["BounceType"], Is.EqualTo("Type"));
        Assert.That(table.Rows[0]["BounceTypeID"], Is.EqualTo(2));
        Assert.That(table.Rows[0]["ResponseText"], Is.EqualTo("Response"));
    }

    [Test]
    public void CreateUnsubscribedContacts_AddsSubaccountIdColumnAndValue()
    {
        var table = PerformanceDataTableMapper.CreateUnsubscribedContacts([
            new UnsubscribedContactApiRecord
            {
                Id = 1,
                SubaccountId = 123,
                SendContactId = 10,
                UnsubscribeDate = DateTimeOffset.UtcNow,
                IsGlobalUnsubscribe = true,
                IsComplaint = false
            }]);

        Assert.That(table.Columns.Contains("SubaccountID"), Is.True);
        Assert.That(table.Rows.Count, Is.EqualTo(1));
        Assert.That(table.Rows[0]["SubaccountID"], Is.EqualTo(123));
        Assert.That(table.Rows[0]["SendContactID"], Is.EqualTo(10));
        Assert.That(table.Rows[0]["IsGlobalUnsubscribe"], Is.EqualTo(true));
        Assert.That(table.Rows[0]["IsComplaint"], Is.EqualTo(false));
    }
}
