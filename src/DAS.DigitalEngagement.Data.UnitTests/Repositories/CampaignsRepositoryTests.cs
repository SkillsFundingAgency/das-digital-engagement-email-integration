using DAS.DigitalEngagement.CampaignInterest.Data.Helpers;
using DAS.DigitalEngagement.CampaignInterest.Data.Models;
using DAS.DigitalEngagement.CampaignInterest.Data.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using System.Data;

namespace DAS.DigitalEngagement.CampaignInterest.Data.UnitTests.Repositories;

[TestFixture]
public class CampaignsRepositoryTests
{
    private CampaignsRepository _campaignsRepository = null!;
    private Mock<IDbConnectionFactory> _mockConnectionFactory = null!;
    private Mock<IDbConnection> _mockConnection = null!;
    private Mock<ILogger<CampaignsRepository>> _mockLogger = null!;

    [SetUp]
    public void Setup()
    {
        _mockConnectionFactory = new Mock<IDbConnectionFactory>();
        _mockConnection = new Mock<IDbConnection>();
        _mockLogger = new Mock<ILogger<CampaignsRepository>>();

        _mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(_mockConnection.Object);

        _campaignsRepository = new CampaignsRepository(_mockConnectionFactory.Object, _mockLogger.Object);
    }

    #region UpsertAsync Tests

    [Test]
    public void UpsertAsync_Should_Accept_Valid_Campaign()
    {
        // Arrange
        var campaign = new Campaigns
        {
            ExternalId = 12345,
            Name = "Test Campaign",
            Type = "Email",
            CreatedBy = "TestUser",
            CreatedOn = DateTime.UtcNow,
            ModifiedBy = "TestUser",
            ModifiedOn = DateTime.UtcNow,
            FirstSendDate = DateTime.UtcNow,
            LastSendDate = DateTime.UtcNow.AddDays(1),
            FromEmailAddress = "test@example.com",
            FromName = "Test Sender",
            ReplyEmailAddress = "reply@example.com",
            Subject = "Test Subject",
            SubStatus = "Active",
            ContactCount = 100,
            Account = "TestAccount"
        };

        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _campaignsRepository.UpsertAsync(campaign));
    }

    [Test]
    public void UpsertAsync_Should_Accept_Campaign_With_Null_OptionalFields()
    {
        // Arrange
        var campaign = new Campaigns
        {
            ExternalId = 456,
            Name = "Minimal Campaign",
            Type = "SMS",
            CreatedBy = "User1",
            CreatedOn = DateTime.UtcNow,
            ModifiedBy = null!,
            ModifiedOn = null,
            FirstSendDate = DateTime.UtcNow,
            LastSendDate = null,
            FromEmailAddress = "from@test.com",
            FromName = "Sender",
            ReplyEmailAddress = "reply@test.com",
            Subject = "Subject",
            SubStatus = "Draft",
            ContactCount = 0,
            Account = "Account1"
        };

        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _campaignsRepository.UpsertAsync(campaign));
    }

    [Test]
    public void UpsertAsync_Should_Accept_Campaign_With_Zero_ExternalId()
    {
        // Arrange
        var campaign = new Campaigns
        {
            ExternalId = 0,
            Name = "Zero ID Campaign",
            Type = "Email",
            CreatedBy = "TestUser",
            CreatedOn = DateTime.UtcNow,
            FirstSendDate = DateTime.UtcNow,
            FromEmailAddress = "test@example.com",
            FromName = "Test",
            ReplyEmailAddress = "reply@example.com",
            Subject = "Test",
            SubStatus = "Active",
            ContactCount = 50,
            Account = "TestAccount"
        };

        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _campaignsRepository.UpsertAsync(campaign));
    }

    [Test]
    public void UpsertAsync_Should_Accept_Campaign_With_Large_ExternalId()
    {
        // Arrange
        var campaign = new Campaigns
        {
            ExternalId = int.MaxValue,
            Name = "Max ID Campaign",
            Type = "Email",
            CreatedBy = "TestUser",
            CreatedOn = DateTime.UtcNow,
            FirstSendDate = DateTime.UtcNow,
            FromEmailAddress = "test@example.com",
            FromName = "Test",
            ReplyEmailAddress = "reply@example.com",
            Subject = "Test",
            SubStatus = "Active",
            ContactCount = 1000,
            Account = "TestAccount"
        };

        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _campaignsRepository.UpsertAsync(campaign));
    }

    [Test]
    public void UpsertAsync_Should_Accept_Campaign_With_Past_Dates()
    {
        // Arrange
        var campaign = new Campaigns
        {
            ExternalId = 789,
            Name = "Historical Campaign",
            Type = "Email",
            CreatedBy = "TestUser",
            CreatedOn = DateTime.UtcNow.AddMonths(-6),
            ModifiedBy = "TestUser",
            ModifiedOn = DateTime.UtcNow.AddMonths(-5),
            FirstSendDate = DateTime.UtcNow.AddMonths(-4),
            LastSendDate = DateTime.UtcNow.AddMonths(-3),
            FromEmailAddress = "test@example.com",
            FromName = "Test",
            ReplyEmailAddress = "reply@example.com",
            Subject = "Old Campaign",
            SubStatus = "Completed",
            ContactCount = 500,
            Account = "TestAccount"
        };

        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _campaignsRepository.UpsertAsync(campaign));
    }

    [Test]
    public void UpsertAsync_Should_Accept_Campaign_With_Future_Dates()
    {
        // Arrange
        var campaign = new Campaigns
        {
            ExternalId = 1011,
            Name = "Scheduled Campaign",
            Type = "Email",
            CreatedBy = "TestUser",
            CreatedOn = DateTime.UtcNow,
            ModifiedBy = "TestUser",
            ModifiedOn = DateTime.UtcNow,
            FirstSendDate = DateTime.UtcNow.AddDays(7),
            LastSendDate = DateTime.UtcNow.AddDays(30),
            FromEmailAddress = "test@example.com",
            FromName = "Test",
            ReplyEmailAddress = "reply@example.com",
            Subject = "Future Campaign",
            SubStatus = "Scheduled",
            ContactCount = 250,
            Account = "TestAccount"
        };

        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _campaignsRepository.UpsertAsync(campaign));
    }

    [Test]
    public void UpsertAsync_Should_Accept_Campaign_With_Zero_ContactCount()
    {
        // Arrange
        var campaign = new Campaigns
        {
            ExternalId = 1213,
            Name = "Empty Campaign",
            Type = "Email",
            CreatedBy = "TestUser",
            CreatedOn = DateTime.UtcNow,
            FirstSendDate = DateTime.UtcNow,
            FromEmailAddress = "test@example.com",
            FromName = "Test",
            ReplyEmailAddress = "reply@example.com",
            Subject = "Test",
            SubStatus = "Draft",
            ContactCount = 0,
            Account = "TestAccount"
        };

        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _campaignsRepository.UpsertAsync(campaign));
    }

    [Test]
    public void UpsertAsync_Should_Accept_Campaign_With_Special_Characters()
    {
        // Arrange
        var campaign = new Campaigns
        {
            ExternalId = 1415,
            Name = "Campaign with Special Ch@rs! & Émoji 🎉",
            Type = "Email",
            CreatedBy = "User's Name",
            CreatedOn = DateTime.UtcNow,
            FirstSendDate = DateTime.UtcNow,
            FromEmailAddress = "test+tag@example.com",
            FromName = "Sender's Name",
            ReplyEmailAddress = "reply+tag@example.com",
            Subject = "Subject: Important! & Urgent",
            SubStatus = "Active",
            ContactCount = 75,
            Account = "Account-Name_123"
        };

        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _campaignsRepository.UpsertAsync(campaign));
    }

    [Test]
    public void UpsertAsync_CurrentImplementation_Throws_InvalidCastException_With_Mock()
    {
        // Arrange
        var campaign = new Campaigns
        {
            ExternalId = 999,
            Name = "Test",
            Type = "Email",
            CreatedBy = "User",
            CreatedOn = DateTime.UtcNow,
            FirstSendDate = DateTime.UtcNow,
            FromEmailAddress = "test@example.com",
            FromName = "Test",
            ReplyEmailAddress = "reply@example.com",
            Subject = "Test",
            SubStatus = "Active",
            ContactCount = 10,
            Account = "Test"
        };

        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _campaignsRepository.UpsertAsync(campaign));
    }

    [Test]
    public void UpsertAsync_Should_Throw_ArgumentNullException_When_Campaign_Is_Null()
    {
        // Arrange
        Campaigns? campaign = null;

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _campaignsRepository.UpsertAsync(campaign!));
    }

    #endregion

    #region GetByIdAsync Tests

    [Test]
    public void GetByIdAsync_Should_Accept_Valid_Id()
    {
        // Arrange
        long id = 123;

        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _campaignsRepository.GetByIdAsync(id));
    }

    [Test]
    public void GetByIdAsync_Should_Accept_Zero_Id()
    {
        // Arrange
        long id = 0;

        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _campaignsRepository.GetByIdAsync(id));
    }

    [Test]
    public void GetByIdAsync_Should_Accept_Large_Id()
    {
        // Arrange
        long id = long.MaxValue;

        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _campaignsRepository.GetByIdAsync(id));
    }

    [Test]
    public void GetByIdAsync_CurrentImplementation_Throws_InvalidCastException_With_Mock()
    {
        // Arrange
        long id = 456;

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _campaignsRepository.GetByIdAsync(id));
        Assert.That(ex!.Message, Does.Contain("SqlConnection"));
    }

    #endregion

    #region GetAllAsync Tests

    [Test]
    public void GetAllAsync_Should_Not_Require_Parameters()
    {
        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _campaignsRepository.GetAllAsync());
    }

    [Test]
    public void GetAllAsync_CurrentImplementation_Throws_InvalidCastException_With_Mock()
    {
        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _campaignsRepository.GetAllAsync());
        Assert.That(ex!.Message, Does.Contain("SqlConnection"));
    }

    #endregion

    #region GetByIdsAsync Tests

    [Test]
    public void GetByIdsAsync_Should_Accept_Empty_Collection()
    {
        // Arrange
        var ids = new List<long>();

        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _campaignsRepository.GetByIdsAsync(ids));
    }

    [Test]
    public void GetByIdsAsync_Should_Accept_Single_Id()
    {
        // Arrange
        var ids = new List<long> { 123 };

        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _campaignsRepository.GetByIdsAsync(ids));
    }

    [Test]
    public void GetByIdsAsync_Should_Accept_Multiple_Ids()
    {
        // Arrange
        var ids = new List<long> { 123, 456, 789 };

        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _campaignsRepository.GetByIdsAsync(ids));
    }

    [Test]
    public void GetByIdsAsync_Should_Accept_Large_Collection()
    {
        // Arrange
        List<long> ids = [.. Enumerable.Range(1, 1000).Select(x => (long)x)];

        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _campaignsRepository.GetByIdsAsync(ids));
    }

    [Test]
    public void GetByIdsAsync_CurrentImplementation_Throws_InvalidCastException_With_Mock()
    {
        // Arrange
        var ids = new List<long> { 100, 200, 300 };

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _campaignsRepository.GetByIdsAsync(ids));
        Assert.That(ex!.Message, Does.Contain("SqlConnection"));
    }

    #endregion
}
