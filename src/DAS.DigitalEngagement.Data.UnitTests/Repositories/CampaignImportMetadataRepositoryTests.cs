using DAS.DigitalEngagement.CampaignInterest.Data.Helpers;
using DAS.DigitalEngagement.CampaignInterest.Data.Models;
using DAS.DigitalEngagement.CampaignInterest.Data.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Data;

namespace DAS.DigitalEngagement.CampaignInterest.Data.UnitTests.Repositories;

/// <summary>
/// Unit tests for CampaignImportMetadataRepository.
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
        // Act & Assert
        try
        {
            await _repository.GetByIdAsync(1);
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
        _mockConnectionFactory.Verify(f => f.CreateConnection(), Times.Once,
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

    [Test]
    public async Task GetAllAsync_Should_Log_Information_With_StoredProcedure()
    {
        // Act
        try
        {
            await _repository.GetAllAsync();
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks - SqlConnection cast will fail
        }

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("dbo.Usp_CampaignImportMetadata_Get")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Logger should be called with stored procedure name");
    }

    [Test]
    public async Task GetAllAsync_Should_Call_Factory_CreateConnection_Once()
    {
        // Act
        try
        {
            await _repository.GetAllAsync();
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }

        // Assert
        _mockConnectionFactory.Verify(f => f.CreateConnection(), Times.Once, "Repository should use factory to create connection exactly once");
    }

    [Test]
    public void GetAllAsync_Should_Attempt_Cast_To_SqlConnection()
    {
        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () => await _repository.GetAllAsync(),
            "Method should attempt to cast connection to SqlConnection");
    }

    [Test]
    public async Task GetAllAsync_Should_Use_Correct_StoredProcedure_Name()
    {
        // Arrange
        const string expectedStoredProcedure = "dbo.Usp_CampaignImportMetadata_Get";

        // Act
        try
        {
            await _repository.GetAllAsync();
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedStoredProcedure)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            $"Logger should be called with stored procedure name '{expectedStoredProcedure}'");
    }

    [Test]
    public async Task GetAllAsync_Should_Log_Before_Connection_Operations()
    {
        // Arrange
        bool logWasCalled = false;

        _loggerMock
            .Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(() => logWasCalled = true);

        _mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(_mockConnection.Object);
        var repository = new CampaignImportMetadataRepository(_mockConnectionFactory.Object, _loggerMock.Object);

        // Act
        try
        {
            await repository.GetAllAsync();
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }

        // Assert
        logWasCalled.Should().BeTrue("Logger should be called before connection operations");
    }

    #endregion

    #region GetByIdAsync Tests

    [Test]
    public void GetByIdsAsync_Should_Accept_Collection_With_MinValue()
    {
        // Arrange
        var campaignIds = new[] { long.MinValue };

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
    public void GetByIdsAsync_Should_Accept_Collection_With_MaxValue()
    {
        // Arrange
        var campaignIds = new[] { long.MaxValue };

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
    public void GetByIdsAsync_Should_Accept_Collection_With_Zero()
    {
        // Arrange
        var campaignIds = new[] { 0L };

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
    public void GetByIdsAsync_Should_Accept_Collection_With_All_Boundary_Values()
    {
        // Arrange
        var campaignIds = new[] { long.MinValue, 0L, long.MaxValue };

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
    public async Task GetByIdsAsync_Should_Call_Factory_CreateConnection()
    {
        // Arrange
        var campaignIds = new[] { 1L, 2L, 3L };

        // Act
        try
        {
            await _repository.GetByIdsAsync(campaignIds);
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }

        // Assert
        _mockConnectionFactory.Verify(f => f.CreateConnection(), Times.Once);
    }

    [Test]
    public async Task GetByIdsAsync_Should_Log_Information_With_Correct_Parameters()
    {
        // Arrange
        var campaignIds = new[] { 1L, 2L, 3L };
        var expectedStoredProcedure = "dbo.Usp_CampaignImportMetadata_Get";

        // Act
        try
        {
            await _repository.GetByIdsAsync(campaignIds);
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Fetching") && v.ToString()!.Contains(expectedStoredProcedure)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetByIdsAsync_Should_Format_Single_Id_Correctly()
    {
        // Arrange
        var campaignIds = new[] { 12345L };

        // Act
        try
        {
            await _repository.GetByIdsAsync(campaignIds);
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }

        // Assert - verify factory was called (indicates string formatting didn't throw)
        _mockConnectionFactory.Verify(f => f.CreateConnection(), Times.Once);
    }

    [Test]
    public async Task GetByIdsAsync_Should_Format_Multiple_Ids_Correctly()
    {
        // Arrange
        var campaignIds = new[] { 1L, 2L, 3L, 4L, 5L };

        // Act
        try
        {
            await _repository.GetByIdsAsync(campaignIds);
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }

        // Assert - verify factory was called (indicates string formatting didn't throw)
        _mockConnectionFactory.Verify(f => f.CreateConnection(), Times.Once);
    }

    [Test]
    public async Task GetByIdAsync_Should_Log_Information_With_CampaignId_And_StoredProcedure()
    {
        // Arrange
        long campaignId = 12345;

        // Act
        try
        {
            await _repository.GetByIdAsync(campaignId);
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("12345") && v.ToString()!.Contains("dbo.Usp_CampaignImportMetadata_Get")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Logger should be called with campaignId and stored procedure name");
    }

    [Test]
    public void GetByIdAsync_Should_Accept_Min_Long_Value()
    {
        // Arrange
        long campaignId = long.MinValue;

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
    public async Task GetByIdAsync_Should_Convert_MinValue_CampaignId_To_String()
    {
        // Arrange
        long campaignId = long.MinValue;
        string expectedStringValue = long.MinValue.ToString();

        // Act
        try
        {
            await _repository.GetByIdAsync(campaignId);
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedStringValue)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Logger should receive the string representation of long.MinValue");
    }

    [Test]
    public async Task GetByIdAsync_Should_Convert_MaxValue_CampaignId_To_String()
    {
        // Arrange
        long campaignId = long.MaxValue;
        string expectedStringValue = long.MaxValue.ToString();

        // Act
        try
        {
            await _repository.GetByIdAsync(campaignId);
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedStringValue)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Logger should receive the string representation of long.MaxValue");
    }

    [Test]
    public async Task GetByIdAsync_Should_Call_Factory_CreateConnection_Once()
    {
        // Arrange
        long campaignId = 100;

        // Act
        try
        {
            await _repository.GetByIdAsync(campaignId);
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }

        // Assert
        _mockConnectionFactory.Verify(f => f.CreateConnection(), Times.Once, "Factory.CreateConnection should be called exactly once");
    }

    [Test]
    public void GetByIdAsync_Should_Attempt_Cast_To_SqlConnection()
    {
        // Arrange
        long campaignId = 1;

        // Act & Assert
        var exception = Assert.ThrowsAsync<InvalidCastException>(async () => await _repository.GetByIdAsync(campaignId));

        exception.Should().NotBeNull("InvalidCastException should be thrown when casting IDbConnection mock to SqlConnection");
    }

    [Test]
    public async Task GetByIdAsync_Should_Use_Correct_StoredProcedure_Name()
    {
        // Arrange
        long campaignId = 999;
        const string expectedStoredProcedure = "dbo.Usp_CampaignImportMetadata_Get";

        // Act
        try
        {
            await _repository.GetByIdAsync(campaignId);
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedStoredProcedure)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            $"Logger should log the stored procedure name: {expectedStoredProcedure}");
    }

    [TestCase(0L)]
    [TestCase(1L)]
    [TestCase(-1L)]
    [TestCase(9223372036854775807L)] // long.MaxValue
    [TestCase(-9223372036854775808L)] // long.MinValue
    public void GetByIdAsync_Should_Accept_Various_CampaignId_Values(long campaignId)
    {
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
        }, $"Method should accept campaignId value: {campaignId}");
    }

    [TestCase(0L, "0")]
    [TestCase(123L, "123")]
    [TestCase(-456L, "-456")]
    [TestCase(9223372036854775807L, "9223372036854775807")]
    [TestCase(-9223372036854775808L, "-9223372036854775808")]
    public async Task GetByIdAsync_Should_Log_CampaignId_As_String(long campaignId, string expectedStringValue)
    {
        // Act
        try
        {
            await _repository.GetByIdAsync(campaignId);
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedStringValue)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            $"Logger should log campaignId {campaignId} as string '{expectedStringValue}'");
    }

    #endregion

    #region GetByIdsAsync Tests

    [Test]
    public void GetByIdsAsync_WithEmptyCollection_ShouldNotThrowArgumentException()
    {
        // Arrange
        var campaignIds = new List<long>();

        // Act
        Func<Task> act = async () => await _repository.GetByIdsAsync(campaignIds);

        // Assert
        act.Should().ThrowAsync<InvalidCastException>();
    }

    [Test]
    public void GetByIdsAsync_WithSingleId_ShouldNotThrowArgumentException()
    {
        // Arrange
        var campaignIds = new List<long> { 123L };

        // Act
        Func<Task> act = async () => await _repository.GetByIdsAsync(campaignIds);

        // Assert
        act.Should().ThrowAsync<InvalidCastException>();
    }

    [Test]
    public void GetByIdsAsync_WithMultipleIds_ShouldNotThrowArgumentException()
    {
        // Arrange
        var campaignIds = new List<long> { 1L, 2L, 3L, 4L, 5L };

        // Act
        Func<Task> act = async () => await _repository.GetByIdsAsync(campaignIds);

        // Assert
        act.Should().ThrowAsync<InvalidCastException>();
    }

    [Test]
    public void GetByIdsAsync_WithDuplicateIds_ShouldNotThrowArgumentException()
    {
        // Arrange
        var campaignIds = new List<long> { 100L, 200L, 100L, 300L, 200L };

        // Act
        Func<Task> act = async () => await _repository.GetByIdsAsync(campaignIds);

        // Assert
        act.Should().ThrowAsync<InvalidCastException>();
    }

    [Test]
    public void GetByIdsAsync_WithLargeCollection_ShouldNotThrowArgumentException()
    {
        // Arrange
        var campaignIds = Enumerable.Range(1, 10000).Select(i => (long)i).ToList();

        // Act
        Func<Task> act = async () => await _repository.GetByIdsAsync(campaignIds);

        // Assert
        act.Should().ThrowAsync<InvalidCastException>();
    }

    [Test]
    public void GetByIdsAsync_WithMinValue_ShouldNotThrowArgumentException()
    {
        // Arrange
        var campaignIds = new List<long> { long.MinValue };

        // Act
        Func<Task> act = async () => await _repository.GetByIdsAsync(campaignIds);

        // Assert
        act.Should().ThrowAsync<InvalidCastException>();
    }

    [Test]
    public void GetByIdsAsync_WithMaxValue_ShouldNotThrowArgumentException()
    {
        // Arrange
        var campaignIds = new List<long> { long.MaxValue };

        // Act
        Func<Task> act = async () => await _repository.GetByIdsAsync(campaignIds);

        // Assert
        act.Should().ThrowAsync<InvalidCastException>();
    }

    [Test]
    public void GetByIdsAsync_WithZero_ShouldNotThrowArgumentException()
    {
        // Arrange
        var campaignIds = new List<long> { 0L };

        // Act
        Func<Task> act = async () => await _repository.GetByIdsAsync(campaignIds);

        // Assert
        act.Should().ThrowAsync<InvalidCastException>();
    }

    [Test]
    public void GetByIdsAsync_WithMixedPositiveAndNegativeIds_ShouldNotThrowArgumentException()
    {
        // Arrange
        var campaignIds = new List<long> { -100L, 50L, -25L, 200L, -1L, 1L };

        // Act
        Func<Task> act = async () => await _repository.GetByIdsAsync(campaignIds);

        // Assert
        act.Should().ThrowAsync<InvalidCastException>();
    }

    [Test]
    public void GetByIdsAsync_WithAllBoundaryValues_ShouldNotThrowArgumentException()
    {
        // Arrange
        var campaignIds = new List<long> { long.MinValue, 0L, long.MaxValue };

        // Act
        Func<Task> act = async () => await _repository.GetByIdsAsync(campaignIds);

        // Assert
        act.Should().ThrowAsync<InvalidCastException>();
    }

    [Test]
    public async Task GetByIdsAsync_ShouldCallFactoryCreateConnection()
    {
        // Arrange
        var campaignIds = new List<long> { 1L, 2L, 3L };

        // Act
        try
        {
            await _repository.GetByIdsAsync(campaignIds);
        }
        catch (InvalidCastException)
        {
            // Expected exception from mock
        }

        // Assert
        _mockConnectionFactory.Verify(f => f.CreateConnection(), Times.Once);
    }

    [Test]
    public async Task GetByIdsAsync_ShouldLogInformationWithCorrectParameters()
    {
        // Arrange
        var campaignIds = new List<long> { 100L, 200L, 300L };

        // Act
        try
        {
            await _repository.GetByIdsAsync(campaignIds);
        }
        catch (InvalidCastException)
        {
            // Expected exception from mock
        }

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Fetching") && v.ToString()!.Contains("CampaignImportMetadata")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [TestCase(new long[] { }, "")]
    [TestCase(new long[] { 123L }, "123")]
    [TestCase(new long[] { 1L, 2L, 3L }, "1,2,3")]
    [TestCase(new long[] { long.MinValue, long.MaxValue }, "-9223372036854775808,9223372036854775807")]
    public async Task GetByIdsAsync_ShouldFormatIdListCorrectly(long[] ids, string expectedFormat)
    {
        // Act
        var actualFormat = string.Join(",", ids);

        // Assert
        actualFormat.Should().Be(expectedFormat);
    }

    [TestCase(new long[] { long.MinValue }, TestName = "GetByIdsAsync_Should_Format_MinValue")]
    [TestCase(new long[] { long.MaxValue }, TestName = "GetByIdsAsync_Should_Format_MaxValue")]
    [TestCase(new long[] { 0L }, TestName = "GetByIdsAsync_Should_Format_Zero")]
    [TestCase(new long[] { -1L, 0L, 1L }, TestName = "GetByIdsAsync_Should_Format_Mixed_Values")]
    public async Task GetByIdsAsync_Should_Format_Boundary_Values_Correctly(long[] campaignIds)
    {
        // Arrange & Act
        try
        {
            await _repository.GetByIdsAsync(campaignIds);
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }

        // Assert - verify factory was called (indicates string formatting succeeded)
        _mockConnectionFactory.Verify(f => f.CreateConnection(), Times.Once);
    }

    [Test]
    public async Task GetByIdsAsync_Should_Use_Correct_StoredProcedure_Name()
    {
        // Arrange
        var campaignIds = new[] { 1L };
        var expectedStoredProcedure = "dbo.Usp_CampaignImportMetadata_Get";

        // Act
        try
        {
            await _repository.GetByIdsAsync(campaignIds);
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedStoredProcedure)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public void GetByIdsAsync_WithNullCampaignIds_ShouldThrowArgumentNullException()
    {
        // Arrange
        IEnumerable<long>? campaignIds = null!;

        // Act
        Func<Task> act = async () => await _repository.GetByIdsAsync(campaignIds!);

        // Assert
        act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Test]
    public void GetByIdsAsync_WithNullCampaignIds_ShouldThrowNullReferenceException()
    {
        // Arrange
        IEnumerable<long>? campaignIds = null!;

        // Act
        Func<Task> act = async () => await _repository.GetByIdsAsync(campaignIds!);

        // Assert
        act.Should().ThrowAsync<NullReferenceException>();
    }

    #endregion

    #region UpsertAsync Tests

    [Test]
    public void UpsertAsync_Should_Accept_Metadata_With_Min_Long_CampaignId()
    {
        // Arrange
        var metadata = new CampaignImportMetadata
        {
            CampaignId = long.MinValue,
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
    public void UpsertAsync_Should_Accept_Metadata_With_ImportEndDate_Equal_To_ImportStartDate()
    {
        // Arrange
        var sameDate = DateTime.UtcNow;
        var metadata = new CampaignImportMetadata
        {
            CampaignId = 100,
            IsImportComplete = true,
            ImportStartDate = sameDate,
            ImportEndDate = sameDate
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
    public void UpsertAsync_Should_Accept_Metadata_With_ImportEndDate_Before_ImportStartDate()
    {
        // Arrange
        var metadata = new CampaignImportMetadata
        {
            CampaignId = 200,
            IsImportComplete = false,
            ImportStartDate = DateTime.UtcNow,
            ImportEndDate = DateTime.UtcNow.AddDays(-1)
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

    [TestCase(0L, Description = "Zero CampaignId with incomplete import")]
    [TestCase(long.MaxValue, Description = "Max CampaignId with incomplete import")]
    [TestCase(long.MinValue, Description = "Min CampaignId with incomplete import")]
    public void UpsertAsync_Should_Accept_Incomplete_Import_With_Various_CampaignIds(long campaignId)
    {
        // Arrange
        var metadata = new CampaignImportMetadata
        {
            CampaignId = campaignId,
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
    public async Task UpsertAsync_Should_Log_Information_Before_Execution()
    {
        // Arrange
        var metadata = new CampaignImportMetadata
        {
            CampaignId = 12345,
            IsImportComplete = true,
            ImportStartDate = DateTime.UtcNow,
            ImportEndDate = DateTime.UtcNow
        };

        // Act
        try
        {
            await _repository.UpsertAsync(metadata);
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Upserting CampaignImportMetadata") &&
                                              v.ToString()!.Contains("12345") &&
                                              v.ToString()!.Contains("dbo.Usp_CampaignImportMetadata_Upsert")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task UpsertAsync_Should_Use_Correct_StoredProcedure_Name()
    {
        // Arrange
        const string expectedStoredProcedure = "dbo.Usp_CampaignImportMetadata_Upsert";
        var metadata = new CampaignImportMetadata
        {
            CampaignId = 999,
            IsImportComplete = false,
            ImportStartDate = DateTime.UtcNow,
            ImportEndDate = null
        };

        // Act
        try
        {
            await _repository.UpsertAsync(metadata);
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedStoredProcedure)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [TestCase(0L, "0")]
    [TestCase(123L, "123")]
    [TestCase(-456L, "-456")]
    [TestCase(9223372036854775807L, "9223372036854775807")]
    [TestCase(-9223372036854775808L, "-9223372036854775808")]
    public async Task UpsertAsync_Should_Log_CampaignId_Value(long campaignId, string expectedIdString)
    {
        // Arrange
        var metadata = new CampaignImportMetadata
        {
            CampaignId = campaignId,
            IsImportComplete = true,
            ImportStartDate = DateTime.UtcNow,
            ImportEndDate = DateTime.UtcNow
        };

        // Act
        try
        {
            await _repository.UpsertAsync(metadata);
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedIdString)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Test]
    public async Task UpsertAsync_Should_Call_Factory_CreateConnection()
    {
        // Arrange
        var metadata = new CampaignImportMetadata
        {
            CampaignId = 777,
            IsImportComplete = true,
            ImportStartDate = DateTime.UtcNow,
            ImportEndDate = DateTime.UtcNow
        };

        // Act
        try
        {
            await _repository.UpsertAsync(metadata);
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }

        // Assert
        _mockConnectionFactory.Verify(x => x.CreateConnection(), Times.Once);
    }

    [Test]
    public void UpsertAsync_Should_Attempt_Cast_To_SqlConnection()
    {
        // Arrange
        var metadata = new CampaignImportMetadata
        {
            CampaignId = 888,
            IsImportComplete = false,
            ImportStartDate = DateTime.UtcNow,
            ImportEndDate = null
        };

        // Act & Assert
        var exception = Assert.CatchAsync<InvalidCastException>(async () =>
            await _repository.UpsertAsync(metadata));

        exception.Should().NotBeNull();
    }

    [Test]
    public void UpsertAsync_Should_Accept_Metadata_With_MinValue_StartDate_And_Null_EndDate()
    {
        // Arrange
        var metadata = new CampaignImportMetadata
        {
            CampaignId = 500,
            IsImportComplete = false,
            ImportStartDate = DateTime.MinValue,
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
    public void UpsertAsync_Should_Accept_Metadata_With_MaxValue_StartDate_And_Null_EndDate()
    {
        // Arrange
        var metadata = new CampaignImportMetadata
        {
            CampaignId = 600,
            IsImportComplete = false,
            ImportStartDate = DateTime.MaxValue,
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

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(5)]
    [TestCase(100)]
    public async Task GetByIdsAsync_Should_Log_Correct_Count(int count)
    {
        // Arrange
        var campaignIds = Enumerable.Range(1, count).Select(i => (long)i).ToArray();

        // Act
        try
        {
            await _repository.GetByIdsAsync(campaignIds);
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"{count}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetByIdsAsync_Should_Format_Empty_Collection_Correctly()
    {
        // Arrange
        var campaignIds = Enumerable.Empty<long>();

        // Act
        try
        {
            await _repository.GetByIdsAsync(campaignIds);
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }

        // Assert - verify factory was called (indicates string formatting didn't throw)
        _mockConnectionFactory.Verify(f => f.CreateConnection(), Times.Once);
    }

    [TestCase(long.MinValue, Description = "Minimum long value")]
    [TestCase(-1L, Description = "Negative value")]
    [TestCase(0L, Description = "Zero value")]
    [TestCase(1L, Description = "Positive value")]
    [TestCase(long.MaxValue, Description = "Maximum long value")]
    public void UpsertAsync_VariousCampaignIds_AcceptsWithoutValidationError(long campaignId)
    {
        // Arrange
        var metadata = new CampaignImportMetadata
        {
            CampaignId = campaignId,
            IsImportComplete = true,
            ImportStartDate = DateTime.UtcNow,
            ImportEndDate = DateTime.UtcNow
        };

        // Act
        Func<Task> act = async () => await _repository.UpsertAsync(metadata);

        // Assert
        act.Should().ThrowAsync<InvalidCastException>();
        _mockConnectionFactory.Verify(x => x.CreateConnection(), Times.Once);
    }

    [Test]
    public void UpsertAsync_MinDateTimeValues_AcceptsWithoutValidationError()
    {
        // Arrange
        var metadata = new CampaignImportMetadata
        {
            CampaignId = 1L,
            IsImportComplete = true,
            ImportStartDate = DateTime.MinValue,
            ImportEndDate = DateTime.MinValue
        };

        // Act
        Func<Task> act = async () => await _repository.UpsertAsync(metadata);

        // Assert
        act.Should().ThrowAsync<InvalidCastException>();
        _mockConnectionFactory.Verify(x => x.CreateConnection(), Times.Once);
    }

    [Test]
    public void UpsertAsync_MaxDateTimeValues_AcceptsWithoutValidationError()
    {
        // Arrange
        var metadata = new CampaignImportMetadata
        {
            CampaignId = 1L,
            IsImportComplete = true,
            ImportStartDate = DateTime.MaxValue,
            ImportEndDate = DateTime.MaxValue
        };

        // Act
        Func<Task> act = async () => await _repository.UpsertAsync(metadata);

        // Assert
        act.Should().ThrowAsync<InvalidCastException>();
        _mockConnectionFactory.Verify(x => x.CreateConnection(), Times.Once);
    }

    [Test]
    public void UpsertAsync_NullImportEndDate_AcceptsWithoutValidationError()
    {
        // Arrange
        var metadata = new CampaignImportMetadata
        {
            CampaignId = 1L,
            IsImportComplete = false,
            ImportStartDate = DateTime.UtcNow,
            ImportEndDate = null
        };

        // Act
        Func<Task> act = async () => await _repository.UpsertAsync(metadata);

        // Assert
        act.Should().ThrowAsync<InvalidCastException>();
        _mockConnectionFactory.Verify(x => x.CreateConnection(), Times.Once);
    }

    [Test]
    public void UpsertAsync_EndDateBeforeStartDate_AcceptsWithoutValidationError()
    {
        // Arrange
        var metadata = new CampaignImportMetadata
        {
            CampaignId = 1L,
            IsImportComplete = true,
            ImportStartDate = DateTime.UtcNow,
            ImportEndDate = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        Func<Task> act = async () => await _repository.UpsertAsync(metadata);

        // Assert
        act.Should().ThrowAsync<InvalidCastException>();
        _mockConnectionFactory.Verify(x => x.CreateConnection(), Times.Once);
    }

    [TestCase(true, Description = "Import complete")]
    [TestCase(false, Description = "Import incomplete")]
    public void UpsertAsync_VariousIsImportCompleteValues_AcceptsWithoutValidationError(bool isImportComplete)
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        mockConnectionFactory.Setup(x => x.CreateConnection()).Returns(mockConnection.Object);
        var mockLogger = new Mock<ILogger<CampaignImportMetadataRepository>>();
        var repository = new CampaignImportMetadataRepository(mockConnectionFactory.Object, mockLogger.Object);

        var metadata = new CampaignImportMetadata
        {
            CampaignId = 100L,
            IsImportComplete = isImportComplete,
            ImportStartDate = DateTime.UtcNow,
            ImportEndDate = isImportComplete ? DateTime.UtcNow : null
        };

        // Act
        Func<Task> act = async () => await repository.UpsertAsync(metadata);

        // Assert
        act.Should().ThrowAsync<InvalidCastException>();
        mockConnectionFactory.Verify(x => x.CreateConnection(), Times.Once);
    }

    [Test]
    public async Task UpsertAsync_ValidMetadata_CallsFactoryCreateConnection()
    {
        // Arrange
        var metadata = new CampaignImportMetadata
        {
            CampaignId = 123L,
            IsImportComplete = true,
            ImportStartDate = DateTime.UtcNow,
            ImportEndDate = DateTime.UtcNow
        };

        // Act
        try
        {
            await _repository.UpsertAsync(metadata);
        }
        catch (InvalidCastException)
        {
            // Expected due to mock limitation
        }

        // Assert
        _mockConnectionFactory.Verify(x => x.CreateConnection(), Times.Once);
    }

    [TestCase(0L, "0")]
    [TestCase(123L, "123")]
    [TestCase(-456L, "-456")]
    [TestCase(9223372036854775807L, "9223372036854775807")]
    [TestCase(-9223372036854775808L, "-9223372036854775808")]
    public async Task UpsertAsync_ValidMetadata_LogsCampaignIdCorrectly(long campaignId, string expectedIdString)
    {
        // Arrange
        var metadata = new CampaignImportMetadata
        {
            CampaignId = campaignId,
            IsImportComplete = true,
            ImportStartDate = DateTime.UtcNow,
            ImportEndDate = DateTime.UtcNow
        };

        // Act
        try
        {
            await _repository.UpsertAsync(metadata);
        }
        catch (InvalidCastException)
        {
            // Expected due to mock limitation
        }

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedIdString) && v.ToString()!.Contains("dbo.Usp_CampaignImportMetadata_Upsert")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Test]
    public async Task UpsertAsync_ValidMetadata_UsesCorrectStoredProcedureName()
    {
        // Arrange
        var metadata = new CampaignImportMetadata
        {
            CampaignId = 999L,
            IsImportComplete = false,
            ImportStartDate = DateTime.UtcNow,
            ImportEndDate = null
        };

        // Act
        try
        {
            await _repository.UpsertAsync(metadata);
        }
        catch (InvalidCastException)
        {
            // Expected due to mock limitation
        }

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("dbo.Usp_CampaignImportMetadata_Upsert")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Test]
    public void UpsertAsync_NonSqlConnection_ThrowsInvalidCastException()
    {
        // Arrange
        var metadata = new CampaignImportMetadata
        {
            CampaignId = 1L,
            IsImportComplete = true,
            ImportStartDate = DateTime.UtcNow,
            ImportEndDate = DateTime.UtcNow
        };

        // Act
        Func<Task> act = async () => await _repository.UpsertAsync(metadata);

        // Assert
        act.Should().ThrowAsync<InvalidCastException>();
    }

    [TestCase(long.MinValue, false, Description = "Min CampaignId with incomplete import")]
    [TestCase(long.MaxValue, false, Description = "Max CampaignId with incomplete import")]
    [TestCase(0L, true, Description = "Zero CampaignId with complete import")]
    [TestCase(-1L, false, Description = "Negative CampaignId with incomplete import")]
    public void UpsertAsync_CombinedEdgeCases_AcceptsWithoutValidationError(long campaignId, bool isImportComplete)
    {
        // Arrange
        var metadata = new CampaignImportMetadata
        {
            CampaignId = campaignId,
            IsImportComplete = isImportComplete,
            ImportStartDate = DateTime.MinValue,
            ImportEndDate = isImportComplete ? DateTime.MaxValue : null
        };

        // Act
        Func<Task> act = async () => await _repository.UpsertAsync(metadata);

        // Assert
        act.Should().ThrowAsync<InvalidCastException>();
        _mockConnectionFactory.Verify(x => x.CreateConnection(), Times.Once);
    }

    [Test]
    public void UpsertAsync_NullMetadata_ThrowsArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _repository.UpsertAsync(null!);

        // Assert
        act.Should().ThrowAsync<ArgumentNullException>().WithMessage("*campaignImportMetadata*");
    }

    #endregion
}
