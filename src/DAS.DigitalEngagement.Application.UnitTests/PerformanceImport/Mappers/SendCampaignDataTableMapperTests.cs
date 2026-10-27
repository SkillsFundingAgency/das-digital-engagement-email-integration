using System;
using System.Collections.Generic;
using System.Data;
using DAS.DigitalEngagement.Application.PerformanceImport.Mappers;
using DAS.DigitalEngagement.Models.PerformanceImport;

namespace DAS.DigitalEngagement.EmailIntegration.UnitTests.PerformanceImport.Mappers;

[TestFixture]
public class SendCampaignDataTableMapperTests
{
    [Test]
    public void CreateSends_AddsExpectedColumnsAndValues()
    {
        var table = SendCampaignDataTableMapper.CreateSends([
            new Send
            {
                Id = 101,
                Name = "Welcome",
                SubaccountId = 12,
                CampaignId = 34,
                SendTypeId = 5,
                MessageDesignId = 7,
                SendType = "Email",
                Status = "Sent",
                SubStatus = "Complete",
                SendDate = DateTimeOffset.Parse("2025-10-14T12:24:56.723Z"),
                SendCompletedDate = DateTimeOffset.Parse("2025-10-14T12:30:00Z"),
                IsOutbox = false,
                CampaignType = "Recurrent",
                MessageType = "HTML",
                ContactCount = 250,
                CreatedBy = "Test User",
                CreatedByUserId = 22,
                ConfirmedByUserId = 23,
                IsArchived = false,
                Sequence = 4,
                CreatedDate = DateTimeOffset.Parse("2025-10-14T12:24:56.723Z"),
                FromEmail = "training@service.education.gov.uk",
                FromName = "Training: Apprenticeships",
                ReplyEmail = "reply@example.com",
                SubjectLine = "Hello there"
            }]);

        Assert.That(table.Columns.Contains("ID"), Is.True);
        Assert.That(table.Columns.Contains("Name"), Is.True);
        Assert.That(table.Columns.Contains("SubaccountID"), Is.True);
        Assert.That(table.Columns.Contains("CampaignID"), Is.True);
        Assert.That(table.Columns.Contains("SendTypeID"), Is.True);
        Assert.That(table.Columns.Contains("MessageDesignID"), Is.True);
        Assert.That(table.Columns.Contains("SendDate"), Is.True);
        Assert.That(table.Rows.Count, Is.EqualTo(1));
        Assert.That(table.Rows[0]["ID"], Is.EqualTo(101));
        Assert.That(table.Rows[0]["Name"], Is.EqualTo("Welcome"));
        Assert.That(table.Rows[0]["SubaccountID"], Is.EqualTo(12));
        Assert.That(table.Rows[0]["CampaignID"], Is.EqualTo(34));
        Assert.That(table.Rows[0]["SendTypeID"], Is.EqualTo(5));
        Assert.That(table.Rows[0]["MessageDesignID"], Is.EqualTo(7));
        Assert.That(table.Rows[0]["SendDate"], Is.EqualTo(DateTime.Parse("2025-10-14T12:24:56.723Z").ToUniversalTime()));
        Assert.That(table.Rows[0]["SendCompletedDate"], Is.EqualTo(DateTime.Parse("2025-10-14T12:30:00Z").ToUniversalTime()));
        Assert.That(table.Rows[0]["CreatedDate"], Is.EqualTo(DateTime.Parse("2025-10-14T12:24:56.723Z").ToUniversalTime()));
        Assert.That(table.Rows[0]["FromEmail"], Is.EqualTo("training@service.education.gov.uk"));
        Assert.That(table.Rows[0]["FromName"], Is.EqualTo("Training: Apprenticeships"));
        Assert.That(table.Rows[0]["ReplyEmail"], Is.EqualTo("reply@example.com"));
        Assert.That(table.Rows[0]["SubjectLine"], Is.EqualTo("Hello there"));
    }

    [Test]
    public void CreateCampaigns_AddsExpectedColumnsAndValues()
    {
        var table = SendCampaignDataTableMapper.CreateCampaigns([
            new Campaign
            {
                Id = 4,
                SubaccountId = 3,
                Name = "TE_EM_Leah Test Nudge_TEST",
                CampaignTypeId = 3,
                Type = "Recurrent",
                CreatedBy = "Leah Taylor-Warsop",
                CreatedByUserId = 27,
                CreatedDate = DateTimeOffset.Parse("2025-10-14T12:24:56.723Z"),
                ModifiedBy = "",
                ModifiedByUserId = null,
                ModifiedDate = DateTimeOffset.Parse("2025-10-31T09:56:04.997Z"),
                DeletedDate = DateTimeOffset.Parse("2026-05-28T09:52:22.867Z"),
                ReportDeletedDate = null,
                IsSetup = true,
                Status = "Inactive",
                FirstSendDate = DateTimeOffset.Parse("2025-10-14T12:42:32.623Z"),
                LastSendDate = DateTimeOffset.Parse("2025-11-19T13:58:06.007Z"),
                CronExpression = "0 30 14 ? * 3",
                StartDate = DateTimeOffset.Parse("2025-10-14T12:24:56Z"),
                EndDate = null,
                NextDueDate = DateTimeOffset.Parse("2025-10-21T13:30:00Z"),
                LastRunDate = DateTimeOffset.Parse("2025-10-14T13:30:00Z"),
                StatusChangedBy = "Leah Taylor-Warsop",
                StatusChangedByUserId = 27,
                StatusChangedDate = DateTimeOffset.Parse("2026-05-28T09:52:22.867Z"),
                TotalSent = 1,
                CanBeModified = false,
                CanBeReported = true,
                IsActive = false,
                HasSpecifiedDates = false,
                SendDate = null,
                FromEmail = "training@service.education.gov.uk",
                FromName = "Training: Apprenticeships",
                ReplyEmail = null,
                SubjectLine = "Almost there! Complete your registration"
            }]);

        Assert.That(table.Columns.Contains("ID"), Is.True);
        Assert.That(table.Columns.Contains("SubaccountID"), Is.True);
        Assert.That(table.Columns.Contains("CronExpression"), Is.True);
        Assert.That(table.Columns.Contains("StartDate"), Is.True);
        Assert.That(table.Columns.Contains("EndDate"), Is.True);
        Assert.That(table.Columns.Contains("NextDueDate"), Is.True);
        Assert.That(table.Columns.Contains("LastRunDate"), Is.True);
        Assert.That(table.Columns.Contains("StatusChangedBy"), Is.True);
        Assert.That(table.Columns.Contains("StatusChangedByUserID"), Is.True);
        Assert.That(table.Columns.Contains("StatusChangedDate"), Is.True);
        Assert.That(table.Columns.Contains("IsActive"), Is.True);
        Assert.That(table.Columns.Contains("HasSpecifiedDates"), Is.True);
        Assert.That(table.Rows.Count, Is.EqualTo(1));
        Assert.That(table.Rows[0]["ID"], Is.EqualTo(4));
        Assert.That(table.Rows[0]["SubaccountID"], Is.EqualTo(3));
        Assert.That(table.Rows[0]["Name"], Is.EqualTo("TE_EM_Leah Test Nudge_TEST"));
        Assert.That(table.Rows[0]["CampaignTypeID"], Is.EqualTo(3));
        Assert.That(table.Rows[0]["Type"], Is.EqualTo("Recurrent"));
        Assert.That(table.Rows[0]["Status"], Is.EqualTo("Inactive"));
        Assert.That(table.Rows[0]["CronExpression"], Is.EqualTo("0 30 14 ? * 3"));
        Assert.That(table.Rows[0]["StartDate"], Is.EqualTo(DateTime.Parse("2025-10-14T12:24:56Z").ToUniversalTime()));
        Assert.That(table.Rows[0]["NextDueDate"], Is.EqualTo(DateTime.Parse("2025-10-21T13:30:00Z").ToUniversalTime()));
        Assert.That(table.Rows[0]["LastRunDate"], Is.EqualTo(DateTime.Parse("2025-10-14T13:30:00Z").ToUniversalTime()));
        Assert.That(table.Rows[0]["StatusChangedBy"], Is.EqualTo("Leah Taylor-Warsop"));
        Assert.That(table.Rows[0]["StatusChangedByUserID"], Is.EqualTo(27));
        Assert.That(table.Rows[0]["IsActive"], Is.EqualTo(false));
        Assert.That(table.Rows[0]["HasSpecifiedDates"], Is.EqualTo(false));
        Assert.That(table.Rows[0]["FromEmail"], Is.EqualTo("training@service.education.gov.uk"));
        Assert.That(table.Rows[0]["SubjectLine"], Is.EqualTo("Almost there! Complete your registration"));
    }

    [Test]
    public void CreateImportMetadata_AddsExpectedColumnsAndValues()
    {
        var importStart = DateTimeOffset.Parse("2025-10-14T12:24:56.723Z");
        var table = SendCampaignDataTableMapper.CreateImportMetadata([
            new SendWithCampaign
            {
                Id = 12,
                CampaignId = 34
            }], importStart);

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
}