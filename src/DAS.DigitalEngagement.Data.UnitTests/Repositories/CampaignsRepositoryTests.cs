using DAS.DigitalEngagement.CampaignInterest.Data.Helpers;
using DAS.DigitalEngagement.CampaignInterest.Data.Models;
using DAS.DigitalEngagement.CampaignInterest.Data.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using System.Data;

namespace DAS.DigitalEngagement.CampaignInterest.Data.UnitTests.Repositories;

/// <summary>
/// Unit tests for CampaignsRepository.
/// Note: Repository methods cannot be fully tested with mocks because Dapper extensions require a real SqlConnection.
/// These tests verify that the repository can be instantiated and validates input parameters.
/// Integration tests should be used to verify the full database interaction functionality.
/// </summary>
[TestFixture]
public class CampaignsRepositoryTests
{
    private CampaignsRepository _repository = null!;
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

        _repository = new CampaignsRepository(_mockConnectionFactory.Object, _mockLogger.Object);
    }

    #region Constructor Tests

    [Test]
    public void Constructor_Should_Create_Instance_With_Valid_Dependencies()
    {
        // Arrange & Act
        var repository = new CampaignsRepository(_mockConnectionFactory.Object, _mockLogger.Object);

        // Assert
        Assert.That(repository, Is.Not.Null);
        Assert.That(repository, Is.InstanceOf<ICampaignsRepository>());
    }

    #endregion

    #region UpsertAsync Validation Tests

    [Test]
    public void UpsertAsync_Should_Throw_ArgumentNullException_When_Campaign_Is_Null()
    {
        // Arrange
        Campaigns? campaign = null;

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _repository.UpsertAsync(campaign!));
    }

    #endregion
}
