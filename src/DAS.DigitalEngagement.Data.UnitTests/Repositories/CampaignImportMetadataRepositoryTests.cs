using DAS.DigitalEngagement.CampaignInterest.Data.Helpers;
using DAS.DigitalEngagement.CampaignInterest.Data.Models;
using DAS.DigitalEngagement.CampaignInterest.Data.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using System.Data;

namespace DAS.DigitalEngagement.CampaignInterest.Data.UnitTests.Repositories;

/// <summary>
/// Unit tests for CampaignImportMetadataRepository.
/// Note: Repository methods cannot be fully tested with mocks because Dapper extensions require a real SqlConnection.
/// These tests verify that the repository can be instantiated and accepts various input parameters.
/// Integration tests should be used to verify the full database interaction functionality.
/// </summary>
[TestFixture]
public class CampaignImportMetadataRepositoryTests
{
    private CampaignImportMetadataRepository _repository = null!;
    private Mock<ILogger<CampaignImportMetadataRepository>> _loggerMock = null!;
    private Mock<IDbConnectionFactory> _mockConnectionFactory = null!;
    private Mock<IDbConnection> _mockConnection = null!;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<CampaignImportMetadataRepository>>();
        _mockConnectionFactory = new Mock<IDbConnectionFactory>();
        _mockConnection = new Mock<IDbConnection>();

        _mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(_mockConnection.Object);

        _repository = new CampaignImportMetadataRepository(_mockConnectionFactory.Object, _loggerMock.Object);
    }

    #region Constructor Tests

    [Test]
    public void Constructor_Should_Create_Instance_With_Valid_Dependencies()
    {
        // Arrange & Act
        var repository = new CampaignImportMetadataRepository(_mockConnectionFactory.Object, _loggerMock.Object);

        // Assert
        Assert.That(repository, Is.Not.Null);
        Assert.That(repository, Is.InstanceOf<ICampaignImportMetadataRepository>());
    }

    #endregion

    #region UpsertAsync Validation Tests

    [Test]
    public void UpsertAsync_Should_Throw_ArgumentNullException_When_Metadata_Is_Null()
    {
        // Arrange
        CampaignImportMetadata? nullMetadata = null;

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _repository.UpsertAsync(nullMetadata!));
    }

    #endregion
}
