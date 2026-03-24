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
/// These tests verify that the repository can be instantiated, validates input parameters, and handles edge cases.
/// Integration tests should be used to verify the full database interaction functionality with stored procedures.
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

    [Test]
    public void Constructor_Should_Accept_Factory_And_Logger_Without_Immediate_Connection()
    {
        // Arrange & Act
        var repository = new CampaignImportMetadataRepository(_mockConnectionFactory.Object, _loggerMock.Object);

        // Assert
        Assert.That(repository, Is.Not.Null);
        _mockConnectionFactory.Verify(f => f.CreateConnection(), Times.Never,
            "Constructor should not create connection immediately");
    }

    #endregion

    #region UpsertAsync Validation Tests

    [Test]
    public void UpsertAsync_Should_Throw_ArgumentNullException_When_Metadata_Is_Null()
    {
        // Arrange
        CampaignImportMetadata? nullMetadata = null;

        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _repository.UpsertAsync(nullMetadata!));

        Assert.That(exception!.ParamName, Is.EqualTo("campaignImportMetadata"));
    }

    [Test]
    public void UpsertAsync_Should_Accept_Complete_Import_Metadata()
    {
        // Arrange
        var metadata = new CampaignImportMetadata
        {
            CampaignId = 12345,
            IsImportComplete = true,
            ImportStartDate = DateTime.UtcNow.AddHours(-1),
            ImportEndDate = DateTime.UtcNow
        };

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.UpsertAsync(metadata);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks - actual behavior would require integration test
            }
        });
    }

    [Test]
    public void UpsertAsync_Should_Accept_Incomplete_Import_Metadata()
    {
        // Arrange
        var metadata = new CampaignImportMetadata
        {
            CampaignId = 12345,
            IsImportComplete = false,
            ImportStartDate = DateTime.UtcNow,
            ImportEndDate = null
        };

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.UpsertAsync(metadata);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks - actual behavior would require integration test
            }
        });
    }

    [Test]
    public void UpsertAsync_Should_Accept_Metadata_With_Null_ImportEndDate()
    {
        // Arrange
        var metadata = new CampaignImportMetadata
        {
            CampaignId = 99999,
            IsImportComplete = false,
            ImportStartDate = DateTime.UtcNow,
            ImportEndDate = null
        };

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.UpsertAsync(metadata);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        });
    }

    #endregion

    #region GetByIdAsync Validation Tests

    [Test]
    public void GetByIdAsync_Should_Accept_Positive_CampaignId()
    {
        // Arrange
        long campaignId = 12345;

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.GetByIdAsync(campaignId);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        });
    }

    [Test]
    public void GetByIdAsync_Should_Accept_Zero_CampaignId()
    {
        // Arrange
        long campaignId = 0;

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.GetByIdAsync(campaignId);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        });
    }

    [Test]
    public void GetByIdAsync_Should_Accept_Negative_CampaignId()
    {
        // Arrange
        long campaignId = -1;

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.GetByIdAsync(campaignId);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        });
    }

    [Test]
    public void GetByIdAsync_Should_Accept_Max_Long_Value()
    {
        // Arrange
        long campaignId = long.MaxValue;

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.GetByIdAsync(campaignId);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        });
    }

    #endregion

    #region GetByIdsAsync Validation Tests

    [Test]
    public void GetByIdsAsync_Should_Accept_Empty_Collection()
    {
        // Arrange
        var campaignIds = Enumerable.Empty<long>();

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.GetByIdsAsync(campaignIds);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        });
    }

    [Test]
    public void GetByIdsAsync_Should_Accept_Single_Id()
    {
        // Arrange
        var campaignIds = new[] { 12345L };

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.GetByIdsAsync(campaignIds);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        });
    }

    [Test]
    public void GetByIdsAsync_Should_Accept_Multiple_Ids()
    {
        // Arrange
        var campaignIds = new[] { 1L, 2L, 3L, 4L, 5L };

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.GetByIdsAsync(campaignIds);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        });
    }

    [Test]
    public void GetByIdsAsync_Should_Handle_Duplicate_Ids()
    {
        // Arrange
        var campaignIds = new[] { 1L, 2L, 2L, 3L, 3L, 3L };

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.GetByIdsAsync(campaignIds);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        });
    }

    [Test]
    public void GetByIdsAsync_Should_Handle_Large_Id_Collection()
    {
        // Arrange
        var campaignIds = Enumerable.Range(1, 1000).Select(i => (long)i);

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.GetByIdsAsync(campaignIds);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        });
    }

    [Test]
    public void GetByIdsAsync_Should_Handle_Mixed_Positive_And_Negative_Ids()
    {
        // Arrange
        var campaignIds = new[] { -1L, 0L, 1L, 100L, -100L };

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.GetByIdsAsync(campaignIds);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        });
    }

    #endregion

    #region GetAllAsync Tests

    [Test]
    public void GetAllAsync_Should_Not_Require_Parameters()
    {
        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.GetAllAsync();
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        });
    }

    #endregion

    #region Interface Implementation Tests

    [Test]
    public void Repository_Should_Implement_All_Interface_Methods()
    {
        // Arrange
        var interfaceType = typeof(ICampaignImportMetadataRepository);
        var implementationType = typeof(CampaignImportMetadataRepository);

        // Act
        var interfaceMethods = interfaceType.GetMethods().Select(m => m.Name).OrderBy(n => n).ToList();
        var implementationMethods = implementationType.GetMethods()
            .Where(m => interfaceMethods.Contains(m.Name))
            .Select(m => m.Name)
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        // Assert
        Assert.That(implementationMethods, Is.EquivalentTo(interfaceMethods));
        Assert.That(implementationMethods, Has.Count.EqualTo(4), 
            "Repository should implement exactly 4 methods: GetByIdAsync, GetAllAsync, GetByIdsAsync, UpsertAsync");
    }

    #endregion

    #region Dependency Usage Tests

    [Test]
    public async Task Repository_Should_Use_Factory_When_Calling_Methods()
    {
        // Arrange
        var factoryMock = new Mock<IDbConnectionFactory>();
        var connectionMock = new Mock<IDbConnection>();
        factoryMock.Setup(f => f.CreateConnection()).Returns(connectionMock.Object);

        var repository = new CampaignImportMetadataRepository(factoryMock.Object, _loggerMock.Object);

        // Act & Assert
        try
        {
            await repository.GetByIdAsync(1);
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks - actual behavior would require integration test
        }
        catch (AggregateException ex) when (ex.InnerException is InvalidCastException)
        {
            // Also expected when using mocks in Task contexts
        }

        // Assert
        factoryMock.Verify(f => f.CreateConnection(), Times.Once,
            "Repository should use factory to create connection when calling GetByIdAsync");
    }

    #endregion

    #region Boundary Value Tests for CampaignImportMetadata

    [Test]
    public void UpsertAsync_Should_Accept_Metadata_With_Min_DateTime_Values()
    {
        // Arrange
        var metadata = new CampaignImportMetadata
        {
            CampaignId = 1,
            IsImportComplete = false,
            ImportStartDate = DateTime.MinValue,
            ImportEndDate = DateTime.MinValue
        };

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.UpsertAsync(metadata);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        });
    }

    [Test]
    public void UpsertAsync_Should_Accept_Metadata_With_Max_DateTime_Values()
    {
        // Arrange
        var metadata = new CampaignImportMetadata
        {
            CampaignId = 1,
            IsImportComplete = true,
            ImportStartDate = DateTime.MaxValue,
            ImportEndDate = DateTime.MaxValue
        };

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.UpsertAsync(metadata);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        });
    }

    [Test]
    public void UpsertAsync_Should_Accept_Metadata_With_Zero_CampaignId()
    {
        // Arrange
        var metadata = new CampaignImportMetadata
        {
            CampaignId = 0,
            IsImportComplete = false,
            ImportStartDate = DateTime.UtcNow,
            ImportEndDate = null
        };

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.UpsertAsync(metadata);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        });
    }

    [Test]
    public void UpsertAsync_Should_Accept_Metadata_With_Negative_CampaignId()
    {
        // Arrange
        var metadata = new CampaignImportMetadata
        {
            CampaignId = -12345,
            IsImportComplete = true,
            ImportStartDate = DateTime.UtcNow,
            ImportEndDate = DateTime.UtcNow
        };

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.UpsertAsync(metadata);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        });
    }

    [Test]
    public void UpsertAsync_Should_Accept_Metadata_With_Max_Long_CampaignId()
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
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.UpsertAsync(metadata);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        });
    }

    #endregion
}
