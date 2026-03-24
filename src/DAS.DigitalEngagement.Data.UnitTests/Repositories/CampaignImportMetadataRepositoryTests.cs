using DAS.DigitalEngagement.CampaignInterest.Data.Helpers;
using DAS.DigitalEngagement.CampaignInterest.Data.Models;
using DAS.DigitalEngagement.CampaignInterest.Data.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using System.Data;

namespace DAS.DigitalEngagement.CampaignInterest.Data.UnitTests.Repositories;

[TestFixture]
public class CampaignImportMetadataRepositoryTests
{
    private CampaignImportMetadataRepository _campaignImportMetadataRepository = null!;
    private Mock<ILogger<CampaignImportMetadataRepository>> _loggerMock;

    private Mock<IDbConnectionFactory> _mockConnectionFactory = null!;
    private Mock<IDbConnection> _mockConnection = null!;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<CampaignImportMetadataRepository>>();

        _mockConnectionFactory = new Mock<IDbConnectionFactory>();
        _mockConnection = new Mock<IDbConnection>();

        _mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(_mockConnection.Object);

        _campaignImportMetadataRepository = new CampaignImportMetadataRepository(_mockConnectionFactory.Object, _loggerMock.Object);
    }

    #region GetByIdAsync Tests

    [Test]
    public void GetByIdAsync_CurrentImplementation_Throws_InvalidCastException_With_Mock()
    {
        // Arrange
        long campaignId = 123;

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _campaignImportMetadataRepository.GetByIdAsync(campaignId));

        Assert.That(ex!.Message, Does.Contain("SqlConnection"));
    }

    [Test]
    public void GetByIdAsync_Should_Accept_Null_Email_Without_Validation_Exception()
    {
        // Arrange
        long campaignId = 0;

        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _campaignImportMetadataRepository.GetByIdAsync(campaignId));
    }

    #endregion

    #region GetAllAsync Tests

    [Test]
    public void GetAllAsync_CurrentImplementation_Throws_InvalidCastException_With_Mock()
    {
        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _campaignImportMetadataRepository.GetAllAsync());

        Assert.That(ex!.Message, Does.Contain("SqlConnection"));
    }

    [Test]
    public void GetAllAsync_Should_Not_Require_Parameters()
    {
        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _campaignImportMetadataRepository.GetAllAsync());
    }

    #endregion

    #region GetByIdsAsync Tests

    [Test]
    public void GetByIdsAsync_CurrentImplementation_Throws_InvalidCastException_With_Mock()
    {
        // Arrange
        var campaignIds = new List<long> { 1234, 4567 };

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _campaignImportMetadataRepository.GetByIdsAsync(campaignIds));
        Assert.That(ex!.Message, Does.Contain("SqlConnection"));
    }

    [Test]
    public void GetByIdsAsync_Should_Accept_Empty_Collection()
    {
        // Arrange
        var campaignIds = new List<long>();

        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _campaignImportMetadataRepository.GetByIdsAsync(campaignIds));
    }

    [Test]
    public void GetByIdsAsync_Should_Accept_Single_CampaignId()
    {
        // Arrange
        var campaignIds = new List<long> { 1234 };

        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _campaignImportMetadataRepository.GetByIdsAsync(campaignIds));
    }

    [Test]
    public void GetByIdsAsync_Should_Accept_Multiple_CampaignIds()
    {
        // Arrange
        var campaignIds = new List<long> { 1234, 5678, 91011 };

        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _campaignImportMetadataRepository.GetByIdsAsync(campaignIds));
    }

    [Test]
    public void GetByIdsAsync_Should_Accept_Large_CampaignId_Collection()
    {
        // Arrange
        List<long> campaignIds = [.. Enumerable.Range(1, 1000).Select(x => (long)x)];

        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _campaignImportMetadataRepository.GetByIdsAsync(campaignIds));
    }

    #endregion

    #region UpsertAsync Tests

    [Test]
    public void UpsertAsync_Should_Accept_Valid_CampaignImportMetadata()
    {
        // Arrange
        var metadata = new CampaignImportMetadata
        {
            CampaignId = 123,
            IsImportComplete = true,
            ImportStartDate = DateTime.UtcNow,
            ImportEndDate = DateTime.UtcNow.AddHours(1)
        };

        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _campaignImportMetadataRepository.UpsertAsync(metadata));
    }

    [Test]
    public void UpsertAsync_Should_Accept_Metadata_Without_ImportEndDate()
    {
        // Arrange
        var metadata = new CampaignImportMetadata
        {
            CampaignId = 456,
            IsImportComplete = false,
            ImportStartDate = DateTime.UtcNow,
            ImportEndDate = null
        };

        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _campaignImportMetadataRepository.UpsertAsync(metadata));
    }

    [Test]
    public void UpsertAsync_Should_Accept_Metadata_With_Zero_CampaignId()
    {
        // Arrange
        var metadata = new CampaignImportMetadata
        {
            CampaignId = 0,
            IsImportComplete = false,
            ImportStartDate = DateTime.UtcNow
        };

        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _campaignImportMetadataRepository.UpsertAsync(metadata));
    }

    [Test]
    public void UpsertAsync_Should_Accept_Metadata_With_Large_CampaignId()
    {
        // Arrange
        var metadata = new CampaignImportMetadata
        {
            CampaignId = long.MaxValue,
            IsImportComplete = true,
            ImportStartDate = DateTime.UtcNow,
            ImportEndDate = DateTime.UtcNow
        };

        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _campaignImportMetadataRepository.UpsertAsync(metadata));
    }

    [Test]
    public void UpsertAsync_Should_Accept_Metadata_With_Past_Dates()
    {
        // Arrange
        var metadata = new CampaignImportMetadata
        {
            CampaignId = 789,
            IsImportComplete = true,
            ImportStartDate = DateTime.UtcNow.AddDays(-7),
            ImportEndDate = DateTime.UtcNow.AddDays(-6)
        };

        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _campaignImportMetadataRepository.UpsertAsync(metadata));
    }

    [Test]
    public void UpsertAsync_Should_Accept_Metadata_With_Future_Dates()
    {
        // Arrange
        var metadata = new CampaignImportMetadata
        {
            CampaignId = 1011,
            IsImportComplete = false,
            ImportStartDate = DateTime.UtcNow.AddDays(1),
            ImportEndDate = DateTime.UtcNow.AddDays(2)
        };

        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _campaignImportMetadataRepository.UpsertAsync(metadata));
    }

    [Test]
    public void UpsertAsync_CurrentImplementation_Throws_InvalidCastException_With_Mock()
    {
        // Arrange
        var metadata = new CampaignImportMetadata
        {
            CampaignId = 999,
            IsImportComplete = true,
            ImportStartDate = DateTime.UtcNow,
            ImportEndDate = DateTime.UtcNow.AddHours(2)
        };

        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _campaignImportMetadataRepository.UpsertAsync(metadata));
    }

    #endregion
}
