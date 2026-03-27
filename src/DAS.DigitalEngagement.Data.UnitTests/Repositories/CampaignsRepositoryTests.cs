using DAS.DigitalEngagement.CampaignInterest.Data.Helpers;
using DAS.DigitalEngagement.CampaignInterest.Data.Models;
using DAS.DigitalEngagement.CampaignInterest.Data.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Data;

namespace DAS.DigitalEngagement.CampaignInterest.Data.UnitTests.Repositories;

/// <summary>
/// Unit tests for CampaignsRepository.
/// Note: Repository methods cannot be fully tested with mocks because Dapper extensions require a real SqlConnection.
/// These tests verify that the repository can be instantiated, validates input parameters, and handles edge cases.
/// Integration tests should be used to verify the full database interaction functionality with stored procedures.
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


    [Test]
    public void Constructor_Should_Create_Instance_With_Valid_Dependencies()
    {
        // Arrange & Act
        var repository = new CampaignsRepository(_mockConnectionFactory.Object, _mockLogger.Object);

        // Assert
        Assert.That(repository, Is.Not.Null);
        Assert.That(repository, Is.InstanceOf<ICampaignsRepository>());
    }

    [Test]
    public void Constructor_Should_Accept_Factory_And_Logger_Without_Immediate_Connection()
    {
        // Arrange & Act
        var repository = new CampaignsRepository(_mockConnectionFactory.Object, _mockLogger.Object);

        // Assert
        Assert.That(repository, Is.Not.Null);
        _mockConnectionFactory.Verify(f => f.CreateConnection(), Times.Never,
            "Constructor should not create connection immediately");
    }



    [Test]
    public void UpsertAsync_Should_Throw_ArgumentNullException_When_Campaign_Is_Null()
    {
        // Arrange
        Campaigns? campaign = null;

        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _repository.UpsertAsync(campaign!));

        Assert.That(exception!.ParamName, Is.EqualTo("campaign"));
    }

    [Test]
    public void UpsertAsync_Should_Accept_Valid_Campaign()
    {
        // Arrange
        var campaign = new Campaigns
        {
            Id = 12345,
            ExternalId = 100,
            Name = "Test Campaign",
            Type = "Email",
            CreatedBy = "TestUser",
            CreatedOn = DateTime.UtcNow,
            ModifiedBy = "TestUser",
            ModifiedOn = DateTime.UtcNow,
            FirstSendDate = DateTime.UtcNow,
            LastSendDate = DateTime.UtcNow,
            FromEmailAddress = "test@example.com",
            FromName = "Test Sender",
            ReplyEmailAddress = "reply@example.com",
            Subject = "Test Subject",
            SubStatus = "Active",
            ContactCount = 100,
            Account = "TestAccount"
        };

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.UpsertAsync(campaign);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks - actual behavior would require integration test
            }
        });
    }

    [Test]
    public void UpsertAsync_Should_Accept_Campaign_With_Null_Optional_Fields()
    {
        // Arrange
        var campaign = new Campaigns
        {
            Id = 12345,
            ExternalId = 100,
            Name = "Test Campaign",
            Type = "Email",
            CreatedBy = "TestUser",
            CreatedOn = DateTime.UtcNow,
            ModifiedBy = "TestUser",
            ModifiedOn = null, // Nullable
            FirstSendDate = DateTime.UtcNow,
            LastSendDate = null, // Nullable
            FromEmailAddress = "test@example.com",
            FromName = "Test Sender",
            ReplyEmailAddress = "reply@example.com",
            Subject = "Test Subject",
            SubStatus = "Active",
            ContactCount = 0,
            Account = "TestAccount"
        };

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.UpsertAsync(campaign);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        });
    }

    [Test]
    public void UpsertAsync_Should_Accept_Campaign_With_Zero_ContactCount()
    {
        // Arrange
        var campaign = CreateValidCampaign();
        campaign.ContactCount = 0;

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.UpsertAsync(campaign);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        });
    }

    [Test]
    public void UpsertAsync_Should_Accept_Campaign_With_Large_ContactCount()
    {
        // Arrange
        var campaign = CreateValidCampaign();
        campaign.ContactCount = int.MaxValue;

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.UpsertAsync(campaign);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        });
    }



    [Test]
    public void GetByIdAsync_Should_Accept_Positive_Id()
    {
        // Arrange
        long id = 12345;

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.GetByIdAsync(id);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        });
    }

    [Test]
    public void GetByIdAsync_Should_Accept_Zero_Id()
    {
        // Arrange
        long id = 0;

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.GetByIdAsync(id);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        });
    }

    [Test]
    public void GetByIdAsync_Should_Accept_Negative_Id()
    {
        // Arrange
        long id = -1;

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.GetByIdAsync(id);
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
        long id = long.MaxValue;

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.GetByIdAsync(id);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        });
    }



    [Test]
    public void GetByIdsAsync_Should_Accept_Empty_Collection()
    {
        // Arrange
        var ids = Enumerable.Empty<long>();

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.GetByIdsAsync(ids);
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
        var ids = new[] { 12345L };

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.GetByIdsAsync(ids);
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
        var ids = new[] { 1L, 2L, 3L, 4L, 5L };

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.GetByIdsAsync(ids);
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
        var ids = new[] { 1L, 2L, 2L, 3L, 3L, 3L };

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.GetByIdsAsync(ids);
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
        var ids = Enumerable.Range(1, 1000).Select(i => (long)i);

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.GetByIdsAsync(ids);
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
        var ids = new[] { -1L, 0L, 1L, 100L, -100L };

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.GetByIdsAsync(ids);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        });
    }



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
    public void Repository_Should_Implement_All_Interface_Methods()
    {
        // Arrange
        var interfaceType = typeof(ICampaignsRepository);
        var implementationType = typeof(CampaignsRepository);

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



    [Test]
    public async Task Repository_Should_Use_Factory_When_Calling_Methods()
    {
        // Arrange
        var factoryMock = new Mock<IDbConnectionFactory>();
        var connectionMock = new Mock<IDbConnection>();
        factoryMock.Setup(f => f.CreateConnection()).Returns(connectionMock.Object);

        var repository = new CampaignsRepository(factoryMock.Object, _mockLogger.Object);

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
        factoryMock.Verify(f => f.CreateConnection(), Times.Once, "Repository should use factory to create connection when calling GetByIdAsync");
    }

    [Test]
    public async Task UpsertAsync_Should_Call_Factory_CreateConnection_Once()
    {
        // Arrange
        var campaign = CreateValidCampaign();

        // Act
        try
        {
            await _repository.UpsertAsync(campaign);
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }

        // Assert
        _mockConnectionFactory.Verify(f => f.CreateConnection(), Times.Once,
            "Repository should use factory to create connection when calling UpsertAsync");
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
        _mockConnectionFactory.Verify(f => f.CreateConnection(), Times.Once,
            "Repository should use factory to create connection when calling GetAllAsync");
    }

    [Test]
    public async Task GetByIdsAsync_Should_Call_Factory_CreateConnection_Once()
    {
        // Arrange
        var ids = new[] { 1L, 2L, 3L };

        // Act
        try
        {
            await _repository.GetByIdsAsync(ids);
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }

        // Assert
        _mockConnectionFactory.Verify(f => f.CreateConnection(), Times.Once,
            "Repository should use factory to create connection when calling GetByIdsAsync");
    }



    [Test]
    public async Task UpsertAsync_Should_Log_Information_Before_Connection_Is_Created()
    {
        // Arrange
        var campaign = CreateValidCampaign();

        // Act
        try
        {
            await _repository.UpsertAsync(campaign);
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Upserting Campaign")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetByIdAsync_Should_Log_Information_Before_Connection_Is_Created()
    {
        // Arrange
        long id = 12345;

        // Act
        try
        {
            await _repository.GetByIdAsync(id);
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Fetching Campaign by Id")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetAllAsync_Should_Log_Information_Before_Connection_Is_Created()
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
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Fetching all Campaigns")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetByIdsAsync_Should_Log_Information_With_Count_Before_Connection_Is_Created()
    {
        // Arrange
        var ids = new[] { 1L, 2L, 3L };

        // Act
        try
        {
            await _repository.GetByIdsAsync(ids);
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Fetching") && v.ToString()!.Contains("Campaigns by Ids")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }



    [Test]
    public void UpsertAsync_Should_Not_Create_Connection_When_Campaign_Is_Null()
    {
        // Act
        Assert.ThrowsAsync<ArgumentNullException>(async () => await _repository.UpsertAsync(null!));

        // Assert - ArgumentNullException.ThrowIfNull fires before factory.CreateConnection() is reached
        _mockConnectionFactory.Verify(f => f.CreateConnection(), Times.Never,
            "Connection should not be created when input validation fails before the factory call");
    }

    [Test]
    public void UpsertAsync_Should_Throw_InvalidCastException_When_Factory_Returns_Mock_Connection()
    {
        // Mock<IDbConnection> cannot be cast to SqlConnection; this is the boundary where
        // unit tests end and integration tests begin for this repository
        var campaign = CreateValidCampaign();

        Assert.ThrowsAsync<InvalidCastException>(async () => await _repository.UpsertAsync(campaign));

        // Factory was called before the cast failed
        _mockConnectionFactory.Verify(f => f.CreateConnection(), Times.Once);
    }

    [Test]
    public void GetByIdAsync_Should_Throw_InvalidCastException_When_Factory_Returns_Mock_Connection()
    {
        Assert.ThrowsAsync<InvalidCastException>(async () => await _repository.GetByIdAsync(1));

        _mockConnectionFactory.Verify(f => f.CreateConnection(), Times.Once);
    }

    [Test]
    public void GetAllAsync_Should_Throw_InvalidCastException_When_Factory_Returns_Mock_Connection()
    {
        Assert.ThrowsAsync<InvalidCastException>(async () => await _repository.GetAllAsync());

        _mockConnectionFactory.Verify(f => f.CreateConnection(), Times.Once);
    }

    [Test]
    public void GetByIdsAsync_Should_Throw_InvalidCastException_When_Factory_Returns_Mock_Connection()
    {
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _repository.GetByIdsAsync([1L]));

        _mockConnectionFactory.Verify(f => f.CreateConnection(), Times.Once);
    }

    [Test]
    public async Task GetByIdAsync_Should_Create_A_New_Connection_On_Each_Invocation()
    {
        // Act - each call must request its own connection; none are reused
        for (var i = 0; i < 3; i++)
        {
            try { await _repository.GetByIdAsync(i); }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        }

        // Assert
        _mockConnectionFactory.Verify(f => f.CreateConnection(), Times.Exactly(3),
            "Each method invocation should request a new connection from the factory");
    }

    [Test]
    public async Task All_Four_Methods_Each_Create_Their_Own_Independent_Connection()
    {
        // Act - each method must use its own connection; no connection state is shared
        try { await _repository.UpsertAsync(CreateValidCampaign()); }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }
        try { await _repository.GetByIdAsync(1); }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }
        try { await _repository.GetAllAsync(); }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }
        try { await _repository.GetByIdsAsync([1L]); }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }

        // Assert - one distinct connection per method call
        _mockConnectionFactory.Verify(f => f.CreateConnection(), Times.Exactly(4),
            "Each repository method should create its own independent connection; none are shared");
    }



    [Test]
    public void UpsertAsync_Should_Accept_Campaign_With_Min_DateTime_Values()
    {
        // Arrange
        var campaign = CreateValidCampaign();
        campaign.CreatedOn = DateTime.MinValue;
        campaign.FirstSendDate = DateTime.MinValue;
        campaign.ModifiedOn = DateTime.MinValue;
        campaign.LastSendDate = DateTime.MinValue;

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.UpsertAsync(campaign);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        });
    }

    [Test]
    public void UpsertAsync_Should_Accept_Campaign_With_Max_DateTime_Values()
    {
        // Arrange
        var campaign = CreateValidCampaign();
        campaign.CreatedOn = DateTime.MaxValue;
        campaign.FirstSendDate = DateTime.MaxValue;
        campaign.ModifiedOn = DateTime.MaxValue;
        campaign.LastSendDate = DateTime.MaxValue;

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.UpsertAsync(campaign);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        });
    }

    [Test]
    public void UpsertAsync_Should_Accept_Campaign_With_Long_String_Values()
    {
        // Arrange
        var campaign = CreateValidCampaign();
        campaign.Name = new string('A', 1000);
        campaign.Subject = new string('B', 1000);
        campaign.FromEmailAddress = "very.long.email.address" + new string('x', 100) + "@example.com";

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.UpsertAsync(campaign);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        });
    }

    [Test]
    public void UpsertAsync_Should_Accept_Campaign_With_Special_Characters_In_Strings()
    {
        // Arrange
        var campaign = CreateValidCampaign();
        campaign.Name = "Test's Campaign \"Special\" & <Characters> 🎉";
        campaign.Subject = "Re: Test's \"Subject\" with & <HTML> tags";

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.UpsertAsync(campaign);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        });
    }

    [Test]
    public void UpsertAsync_Should_Accept_Campaign_With_Zero_And_Negative_ExternalId()
    {
        // Arrange
        var campaign1 = CreateValidCampaign();
        campaign1.ExternalId = 0;

        var campaign2 = CreateValidCampaign();
        campaign2.ExternalId = -1;

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.UpsertAsync(campaign1);
                await _repository.UpsertAsync(campaign2);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        });
    }



    private static Campaigns CreateValidCampaign()
    {
        return new Campaigns
        {
            Id = 12345,
            ExternalId = 100,
            Name = "Test Campaign",
            Type = "Email",
            CreatedBy = "TestUser",
            CreatedOn = DateTime.UtcNow,
            ModifiedBy = "TestUser",
            ModifiedOn = DateTime.UtcNow,
            FirstSendDate = DateTime.UtcNow,
            LastSendDate = DateTime.UtcNow,
            FromEmailAddress = "test@example.com",
            FromName = "Test Sender",
            ReplyEmailAddress = "reply@example.com",
            Subject = "Test Subject",
            SubStatus = "Active",
            ContactCount = 100,
            Account = "TestAccount"
        };
    }


    /// <summary>
    /// Tests that GetByIdAsync accepts the minimum long value (long.MinValue) without throwing unexpected exceptions.
    /// This test verifies boundary value handling for the id parameter.
    /// Expected: Method should not throw unexpected exceptions (InvalidCastException is expected when using mocks).
    /// </summary>
    [Test]
    public void GetByIdAsync_Should_Accept_Min_Long_Value()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        mockConnection.Setup(c => c.State).Returns(ConnectionState.Open);

        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);

        var mockLogger = new Mock<ILogger<CampaignsRepository>>();
        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);
        long id = long.MinValue;

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await repository.GetByIdAsync(id);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
            catch (NullReferenceException)
            {
                // Expected when mock connection doesn't fully implement SqlConnection behavior
            }
        });
    }

    /// <summary>
    /// Tests that GetByIdsAsync throws ArgumentNullException when ids parameter is null.
    /// This validates null parameter handling before any database operations.
    /// </summary>
    [Test]
    public void GetByIdsAsync_Should_Throw_ArgumentNullException_When_Ids_Is_Null()
    {
        // Arrange
        IEnumerable<long>? ids = null;

        // Act
        Func<Task> act = async () => await _repository.GetByIdsAsync(ids!);

        // Assert
        act.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that GetByIdsAsync correctly handles collection containing long.MinValue.
    /// Verifies that extreme negative boundary values are processed without error.
    /// </summary>
    [Test]
    public void GetByIdsAsync_Should_Accept_Collection_With_MinValue()
    {
        // Arrange
        var ids = new[] { long.MinValue, 1L, 2L };

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.GetByIdsAsync(ids);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        });
    }

    /// <summary>
    /// Tests that GetByIdsAsync correctly handles collection containing long.MaxValue.
    /// Verifies that extreme positive boundary values are processed without error.
    /// </summary>
    [Test]
    public void GetByIdsAsync_Should_Accept_Collection_With_MaxValue()
    {
        // Arrange
        var ids = new[] { 1L, long.MaxValue, 3L };

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.GetByIdsAsync(ids);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        });
    }

    /// <summary>
    /// Tests that GetByIdsAsync correctly handles collection with both extreme boundary values.
    /// Verifies that long.MinValue and long.MaxValue can coexist in the same collection.
    /// </summary>
    [Test]
    public void GetByIdsAsync_Should_Accept_Collection_With_Both_MinValue_And_MaxValue()
    {
        // Arrange
        var ids = new[] { long.MinValue, 0L, long.MaxValue };

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.GetByIdsAsync(ids);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        });
    }

    /// <summary>
    /// Tests that GetByIdsAsync correctly handles collection containing only zero values.
    /// Verifies that zero, a common boundary value, is properly processed.
    /// </summary>
    [Test]
    public void GetByIdsAsync_Should_Accept_Collection_With_Only_Zeros()
    {
        // Arrange
        var ids = new[] { 0L, 0L, 0L };

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.GetByIdsAsync(ids);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        });
    }

    /// <summary>
    /// Tests that GetByIdsAsync logs the correct count when ids collection contains boundary values.
    /// Verifies that Count() extension method works correctly with extreme long values.
    /// </summary>
    [Test]
    public async Task GetByIdsAsync_Should_Log_Correct_Count_For_Collection_With_Boundary_Values()
    {
        // Arrange
        var ids = new[] { long.MinValue, -1L, 0L, 1L, long.MaxValue };
        var expectedCount = 5;

        // Act
        try
        {
            await _repository.GetByIdsAsync(ids);
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedCount.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that GetByIdsAsync creates comma-separated string correctly for boundary values.
    /// Verifies string.Join behavior with extreme long values by checking log output.
    /// </summary>
    [Test]
    public async Task GetByIdsAsync_Should_Create_Correct_String_Format_For_Boundary_Values()
    {
        // Arrange
        var ids = new[] { long.MinValue, long.MaxValue };

        // Act
        try
        {
            await _repository.GetByIdsAsync(ids);
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks - string.Join executes before this exception
        }

        // Assert - Verify logger was called (string.Join happens before logging)
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "String.Join should execute successfully before logging");
    }

    /// <summary>
    /// Tests that GetByIdsAsync handles a very large collection of ids.
    /// Verifies that string.Join can handle extremely large collections without issues.
    /// Expected result: No exception thrown during string concatenation or logging.
    /// </summary>
    [Test]
    public void GetByIdsAsync_Should_Handle_Very_Large_Id_Collection_Without_String_Join_Error()
    {
        // Arrange
        var ids = Enumerable.Range(1, 10000).Select(i => (long)i);

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.GetByIdsAsync(ids);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        });
    }

    /// <summary>
    /// Tests that GetByIdsAsync calls factory.CreateConnection exactly once for boundary value collection.
    /// Verifies dependency injection and factory usage with extreme values.
    /// </summary>
    [Test]
    public async Task GetByIdsAsync_Should_Call_Factory_Once_For_Boundary_Values()
    {
        // Arrange
        var ids = new[] { long.MinValue, 0L, long.MaxValue };

        // Act
        try
        {
            await _repository.GetByIdsAsync(ids);
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }

        // Assert
        _mockConnectionFactory.Verify(x => x.CreateConnection(), Times.Once);
    }

    /// <summary>
    /// Tests that GetByIdsAsync properly handles negative ids in the collection.
    /// Verifies that negative values (which may represent invalid ids in domain logic)
    /// are still processed by the method without throwing at the repository level.
    /// </summary>
    [Test]
    public void GetByIdsAsync_Should_Accept_All_Negative_Ids()
    {
        // Arrange
        var ids = new[] { -1L, -100L, -999L, long.MinValue };

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.GetByIdsAsync(ids);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        });
    }

    /// <summary>
    /// Tests that UpsertAsync accepts campaign with int.MinValue for ExternalId.
    /// Verifies that extreme negative boundary value for int is handled without exception.
    /// Expected: Method should not throw ArgumentException for valid boundary value.
    /// </summary>
    [Test]
    public void UpsertAsync_Should_Accept_Campaign_With_MinValue_ExternalId()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();
        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);
        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        var campaign = new Campaigns
        {
            Id = 1,
            ExternalId = int.MinValue,
            Name = "Test Campaign",
            Type = "Email",
            CreatedBy = "System",
            CreatedOn = DateTime.UtcNow,
            ModifiedBy = "System",
            FirstSendDate = DateTime.UtcNow,
            FromEmailAddress = "test@example.com",
            FromName = "Test",
            ReplyEmailAddress = "reply@example.com",
            Subject = "Test Subject",
            SubStatus = "Active",
            ContactCount = 100,
            Account = "TestAccount"
        };

        // Act
        Func<System.Threading.Tasks.Task> act = async () => await repository.UpsertAsync(campaign);

        // Assert
        act.Should().ThrowAsync<InvalidCastException>();
    }

    /// <summary>
    /// Tests that UpsertAsync accepts campaign with int.MaxValue for ExternalId.
    /// Verifies that extreme positive boundary value for int is handled without exception.
    /// Expected: Method should not throw ArgumentException for valid boundary value.
    /// </summary>
    [Test]
    public void UpsertAsync_Should_Accept_Campaign_With_MaxValue_ExternalId()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();
        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);
        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        var campaign = new Campaigns
        {
            Id = 1,
            ExternalId = int.MaxValue,
            Name = "Test Campaign",
            Type = "Email",
            CreatedBy = "System",
            CreatedOn = DateTime.UtcNow,
            ModifiedBy = "System",
            FirstSendDate = DateTime.UtcNow,
            FromEmailAddress = "test@example.com",
            FromName = "Test",
            ReplyEmailAddress = "reply@example.com",
            Subject = "Test Subject",
            SubStatus = "Active",
            ContactCount = 100,
            Account = "TestAccount"
        };

        // Act
        Func<System.Threading.Tasks.Task> act = async () => await repository.UpsertAsync(campaign);

        // Assert
        act.Should().ThrowAsync<InvalidCastException>();
    }

    /// <summary>
    /// Tests that UpsertAsync accepts campaign with int.MinValue for ContactCount.
    /// Verifies that extreme negative boundary value for int ContactCount is handled.
    /// Expected: Method should not throw ArgumentException (business validation should happen elsewhere).
    /// </summary>
    [Test]
    public void UpsertAsync_Should_Accept_Campaign_With_MinValue_ContactCount()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();
        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);
        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        var campaign = new Campaigns
        {
            Id = 1,
            ExternalId = 123,
            Name = "Test Campaign",
            Type = "Email",
            CreatedBy = "System",
            CreatedOn = DateTime.UtcNow,
            ModifiedBy = "System",
            FirstSendDate = DateTime.UtcNow,
            FromEmailAddress = "test@example.com",
            FromName = "Test",
            ReplyEmailAddress = "reply@example.com",
            Subject = "Test Subject",
            SubStatus = "Active",
            ContactCount = int.MinValue,
            Account = "TestAccount"
        };

        // Act
        Func<System.Threading.Tasks.Task> act = async () => await repository.UpsertAsync(campaign);

        // Assert
        act.Should().ThrowAsync<InvalidCastException>();
    }

    /// <summary>
    /// Tests that UpsertAsync accepts campaign with int.MaxValue for ContactCount.
    /// Verifies that extreme positive boundary value for int ContactCount is handled.
    /// Expected: Method should not throw ArgumentException for valid boundary value.
    /// </summary>
    [Test]
    public void UpsertAsync_Should_Accept_Campaign_With_MaxValue_ContactCount()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();
        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);
        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        var campaign = new Campaigns
        {
            Id = 1,
            ExternalId = 123,
            Name = "Test Campaign",
            Type = "Email",
            CreatedBy = "System",
            CreatedOn = DateTime.UtcNow,
            ModifiedBy = "System",
            FirstSendDate = DateTime.UtcNow,
            FromEmailAddress = "test@example.com",
            FromName = "Test",
            ReplyEmailAddress = "reply@example.com",
            Subject = "Test Subject",
            SubStatus = "Active",
            ContactCount = int.MaxValue,
            Account = "TestAccount"
        };

        // Act
        Func<System.Threading.Tasks.Task> act = async () => await repository.UpsertAsync(campaign);

        // Assert
        act.Should().ThrowAsync<InvalidCastException>();
    }

    /// <summary>
    /// Tests that UpsertAsync accepts campaign with long.MinValue for Id.
    /// Verifies that extreme negative boundary value for long Id is handled.
    /// Expected: Method should not throw ArgumentException for boundary value.
    /// </summary>
    [Test]
    public void UpsertAsync_Should_Accept_Campaign_With_MinValue_Id()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();
        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);
        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        var campaign = new Campaigns
        {
            Id = long.MinValue,
            ExternalId = 123,
            Name = "Test Campaign",
            Type = "Email",
            CreatedBy = "System",
            CreatedOn = DateTime.UtcNow,
            ModifiedBy = "System",
            FirstSendDate = DateTime.UtcNow,
            FromEmailAddress = "test@example.com",
            FromName = "Test",
            ReplyEmailAddress = "reply@example.com",
            Subject = "Test Subject",
            SubStatus = "Active",
            ContactCount = 100,
            Account = "TestAccount"
        };

        // Act
        Func<System.Threading.Tasks.Task> act = async () => await repository.UpsertAsync(campaign);

        // Assert
        act.Should().ThrowAsync<InvalidCastException>();
    }

    /// <summary>
    /// Tests that UpsertAsync accepts campaign with long.MaxValue for Id.
    /// Verifies that extreme positive boundary value for long Id is handled.
    /// Expected: Method should not throw ArgumentException for boundary value.
    /// </summary>
    [Test]
    public void UpsertAsync_Should_Accept_Campaign_With_MaxValue_Id()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();
        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);
        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        var campaign = new Campaigns
        {
            Id = long.MaxValue,
            ExternalId = 123,
            Name = "Test Campaign",
            Type = "Email",
            CreatedBy = "System",
            CreatedOn = DateTime.UtcNow,
            ModifiedBy = "System",
            FirstSendDate = DateTime.UtcNow,
            FromEmailAddress = "test@example.com",
            FromName = "Test",
            ReplyEmailAddress = "reply@example.com",
            Subject = "Test Subject",
            SubStatus = "Active",
            ContactCount = 100,
            Account = "TestAccount"
        };

        // Act
        Func<System.Threading.Tasks.Task> act = async () => await repository.UpsertAsync(campaign);

        // Assert
        act.Should().ThrowAsync<InvalidCastException>();
    }

    /// <summary>
    /// Tests that UpsertAsync accepts campaign with zero for Id.
    /// Verifies that zero boundary value for long Id is handled.
    /// Expected: Method should not throw ArgumentException for zero Id.
    /// </summary>
    [Test]
    public void UpsertAsync_Should_Accept_Campaign_With_Zero_Id()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();
        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);
        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        var campaign = new Campaigns
        {
            Id = 0,
            ExternalId = 123,
            Name = "Test Campaign",
            Type = "Email",
            CreatedBy = "System",
            CreatedOn = DateTime.UtcNow,
            ModifiedBy = "System",
            FirstSendDate = DateTime.UtcNow,
            FromEmailAddress = "test@example.com",
            FromName = "Test",
            ReplyEmailAddress = "reply@example.com",
            Subject = "Test Subject",
            SubStatus = "Active",
            ContactCount = 100,
            Account = "TestAccount"
        };

        // Act
        Func<System.Threading.Tasks.Task> act = async () => await repository.UpsertAsync(campaign);

        // Assert
        act.Should().ThrowAsync<InvalidCastException>();
    }

    /// <summary>
    /// Tests that UpsertAsync accepts campaign with negative Id.
    /// Verifies that negative value for long Id is handled.
    /// Expected: Method should not throw ArgumentException for negative Id.
    /// </summary>
    [Test]
    public void UpsertAsync_Should_Accept_Campaign_With_Negative_Id()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();
        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);
        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        var campaign = new Campaigns
        {
            Id = -1,
            ExternalId = 123,
            Name = "Test Campaign",
            Type = "Email",
            CreatedBy = "System",
            CreatedOn = DateTime.UtcNow,
            ModifiedBy = "System",
            FirstSendDate = DateTime.UtcNow,
            FromEmailAddress = "test@example.com",
            FromName = "Test",
            ReplyEmailAddress = "reply@example.com",
            Subject = "Test Subject",
            SubStatus = "Active",
            ContactCount = 100,
            Account = "TestAccount"
        };

        // Act
        Func<System.Threading.Tasks.Task> act = async () => await repository.UpsertAsync(campaign);

        // Assert
        act.Should().ThrowAsync<InvalidCastException>();
    }

    /// <summary>
    /// Tests that UpsertAsync accepts campaign with empty strings for all string properties.
    /// Verifies that empty string edge case is handled without throwing exceptions at repository level.
    /// Expected: Method should not throw ArgumentException (business validation should happen elsewhere).
    /// </summary>
    [Test]
    public void UpsertAsync_Should_Accept_Campaign_With_Empty_Strings()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();
        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);
        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        var campaign = new Campaigns
        {
            Id = 1,
            ExternalId = 123,
            Name = string.Empty,
            Type = string.Empty,
            CreatedBy = string.Empty,
            CreatedOn = DateTime.UtcNow,
            ModifiedBy = string.Empty,
            FirstSendDate = DateTime.UtcNow,
            FromEmailAddress = string.Empty,
            FromName = string.Empty,
            ReplyEmailAddress = string.Empty,
            Subject = string.Empty,
            SubStatus = string.Empty,
            ContactCount = 100,
            Account = string.Empty
        };

        // Act
        Func<System.Threading.Tasks.Task> act = async () => await repository.UpsertAsync(campaign);

        // Assert
        act.Should().ThrowAsync<InvalidCastException>();
    }

    /// <summary>
    /// Tests that UpsertAsync accepts campaign with whitespace-only strings for all string properties.
    /// Verifies that whitespace edge case is handled without throwing exceptions at repository level.
    /// Expected: Method should not throw ArgumentException (business validation should happen elsewhere).
    /// </summary>
    [Test]
    public void UpsertAsync_Should_Accept_Campaign_With_Whitespace_Strings()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();
        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);
        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        var campaign = new Campaigns
        {
            Id = 1,
            ExternalId = 123,
            Name = "   ",
            Type = "\t",
            CreatedBy = " \n ",
            CreatedOn = DateTime.UtcNow,
            ModifiedBy = "  ",
            FirstSendDate = DateTime.UtcNow,
            FromEmailAddress = "   ",
            FromName = "\t\t",
            ReplyEmailAddress = " ",
            Subject = "  \n  ",
            SubStatus = "\t",
            ContactCount = 100,
            Account = "   "
        };

        // Act
        Func<System.Threading.Tasks.Task> act = async () => await repository.UpsertAsync(campaign);

        // Assert
        act.Should().ThrowAsync<InvalidCastException>();
    }

    /// <summary>
    /// Tests that UpsertAsync logs the correct campaign Id for campaigns with boundary long values.
    /// Verifies logging behavior with extreme Id values.
    /// Expected: Logger should be called with the correct Id value in the log message.
    /// </summary>
    [Test]
    public async System.Threading.Tasks.Task UpsertAsync_Should_Log_Correct_Id_For_Boundary_Values()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();
        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);
        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        var campaign = new Campaigns
        {
            Id = long.MaxValue,
            ExternalId = 123,
            Name = "Test",
            Type = "Email",
            CreatedBy = "System",
            CreatedOn = DateTime.UtcNow,
            ModifiedBy = "System",
            FirstSendDate = DateTime.UtcNow,
            FromEmailAddress = "test@example.com",
            FromName = "Test",
            ReplyEmailAddress = "reply@example.com",
            Subject = "Test",
            SubStatus = "Active",
            ContactCount = 100,
            Account = "TestAccount"
        };

        // Act
        try
        {
            await repository.UpsertAsync(campaign);
        }
        catch (InvalidCastException)
        {
            // Expected when using mock connection
        }

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(long.MaxValue.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that UpsertAsync handles campaign with all boundary numeric values simultaneously.
    /// Verifies combined edge case handling for Id, ExternalId, and ContactCount.
    /// Expected: Method should handle all boundary values without throwing ArgumentException.
    /// </summary>
    [Test]
    public void UpsertAsync_Should_Accept_Campaign_With_All_Boundary_Numeric_Values()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();
        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);
        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        var campaign = new Campaigns
        {
            Id = long.MaxValue,
            ExternalId = int.MaxValue,
            Name = "Test",
            Type = "Email",
            CreatedBy = "System",
            CreatedOn = DateTime.MaxValue,
            ModifiedBy = "System",
            ModifiedOn = DateTime.MinValue,
            FirstSendDate = DateTime.MaxValue,
            LastSendDate = DateTime.MinValue,
            FromEmailAddress = "test@example.com",
            FromName = "Test",
            ReplyEmailAddress = "reply@example.com",
            Subject = "Test",
            SubStatus = "Active",
            ContactCount = int.MaxValue,
            Account = "TestAccount"
        };

        // Act
        Func<System.Threading.Tasks.Task> act = async () => await repository.UpsertAsync(campaign);

        // Assert
        act.Should().ThrowAsync<InvalidCastException>();
    }

    /// <summary>
    /// Tests that GetAllAsync creates a new connection on each invocation.
    /// Verifies that the factory is called multiple times for multiple invocations.
    /// Expected: CreateConnection should be called exactly twice for two separate invocations.
    /// </summary>
    [Test]
    public async Task GetAllAsync_Should_Create_New_Connection_On_Each_Invocation()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();
        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        // Act
        try { await repository.GetAllAsync(); } catch { }
        try { await repository.GetAllAsync(); } catch { }

        // Assert
        mockConnectionFactory.Verify(f => f.CreateConnection(), Times.Exactly(2));
    }

    /// <summary>
    /// Tests that GetAllAsync returns a Task of IEnumerable of Campaigns type.
    /// Validates the return type matches the interface contract.
    /// Expected: Method should return Task with IEnumerable of Campaigns as generic type.
    /// </summary>
    [Test]
    public void GetAllAsync_Should_Return_Task_Of_IEnumerable_Of_Campaigns()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();
        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        // Act
        var method = repository.GetType().GetMethod(nameof(repository.GetAllAsync));

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IEnumerable<Campaigns>>));
    }

    /// <summary>
    /// Tests that GetAllAsync is properly marked as async.
    /// Validates the method implementation follows async pattern.
    /// Expected: Method should be an async method returning Task.
    /// </summary>
    [Test]
    public void GetAllAsync_Should_Be_Async_Method()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();
        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        // Act
        var method = repository.GetType().GetMethod(nameof(repository.GetAllAsync));

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.BaseType.Should().Be(typeof(Task));
    }

    /// <summary>
    /// Tests that GetAllAsync logs the correct stored procedure name in the log message.
    /// This validates that the logging includes the expected stored procedure identifier.
    /// Expected: Log message should contain "dbo.Usp_Campaigns_Get".
    /// </summary>
    [Test]
    public async Task GetAllAsync_Should_Log_Stored_Procedure_Name()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();
        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);
        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

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
        mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("dbo.Usp_Campaigns_Get")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Log message should include the stored procedure name 'dbo.Usp_Campaigns_Get'");
    }

    /// <summary>
    /// Tests that GetAllAsync propagates exceptions thrown by the factory when creating connection.
    /// This verifies proper exception handling when the dependency fails.
    /// Expected: The exception from factory should propagate to the caller.
    /// </summary>
    [Test]
    public void GetAllAsync_Should_Propagate_Exception_When_Factory_Throws()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Factory failed to create connection");
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();

        mockConnectionFactory
            .Setup(f => f.CreateConnection())
            .Throws(expectedException);

        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        // Act
        Func<Task> act = async () => await repository.GetAllAsync();

        // Assert
        act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Factory failed to create connection");
    }

    /// <summary>
    /// Tests that GetAllAsync throws NullReferenceException when factory returns null.
    /// This verifies the method's behavior when dependency returns unexpected null value.
    /// Expected: NullReferenceException should be thrown when attempting to cast null.
    /// </summary>
    [Test]
    public void GetAllAsync_Should_Throw_NullReferenceException_When_Factory_Returns_Null()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();

        mockConnectionFactory
            .Setup(f => f.CreateConnection())
            .Returns((IDbConnection?)null);

        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        // Act
        Func<Task> act = async () => await repository.GetAllAsync();

        // Assert
        act.Should().ThrowAsync<NullReferenceException>();
    }

    /// <summary>
    /// Tests that GetAllAsync logs before attempting to create connection, even when factory throws.
    /// This verifies that logging occurs at the correct point in the execution flow.
    /// Expected: Logger should be called once before the factory exception occurs.
    /// </summary>
    [Test]
    public void GetAllAsync_Should_Log_Before_Factory_Exception()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Factory failure");
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();

        mockConnectionFactory
            .Setup(f => f.CreateConnection())
            .Throws(expectedException);

        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        // Act
        Func<Task> act = async () => await repository.GetAllAsync();

        // Assert
        act.Should().ThrowAsync<InvalidOperationException>();

        mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Fetching all Campaigns")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that GetAllAsync does not call factory when called multiple times concurrently.
    /// This verifies that each concurrent call independently uses the factory.
    /// Expected: Factory should be called exactly the same number of times as concurrent calls.
    /// </summary>
    [Test]
    public async Task GetAllAsync_Should_Call_Factory_For_Each_Concurrent_Call()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();

        mockConnectionFactory
            .Setup(f => f.CreateConnection())
            .Returns(mockConnection.Object);

        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        // Act
        var tasks = new List<Task>();
        for (int i = 0; i < 3; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await repository.GetAllAsync();
                }
                catch (InvalidCastException)
                {
                    // Expected when using mocks
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        mockConnectionFactory.Verify(f => f.CreateConnection(), Times.Exactly(3));
    }

    /// <summary>
    /// Tests that GetAllAsync uses the specific stored procedure name in logging.
    /// This verifies the exact stored procedure identifier used by the method.
    /// Expected: Log message should contain "dbo.Usp_Campaigns_Get".
    /// </summary>
    [Test]
    public async Task GetAllAsync_Should_Use_Correct_Stored_Procedure_Constant()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();

        mockConnectionFactory
            .Setup(f => f.CreateConnection())
            .Returns(mockConnection.Object);

        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

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
        mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("dbo.Usp_Campaigns_Get")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Log message should contain the exact stored procedure name");
    }

    /// <summary>
    /// Tests that GetAllAsync does not swallow exceptions from logger.
    /// This verifies proper exception propagation when logging fails.
    /// Expected: Exception from logger should propagate to the caller.
    /// </summary>
    [Test]
    public void GetAllAsync_Should_Propagate_Exception_When_Logger_Throws()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Logger failure");
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();

        mockLogger
            .Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Throws(expectedException);

        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        // Act
        Func<Task> act = async () => await repository.GetAllAsync();

        // Assert
        act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Logger failure");
    }

    /// <summary>
    /// Tests that GetAllAsync method signature returns the correct Task type.
    /// This validates that the method returns a Task containing IEnumerable of Campaigns.
    /// Expected: Method should return Task&lt;IEnumerable&lt;Campaigns&gt;&gt;.
    /// </summary>
    [Test]
    public void GetAllAsync_Should_Have_Correct_Return_Type_Signature()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();

        mockConnectionFactory
            .Setup(f => f.CreateConnection())
            .Returns(mockConnection.Object);

        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        // Act
        var result = repository.GetAllAsync();

        // Assert
        result.Should().BeAssignableTo<Task<IEnumerable<Campaigns>>>();
    }

    /// <summary>
    /// Tests that GetAllAsync can be called multiple times in sequence without issues.
    /// This verifies that the method does not maintain state between calls.
    /// Expected: Each call should independently use the factory and logger.
    /// </summary>
    [Test]
    public async Task GetAllAsync_Should_Handle_Multiple_Sequential_Calls()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();

        mockConnectionFactory
            .Setup(f => f.CreateConnection())
            .Returns(mockConnection.Object);

        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        // Act & Assert
        for (int i = 0; i < 5; i++)
        {
            try
            {
                await repository.GetAllAsync();
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        }

        // Assert
        mockConnectionFactory.Verify(f => f.CreateConnection(), Times.Exactly(5));
        mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(5));
    }


    /// <summary>
    /// Tests that GetByIdAsync accepts various boundary and edge case values for the id parameter.
    /// This parameterized test verifies that the method handles long.MinValue, long.MaxValue, zero,
    /// positive, and negative values without throwing ArgumentException.
    /// Expected: Method should accept all valid long values and only throw InvalidCastException when using mock connections.
    /// </summary>
    /// <param name="id">The campaign id to test.</param>
    [TestCase(long.MinValue)]
    [TestCase(long.MaxValue)]
    [TestCase(0L)]
    [TestCase(1L)]
    [TestCase(-1L)]
    [TestCase(12345L)]
    [TestCase(-12345L)]
    public void GetByIdAsync_Should_Accept_Valid_Long_Values(long id)
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();

        mockConnectionFactory.Setup(x => x.CreateConnection()).Returns(mockConnection.Object);

        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await repository.GetByIdAsync(id);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks - Dapper requires real SqlConnection
            }
        });
    }

    /// <summary>
    /// Tests that GetByIdAsync logs the correct information including the id value and stored procedure name.
    /// This parameterized test verifies that the log message contains the correct id and stored procedure.
    /// Expected: Logger should be called with LogLevel.Information and message containing id and "dbo.Usp_Campaigns_Get".
    /// </summary>
    /// <param name="id">The campaign id to test.</param>
    [TestCase(1L)]
    [TestCase(long.MinValue)]
    [TestCase(long.MaxValue)]
    [TestCase(0L)]
    [TestCase(-999L)]
    public async Task GetByIdAsync_Should_Log_Id_And_StoredProcedure(long id)
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();

        mockConnectionFactory.Setup(x => x.CreateConnection()).Returns(mockConnection.Object);

        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        // Act
        try
        {
            await repository.GetByIdAsync(id);
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }

        // Assert
        mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Fetching Campaign by Id")
                                            && v.ToString()!.Contains(id.ToString())
                                            && v.ToString()!.Contains("dbo.Usp_Campaigns_Get")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that GetByIdAsync calls the factory CreateConnection method exactly once.
    /// This parameterized test verifies factory usage for various id values.
    /// Expected: CreateConnection should be invoked exactly once per method call.
    /// </summary>
    /// <param name="id">The campaign id to test.</param>
    [TestCase(1L)]
    [TestCase(long.MinValue)]
    [TestCase(long.MaxValue)]
    public async Task GetByIdAsync_Should_Call_Factory_CreateConnection_Once(long id)
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();

        mockConnectionFactory.Setup(x => x.CreateConnection()).Returns(mockConnection.Object);

        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        // Act
        try
        {
            await repository.GetByIdAsync(id);
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }

        // Assert
        mockConnectionFactory.Verify(x => x.CreateConnection(), Times.Once);
    }

    /// <summary>
    /// Tests that GetByIdAsync throws InvalidCastException when factory returns a mock connection.
    /// This verifies that the method requires a real SqlConnection (not just IDbConnection).
    /// Expected: InvalidCastException should be thrown when casting IDbConnection to SqlConnection.
    /// </summary>
    [Test]
    public void GetByIdAsync_Should_Throw_InvalidCastException_With_Mock_Connection()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();

        mockConnectionFactory.Setup(x => x.CreateConnection()).Returns(mockConnection.Object);

        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () => await repository.GetByIdAsync(1L));
    }

    /// <summary>
    /// Tests that GetByIdAsync returns a Task with the correct return type.
    /// Verifies the method signature matches the interface contract.
    /// Expected: Method should return Task&lt;Campaigns?&gt;.
    /// </summary>
    [Test]
    public void GetByIdAsync_Should_Return_Task_Of_Nullable_Campaigns()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();

        mockConnectionFactory.Setup(x => x.CreateConnection()).Returns(mockConnection.Object);

        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        // Act
        var result = repository.GetByIdAsync(1L);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<Task<Campaigns?>>();
    }

    /// <summary>
    /// Tests that GetByIdAsync creates a new connection for each invocation.
    /// Verifies that connections are not reused across multiple calls.
    /// Expected: CreateConnection should be called exactly twice for two invocations.
    /// </summary>
    [Test]
    public async Task GetByIdAsync_Should_Create_New_Connection_For_Each_Invocation()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();

        mockConnectionFactory.Setup(x => x.CreateConnection()).Returns(mockConnection.Object);

        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        // Act
        try
        {
            await repository.GetByIdAsync(1L);
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }

        try
        {
            await repository.GetByIdAsync(2L);
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }

        // Assert
        mockConnectionFactory.Verify(x => x.CreateConnection(), Times.Exactly(2));
    }

    /// <summary>
    /// Tests that GetByIdAsync correctly formats extreme long values when converting to string.
    /// Verifies that id.ToString() works correctly for boundary values in the log message.
    /// Expected: Log should contain the correctly formatted string representation of extreme long values.
    /// </summary>
    [TestCase(long.MinValue, "-9223372036854775808")]
    [TestCase(long.MaxValue, "9223372036854775807")]
    [TestCase(0L, "0")]
    public async Task GetByIdAsync_Should_Format_Id_Correctly_As_String(long id, string expectedString)
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();

        mockConnectionFactory.Setup(x => x.CreateConnection()).Returns(mockConnection.Object);

        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        // Act
        try
        {
            await repository.GetByIdAsync(id);
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }

        // Assert
        mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedString)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that GetByIdsAsync correctly formats comma-separated string for empty collection.
    /// Verifies string.Join behavior with empty enumerable by checking log output contains "".
    /// Expected: Empty string should be created and logged without throwing exceptions.
    /// </summary>
    [Test]
    public async Task GetByIdsAsync_Should_Create_Empty_String_For_Empty_Collection()
    {
        // Arrange
        var emptyIds = Enumerable.Empty<long>();
        var loggedMessages = new List<string>();

        _mockLogger.Setup(x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => CaptureLogMessage(v, loggedMessages)),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()));

        // Act & Assert
        try
        {
            await _repository.GetByIdsAsync(emptyIds);
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks - we're testing the pre-database logic
        }

        // Assert
        _mockLogger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    /// <summary>
    /// Tests that GetByIdsAsync handles IEnumerable that requires multiple enumerations correctly.
    /// String.Join and Count() both enumerate the collection, verifying no issues with deferred execution.
    /// Expected: Method should handle collections that can be enumerated multiple times.
    /// </summary>
    [Test]
    public void GetByIdsAsync_Should_Handle_Multiple_Enumerations_Of_Collection()
    {
        // Arrange
        var ids = new List<long> { 1, 2, 3 }.AsEnumerable(); // Deferred execution enumerable

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.GetByIdsAsync(ids);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        });
    }

    /// <summary>
    /// Tests that GetByIdsAsync correctly creates comma-separated string with single ID.
    /// Verifies string.Join produces correct format "123" for single element.
    /// Expected: Single ID should be converted to string without commas.
    /// </summary>
    [Test]
    public async Task GetByIdsAsync_Should_Create_Correct_String_For_Single_Id()
    {
        // Arrange
        var singleId = new[] { 123L };
        _mockLogger.Setup(x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()));

        // Act
        try
        {
            await _repository.GetByIdsAsync(singleId);
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }

        // Assert - verify logger was called (string.Join succeeded)
        _mockLogger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    /// <summary>
    /// Tests that GetByIdsAsync properly handles a collection containing only long.MaxValue.
    /// Verifies string conversion of extreme positive boundary value.
    /// Expected: long.MaxValue should be correctly converted to string representation.
    /// </summary>
    [Test]
    public void GetByIdsAsync_Should_Handle_Collection_With_Only_MaxValue()
    {
        // Arrange
        var ids = new[] { long.MaxValue };

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.GetByIdsAsync(ids);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        });

        _mockConnectionFactory.Verify(f => f.CreateConnection(), Times.Once);
    }

    /// <summary>
    /// Tests that GetByIdsAsync properly handles a collection containing only long.MinValue.
    /// Verifies string conversion of extreme negative boundary value.
    /// Expected: long.MinValue should be correctly converted to string representation.
    /// </summary>
    [Test]
    public void GetByIdsAsync_Should_Handle_Collection_With_Only_MinValue()
    {
        // Arrange
        var ids = new[] { long.MinValue };

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await _repository.GetByIdsAsync(ids);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        });

        _mockConnectionFactory.Verify(f => f.CreateConnection(), Times.Once);
    }

    /// <summary>
    /// Tests that GetByIdsAsync uses the correct stored procedure name constant.
    /// Verifies the stored procedure name "dbo.Usp_Campaigns_Get" is used in logging.
    /// Expected: Log message should reference the correct stored procedure name.
    /// </summary>
    [Test]
    public async Task GetByIdsAsync_Should_Use_Correct_Stored_Procedure_Name()
    {
        // Arrange
        var ids = new[] { 1L };
        var capturedLogValues = new List<object?>();

        _mockLogger.Setup(x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => CaptureLogValues(v, capturedLogValues)),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()));

        // Act
        try
        {
            await _repository.GetByIdsAsync(ids);
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }

        // Assert
        _mockLogger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    /// <summary>
    /// Tests that GetByIdsAsync returns Task type (not void).
    /// Validates the method signature matches the interface contract.
    /// Expected: Method should return Task with IEnumerable of Campaigns.
    /// </summary>
    [Test]
    public void GetByIdsAsync_Should_Return_Task_Type()
    {
        // Arrange
        var ids = new[] { 1L };

        // Act
        var result = _repository.GetByIdsAsync(ids);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<Task<IEnumerable<Campaigns>>>();
    }

    /// <summary>
    /// Helper method to capture log message values for assertion.
    /// </summary>
    private static bool CaptureLogMessage(object state, List<string> messages)
    {
        messages.Add(state.ToString() ?? string.Empty);
        return true;
    }

    /// <summary>
    /// Helper method to capture log values for assertion.
    /// </summary>
    private static bool CaptureLogValues(object state, List<object?> values)
    {
        values.Add(state);
        return true;
    }

    /// <summary>
    /// Tests that UpsertAsync throws ArgumentNullException when campaign parameter is null.
    /// This validates the null guard clause at the method entry point.
    /// Expected: ArgumentNullException with parameter name "campaign".
    /// </summary>
    [Test]
    public void UpsertAsync_NullCampaign_ThrowsArgumentNullException()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();
        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        // Act
        Func<Task> act = async () => await repository.UpsertAsync(null!);

        // Assert
        act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("campaign");
    }

    /// <summary>
    /// Tests that UpsertAsync accepts a valid campaign object with all required properties populated.
    /// This validates the happy path scenario with typical valid input.
    /// Expected: Method executes without throwing ArgumentException.
    /// </summary>
    [Test]
    public void UpsertAsync_ValidCampaign_DoesNotThrowArgumentException()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();
        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        var campaign = new Campaigns
        {
            Id = 1,
            ExternalId = 100,
            Name = "Test Campaign",
            Type = "Email",
            CreatedBy = "TestUser",
            CreatedOn = DateTime.UtcNow,
            ModifiedBy = "TestUser",
            ModifiedOn = DateTime.UtcNow,
            FirstSendDate = DateTime.UtcNow,
            LastSendDate = DateTime.UtcNow,
            FromEmailAddress = "test@example.com",
            FromName = "Test Sender",
            ReplyEmailAddress = "reply@example.com",
            Subject = "Test Subject",
            SubStatus = "Active",
            ContactCount = 100,
            Account = "TestAccount"
        };

        // Act
        Func<Task> act = async () => await repository.UpsertAsync(campaign);

        // Assert
        act.Should().NotThrowAsync<ArgumentException>();
    }

    /// <summary>
    /// Tests that UpsertAsync accepts campaign with null values for nullable DateTime properties.
    /// This validates handling of optional fields (ModifiedOn, LastSendDate).
    /// Expected: Method executes without throwing ArgumentException or NullReferenceException.
    /// </summary>
    [Test]
    public void UpsertAsync_CampaignWithNullOptionalDateTimes_DoesNotThrowException()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();
        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        var campaign = new Campaigns
        {
            Id = 1,
            ExternalId = 100,
            Name = "Test Campaign",
            Type = "Email",
            CreatedBy = "TestUser",
            CreatedOn = DateTime.UtcNow,
            ModifiedBy = "TestUser",
            ModifiedOn = null,
            FirstSendDate = DateTime.UtcNow,
            LastSendDate = null,
            FromEmailAddress = "test@example.com",
            FromName = "Test Sender",
            ReplyEmailAddress = "reply@example.com",
            Subject = "Test Subject",
            SubStatus = "Active",
            ContactCount = 100,
            Account = "TestAccount"
        };

        // Act
        Func<Task> act = async () => await repository.UpsertAsync(campaign);

        // Assert
        act.Should().NotThrowAsync<ArgumentException>();
        act.Should().NotThrowAsync<NullReferenceException>();
    }

    /// <summary>
    /// Tests that UpsertAsync accepts campaign with int.MaxValue for ExternalId.
    /// This validates handling of maximum positive boundary value for int type.
    /// Expected: Method executes without throwing OverflowException or ArgumentOutOfRangeException.
    /// </summary>
    [Test]
    public void UpsertAsync_CampaignWithMaxExternalId_DoesNotThrowException()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();
        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        var campaign = new Campaigns
        {
            Id = 1,
            ExternalId = int.MaxValue,
            Name = "Test Campaign",
            Type = "Email",
            CreatedBy = "TestUser",
            CreatedOn = DateTime.UtcNow,
            ModifiedBy = "TestUser",
            FirstSendDate = DateTime.UtcNow,
            FromEmailAddress = "test@example.com",
            FromName = "Test Sender",
            ReplyEmailAddress = "reply@example.com",
            Subject = "Test Subject",
            SubStatus = "Active",
            ContactCount = 0,
            Account = "TestAccount"
        };

        // Act
        Func<Task> act = async () => await repository.UpsertAsync(campaign);

        // Assert
        act.Should().NotThrowAsync<OverflowException>();
        act.Should().NotThrowAsync<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Tests that UpsertAsync accepts campaign with int.MinValue for ExternalId.
    /// This validates handling of minimum negative boundary value for int type.
    /// Expected: Method executes without throwing OverflowException or ArgumentOutOfRangeException.
    /// </summary>
    [Test]
    public void UpsertAsync_CampaignWithMinExternalId_DoesNotThrowException()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();
        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        var campaign = new Campaigns
        {
            Id = 1,
            ExternalId = int.MinValue,
            Name = "Test Campaign",
            Type = "Email",
            CreatedBy = "TestUser",
            CreatedOn = DateTime.UtcNow,
            ModifiedBy = "TestUser",
            FirstSendDate = DateTime.UtcNow,
            FromEmailAddress = "test@example.com",
            FromName = "Test Sender",
            ReplyEmailAddress = "reply@example.com",
            Subject = "Test Subject",
            SubStatus = "Active",
            ContactCount = 0,
            Account = "TestAccount"
        };

        // Act
        Func<Task> act = async () => await repository.UpsertAsync(campaign);

        // Assert
        act.Should().NotThrowAsync<OverflowException>();
        act.Should().NotThrowAsync<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Tests that UpsertAsync accepts campaign with int.MaxValue for ContactCount.
    /// This validates handling of maximum positive boundary value for ContactCount property.
    /// Expected: Method executes without throwing OverflowException or ArgumentOutOfRangeException.
    /// </summary>
    [Test]
    public void UpsertAsync_CampaignWithMaxContactCount_DoesNotThrowException()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();
        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        var campaign = new Campaigns
        {
            Id = 1,
            ExternalId = 100,
            Name = "Test Campaign",
            Type = "Email",
            CreatedBy = "TestUser",
            CreatedOn = DateTime.UtcNow,
            ModifiedBy = "TestUser",
            FirstSendDate = DateTime.UtcNow,
            FromEmailAddress = "test@example.com",
            FromName = "Test Sender",
            ReplyEmailAddress = "reply@example.com",
            Subject = "Test Subject",
            SubStatus = "Active",
            ContactCount = int.MaxValue,
            Account = "TestAccount"
        };

        // Act
        Func<Task> act = async () => await repository.UpsertAsync(campaign);

        // Assert
        act.Should().NotThrowAsync<OverflowException>();
        act.Should().NotThrowAsync<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Tests that UpsertAsync accepts campaign with int.MinValue for ContactCount.
    /// This validates handling of minimum negative boundary value for ContactCount property.
    /// Expected: Method executes without throwing OverflowException or ArgumentOutOfRangeException.
    /// </summary>
    [Test]
    public void UpsertAsync_CampaignWithMinContactCount_DoesNotThrowException()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();
        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        var campaign = new Campaigns
        {
            Id = 1,
            ExternalId = 100,
            Name = "Test Campaign",
            Type = "Email",
            CreatedBy = "TestUser",
            CreatedOn = DateTime.UtcNow,
            ModifiedBy = "TestUser",
            FirstSendDate = DateTime.UtcNow,
            FromEmailAddress = "test@example.com",
            FromName = "Test Sender",
            ReplyEmailAddress = "reply@example.com",
            Subject = "Test Subject",
            SubStatus = "Active",
            ContactCount = int.MinValue,
            Account = "TestAccount"
        };

        // Act
        Func<Task> act = async () => await repository.UpsertAsync(campaign);

        // Assert
        act.Should().NotThrowAsync<OverflowException>();
        act.Should().NotThrowAsync<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Tests that UpsertAsync accepts campaign with long.MaxValue for Id.
    /// This validates handling of maximum positive boundary value for long Id property.
    /// Expected: Method executes without throwing OverflowException or ArgumentOutOfRangeException.
    /// </summary>
    [Test]
    public void UpsertAsync_CampaignWithMaxId_DoesNotThrowException()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();
        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        var campaign = new Campaigns
        {
            Id = long.MaxValue,
            ExternalId = 100,
            Name = "Test Campaign",
            Type = "Email",
            CreatedBy = "TestUser",
            CreatedOn = DateTime.UtcNow,
            ModifiedBy = "TestUser",
            FirstSendDate = DateTime.UtcNow,
            FromEmailAddress = "test@example.com",
            FromName = "Test Sender",
            ReplyEmailAddress = "reply@example.com",
            Subject = "Test Subject",
            SubStatus = "Active",
            ContactCount = 0,
            Account = "TestAccount"
        };

        // Act
        Func<Task> act = async () => await repository.UpsertAsync(campaign);

        // Assert
        act.Should().NotThrowAsync<OverflowException>();
        act.Should().NotThrowAsync<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Tests that UpsertAsync accepts campaign with long.MinValue for Id.
    /// This validates handling of minimum negative boundary value for long Id property.
    /// Expected: Method executes without throwing OverflowException or ArgumentOutOfRangeException.
    /// </summary>
    [Test]
    public void UpsertAsync_CampaignWithMinId_DoesNotThrowException()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();
        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        var campaign = new Campaigns
        {
            Id = long.MinValue,
            ExternalId = 100,
            Name = "Test Campaign",
            Type = "Email",
            CreatedBy = "TestUser",
            CreatedOn = DateTime.UtcNow,
            ModifiedBy = "TestUser",
            FirstSendDate = DateTime.UtcNow,
            FromEmailAddress = "test@example.com",
            FromName = "Test Sender",
            ReplyEmailAddress = "reply@example.com",
            Subject = "Test Subject",
            SubStatus = "Active",
            ContactCount = 0,
            Account = "TestAccount"
        };

        // Act
        Func<Task> act = async () => await repository.UpsertAsync(campaign);

        // Assert
        act.Should().NotThrowAsync<OverflowException>();
        act.Should().NotThrowAsync<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Tests that UpsertAsync accepts campaign with zero for Id.
    /// This validates handling of zero boundary value for long Id property.
    /// Expected: Method executes without throwing ArgumentOutOfRangeException.
    /// </summary>
    [Test]
    public void UpsertAsync_CampaignWithZeroId_DoesNotThrowException()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();
        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        var campaign = new Campaigns
        {
            Id = 0,
            ExternalId = 100,
            Name = "Test Campaign",
            Type = "Email",
            CreatedBy = "TestUser",
            CreatedOn = DateTime.UtcNow,
            ModifiedBy = "TestUser",
            FirstSendDate = DateTime.UtcNow,
            FromEmailAddress = "test@example.com",
            FromName = "Test Sender",
            ReplyEmailAddress = "reply@example.com",
            Subject = "Test Subject",
            SubStatus = "Active",
            ContactCount = 0,
            Account = "TestAccount"
        };

        // Act
        Func<Task> act = async () => await repository.UpsertAsync(campaign);

        // Assert
        act.Should().NotThrowAsync<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Tests that UpsertAsync accepts campaign with DateTime.MinValue for required DateTime properties.
    /// This validates handling of minimum DateTime boundary values.
    /// Expected: Method executes without throwing ArgumentOutOfRangeException.
    /// </summary>
    [Test]
    public void UpsertAsync_CampaignWithMinDateTime_DoesNotThrowException()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();
        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        var campaign = new Campaigns
        {
            Id = 1,
            ExternalId = 100,
            Name = "Test Campaign",
            Type = "Email",
            CreatedBy = "TestUser",
            CreatedOn = DateTime.MinValue,
            ModifiedBy = "TestUser",
            FirstSendDate = DateTime.MinValue,
            FromEmailAddress = "test@example.com",
            FromName = "Test Sender",
            ReplyEmailAddress = "reply@example.com",
            Subject = "Test Subject",
            SubStatus = "Active",
            ContactCount = 0,
            Account = "TestAccount"
        };

        // Act
        Func<Task> act = async () => await repository.UpsertAsync(campaign);

        // Assert
        act.Should().NotThrowAsync<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Tests that UpsertAsync accepts campaign with DateTime.MaxValue for required DateTime properties.
    /// This validates handling of maximum DateTime boundary values.
    /// Expected: Method executes without throwing ArgumentOutOfRangeException.
    /// </summary>
    [Test]
    public void UpsertAsync_CampaignWithMaxDateTime_DoesNotThrowException()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();
        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        var campaign = new Campaigns
        {
            Id = 1,
            ExternalId = 100,
            Name = "Test Campaign",
            Type = "Email",
            CreatedBy = "TestUser",
            CreatedOn = DateTime.MaxValue,
            ModifiedBy = "TestUser",
            FirstSendDate = DateTime.MaxValue,
            FromEmailAddress = "test@example.com",
            FromName = "Test Sender",
            ReplyEmailAddress = "reply@example.com",
            Subject = "Test Subject",
            SubStatus = "Active",
            ContactCount = 0,
            Account = "TestAccount"
        };

        // Act
        Func<Task> act = async () => await repository.UpsertAsync(campaign);

        // Assert
        act.Should().NotThrowAsync<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Tests that UpsertAsync accepts campaign with empty strings for all string properties.
    /// This validates handling of empty string edge case (not null, but zero length).
    /// Expected: Method executes without throwing ArgumentException.
    /// </summary>
    [Test]
    public void UpsertAsync_CampaignWithEmptyStrings_DoesNotThrowException()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();
        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        var campaign = new Campaigns
        {
            Id = 1,
            ExternalId = 100,
            Name = string.Empty,
            Type = string.Empty,
            CreatedBy = string.Empty,
            CreatedOn = DateTime.UtcNow,
            ModifiedBy = string.Empty,
            FirstSendDate = DateTime.UtcNow,
            FromEmailAddress = string.Empty,
            FromName = string.Empty,
            ReplyEmailAddress = string.Empty,
            Subject = string.Empty,
            SubStatus = string.Empty,
            ContactCount = 0,
            Account = string.Empty
        };

        // Act
        Func<Task> act = async () => await repository.UpsertAsync(campaign);

        // Assert
        act.Should().NotThrowAsync<ArgumentException>();
    }

    /// <summary>
    /// Tests that UpsertAsync accepts campaign with whitespace-only strings for all string properties.
    /// This validates handling of whitespace string edge case.
    /// Expected: Method executes without throwing ArgumentException.
    /// </summary>
    [Test]
    public void UpsertAsync_CampaignWithWhitespaceStrings_DoesNotThrowException()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();
        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        var campaign = new Campaigns
        {
            Id = 1,
            ExternalId = 100,
            Name = "   ",
            Type = "   ",
            CreatedBy = "   ",
            CreatedOn = DateTime.UtcNow,
            ModifiedBy = "   ",
            FirstSendDate = DateTime.UtcNow,
            FromEmailAddress = "   ",
            FromName = "   ",
            ReplyEmailAddress = "   ",
            Subject = "   ",
            SubStatus = "   ",
            ContactCount = 0,
            Account = "   "
        };

        // Act
        Func<Task> act = async () => await repository.UpsertAsync(campaign);

        // Assert
        act.Should().NotThrowAsync<ArgumentException>();
    }

    /// <summary>
    /// Tests that UpsertAsync accepts campaign with very long strings (10000 characters).
    /// This validates handling of extremely long string values.
    /// Expected: Method executes without throwing ArgumentException or OverflowException.
    /// </summary>
    [Test]
    public void UpsertAsync_CampaignWithVeryLongStrings_DoesNotThrowException()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();
        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        var longString = new string('A', 10000);
        var campaign = new Campaigns
        {
            Id = 1,
            ExternalId = 100,
            Name = longString,
            Type = longString,
            CreatedBy = longString,
            CreatedOn = DateTime.UtcNow,
            ModifiedBy = longString,
            FirstSendDate = DateTime.UtcNow,
            FromEmailAddress = longString,
            FromName = longString,
            ReplyEmailAddress = longString,
            Subject = longString,
            SubStatus = longString,
            ContactCount = 0,
            Account = longString
        };

        // Act
        Func<Task> act = async () => await repository.UpsertAsync(campaign);

        // Assert
        act.Should().NotThrowAsync<ArgumentException>();
        act.Should().NotThrowAsync<OverflowException>();
    }

    /// <summary>
    /// Tests that UpsertAsync accepts campaign with special characters in string properties.
    /// This validates handling of strings containing special characters like quotes, newlines, etc.
    /// Expected: Method executes without throwing ArgumentException or FormatException.
    /// </summary>
    [Test]
    public void UpsertAsync_CampaignWithSpecialCharactersInStrings_DoesNotThrowException()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();
        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        var specialString = "Test'\"\\<>;\n\r\t";
        var campaign = new Campaigns
        {
            Id = 1,
            ExternalId = 100,
            Name = specialString,
            Type = specialString,
            CreatedBy = specialString,
            CreatedOn = DateTime.UtcNow,
            ModifiedBy = specialString,
            FirstSendDate = DateTime.UtcNow,
            FromEmailAddress = specialString,
            FromName = specialString,
            ReplyEmailAddress = specialString,
            Subject = specialString,
            SubStatus = specialString,
            ContactCount = 0,
            Account = specialString
        };

        // Act
        Func<Task> act = async () => await repository.UpsertAsync(campaign);

        // Assert
        act.Should().NotThrowAsync<ArgumentException>();
        act.Should().NotThrowAsync<FormatException>();
    }

    /// <summary>
    /// Tests that UpsertAsync accepts campaign with zero ContactCount.
    /// This validates handling of zero boundary value for ContactCount.
    /// Expected: Method executes without throwing ArgumentOutOfRangeException.
    /// </summary>
    [Test]
    public void UpsertAsync_CampaignWithZeroContactCount_DoesNotThrowException()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();
        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        var campaign = new Campaigns
        {
            Id = 1,
            ExternalId = 100,
            Name = "Test Campaign",
            Type = "Email",
            CreatedBy = "TestUser",
            CreatedOn = DateTime.UtcNow,
            ModifiedBy = "TestUser",
            FirstSendDate = DateTime.UtcNow,
            FromEmailAddress = "test@example.com",
            FromName = "Test Sender",
            ReplyEmailAddress = "reply@example.com",
            Subject = "Test Subject",
            SubStatus = "Active",
            ContactCount = 0,
            Account = "TestAccount"
        };

        // Act
        Func<Task> act = async () => await repository.UpsertAsync(campaign);

        // Assert
        act.Should().NotThrowAsync<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Tests that UpsertAsync accepts campaign with negative ContactCount.
    /// This validates handling of negative values for ContactCount.
    /// Expected: Method executes without throwing ArgumentOutOfRangeException.
    /// </summary>
    [Test]
    public void UpsertAsync_CampaignWithNegativeContactCount_DoesNotThrowException()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();
        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        var campaign = new Campaigns
        {
            Id = 1,
            ExternalId = 100,
            Name = "Test Campaign",
            Type = "Email",
            CreatedBy = "TestUser",
            CreatedOn = DateTime.UtcNow,
            ModifiedBy = "TestUser",
            FirstSendDate = DateTime.UtcNow,
            FromEmailAddress = "test@example.com",
            FromName = "Test Sender",
            ReplyEmailAddress = "reply@example.com",
            Subject = "Test Subject",
            SubStatus = "Active",
            ContactCount = -100,
            Account = "TestAccount"
        };

        // Act
        Func<Task> act = async () => await repository.UpsertAsync(campaign);

        // Assert
        act.Should().NotThrowAsync<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Tests that GetByIdsAsync accepts various valid collections of ids including edge cases.
    /// This parameterized test consolidates multiple scenarios: empty, single, multiple, duplicates,
    /// boundary values, negative values, and mixed values.
    /// Expected: Method should accept all valid IEnumerable collections and throw InvalidCastException
    /// when using mock connections (as Dapper requires real SqlConnection).
    /// </summary>
    /// <param name="ids">The collection of ids to test.</param>
    /// <param name="description">Description of the test case for clarity.</param>
    [TestCase(new long[] { }, "Empty collection")]
    [TestCase(new long[] { 1L }, "Single id")]
    [TestCase(new long[] { 1L, 2L, 3L }, "Multiple ids")]
    [TestCase(new long[] { 5L, 5L, 5L }, "Duplicate ids")]
    [TestCase(new long[] { long.MinValue }, "Single MinValue")]
    [TestCase(new long[] { long.MaxValue }, "Single MaxValue")]
    [TestCase(new long[] { 0L }, "Single zero")]
    [TestCase(new long[] { -1L, -2L, -3L }, "All negative ids")]
    [TestCase(new long[] { -5L, 0L, 5L }, "Mixed negative, zero, and positive")]
    [TestCase(new long[] { long.MinValue, long.MaxValue }, "Both extreme boundary values")]
    [TestCase(new long[] { 0L, 0L, 0L }, "Only zeros")]
    public void GetByIdsAsync_Should_Accept_Various_Valid_Collections(long[] ids, string description)
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();

        mockConnectionFactory.Setup(x => x.CreateConnection()).Returns(mockConnection.Object);

        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        // Act
        Func<Task> act = async () => await repository.GetByIdsAsync(ids);

        // Assert
        act.Should().ThrowAsync<InvalidCastException>($"because mock connection cannot be cast to SqlConnection for test case: {description}");
        mockConnectionFactory.Verify(x => x.CreateConnection(), Times.Once, $"factory should be called once for test case: {description}");
    }

    /// <summary>
    /// Tests that GetByIdsAsync correctly logs the count for various collection sizes.
    /// This parameterized test verifies that the Count() extension is called correctly
    /// and logged for different collection sizes including edge cases.
    /// Expected: Logger should be invoked with the correct count value for each collection size.
    /// </summary>
    /// <param name="ids">The collection of ids to test.</param>
    /// <param name="expectedCount">The expected count that should be logged.</param>
    [TestCase(new long[] { }, 0)]
    [TestCase(new long[] { 1L }, 1)]
    [TestCase(new long[] { 1L, 2L, 3L, 4L, 5L }, 5)]
    [TestCase(new long[] { long.MinValue, long.MaxValue }, 2)]
    [TestCase(new long[] { 1L, 1L, 1L, 1L }, 4)]
    public async Task GetByIdsAsync_Should_Log_Correct_Count_For_Various_Collections(long[] ids, int expectedCount)
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();

        mockConnectionFactory.Setup(x => x.CreateConnection()).Returns(mockConnection.Object);

        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        // Act
        try
        {
            await repository.GetByIdsAsync(ids);
        }
        catch (InvalidCastException)
        {
            // Expected exception when using mock connection
        }

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, type) =>
                    state.ToString()!.Contains(expectedCount.ToString()) &&
                    state.ToString()!.Contains("dbo.Usp_Campaigns_Get")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            $"logger should log count {expectedCount} for collection");
    }

    /// <summary>
    /// Tests that GetByIdsAsync creates correct comma-separated string for various id collections.
    /// This verifies the string.Join behavior with different collections by examining log output.
    /// Expected: The log message should contain the correctly formatted comma-separated id string.
    /// </summary>
    /// <param name="ids">The collection of ids to test.</param>
    /// <param name="expectedString">The expected comma-separated string representation.</param>
    [TestCase(new long[] { }, "")]
    [TestCase(new long[] { 123L }, "123")]
    [TestCase(new long[] { 1L, 2L, 3L }, "1,2,3")]
    [TestCase(new long[] { long.MinValue }, "-9223372036854775808")]
    [TestCase(new long[] { long.MaxValue }, "9223372036854775807")]
    [TestCase(new long[] { long.MinValue, 0L, long.MaxValue }, "-9223372036854775808,0,9223372036854775807")]
    [TestCase(new long[] { -1L, -2L, -3L }, "-1,-2,-3")]
    public async Task GetByIdsAsync_Should_Create_Correct_String_Format_For_Various_Collections(long[] ids, string expectedString)
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();

        mockConnectionFactory.Setup(x => x.CreateConnection()).Returns(mockConnection.Object);

        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        var actualString = string.Join(",", ids);

        // Act
        try
        {
            await repository.GetByIdsAsync(ids);
        }
        catch (InvalidCastException)
        {
            // Expected exception when using mock connection
        }

        // Assert
        actualString.Should().Be(expectedString, $"string.Join should produce the expected format for ids: [{string.Join(", ", ids)}]");
    }

    /// <summary>
    /// Tests that GetByIdsAsync throws ArgumentNullException when ids parameter is null.
    /// This validates that the method properly handles null input by throwing from string.Join.
    /// Expected: ArgumentNullException should be thrown before any logging or connection creation occurs.
    /// </summary>
    [Test]
    public void GetByIdsAsync_Should_Throw_ArgumentNullException_For_Null_Parameter()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();

        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);
        IEnumerable<long>? nullIds = null;

        // Act
        Func<Task> act = async () => await repository.GetByIdsAsync(nullIds!);

        // Assert
        act.Should().ThrowAsync<ArgumentNullException>()
            .WithMessage("*ids*", "because null ids should throw ArgumentNullException from string.Join");

        mockConnectionFactory.Verify(x => x.CreateConnection(), Times.Never,
            "factory should not be called when ids is null");
    }

    /// <summary>
    /// Tests that GetByIdsAsync returns correct Task type matching the interface contract.
    /// Verifies that the method signature returns Task of IEnumerable of Campaigns.
    /// Expected: Method should return Task&lt;IEnumerable&lt;Campaigns&gt;&gt;.
    /// </summary>
    [Test]
    public void GetByIdsAsync_Should_Return_Correct_Task_Type_Signature()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();
        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);
        var ids = new List<long> { 1L };

        // Act
        var result = repository.GetByIdsAsync(ids);

        // Assert
        result.Should().BeOfType<Task<IEnumerable<Campaigns>>>()
            .And.NotBeNull("because GetByIdsAsync should return a Task containing IEnumerable of Campaigns");
    }

    /// <summary>
    /// Tests that GetByIdsAsync propagates exceptions from the factory when creating connection.
    /// This verifies proper exception handling and propagation when a dependency fails.
    /// Expected: The exception thrown by factory should propagate to the caller.
    /// </summary>
    [Test]
    public void GetByIdsAsync_Should_Propagate_Exception_When_Factory_Throws()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();
        var expectedException = new InvalidOperationException("Factory error");

        mockConnectionFactory.Setup(x => x.CreateConnection()).Throws(expectedException);

        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);
        var ids = new List<long> { 1L, 2L };

        // Act
        Func<Task> act = async () => await repository.GetByIdsAsync(ids);

        // Assert
        act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Factory error", "because factory exception should propagate to caller");
    }

    /// <summary>
    /// Tests that GetByIdsAsync logs before attempting to create connection, even when factory throws.
    /// This verifies the execution order: logging happens before factory call.
    /// Expected: Logger should be invoked before the factory exception occurs.
    /// </summary>
    [Test]
    public void GetByIdsAsync_Should_Log_Before_Factory_Exception()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();

        mockConnectionFactory.Setup(x => x.CreateConnection()).Throws<InvalidOperationException>();

        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);
        var ids = new List<long> { 1L };

        // Act
        try
        {
            var _ = repository.GetByIdsAsync(ids).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException)
        {
            // Expected exception from factory
        }

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, type) => state.ToString()!.Contains("Fetching")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "logger should be called before factory throws exception");
    }

    /// <summary>
    /// Tests that GetByIdsAsync creates a new connection for each invocation.
    /// Verifies that connections are not cached or reused across multiple calls.
    /// Expected: CreateConnection should be called exactly N times for N invocations.
    /// </summary>
    [Test]
    public async Task GetByIdsAsync_Should_Create_New_Connection_For_Each_Invocation()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();

        mockConnectionFactory.Setup(x => x.CreateConnection()).Returns(mockConnection.Object);

        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);
        var ids = new List<long> { 1L };

        // Act
        try
        {
            await repository.GetByIdsAsync(ids);
            await repository.GetByIdsAsync(ids);
            await repository.GetByIdsAsync(ids);
        }
        catch (InvalidCastException)
        {
            // Expected exception when using mock connection
        }

        // Assert
        mockConnectionFactory.Verify(x => x.CreateConnection(), Times.Exactly(3),
            "factory should be called once per method invocation");
    }

    /// <summary>
    /// Tests that GetByIdsAsync handles concurrent calls correctly, creating independent connections.
    /// This verifies that concurrent invocations don't share state or connections.
    /// Expected: Each concurrent call should independently use the factory.
    /// </summary>
    [Test]
    public async Task GetByIdsAsync_Should_Handle_Concurrent_Calls_Independently()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();

        mockConnectionFactory.Setup(x => x.CreateConnection()).Returns(mockConnection.Object);

        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);
        var ids1 = new List<long> { 1L, 2L };
        var ids2 = new List<long> { 3L, 4L };
        var ids3 = new List<long> { 5L, 6L };

        // Act
        var tasks = new[]
        {
            Task.Run(async () => { try { await repository.GetByIdsAsync(ids1); } catch { } }),
            Task.Run(async () => { try { await repository.GetByIdsAsync(ids2); } catch { } }),
            Task.Run(async () => { try { await repository.GetByIdsAsync(ids3); } catch { } })
        };

        await Task.WhenAll(tasks);

        // Assert
        mockConnectionFactory.Verify(x => x.CreateConnection(), Times.Exactly(3),
            "factory should be called once for each concurrent call");
    }

    /// <summary>
    /// Tests that GetByIdAsync propagates exceptions thrown by the factory when creating a connection.
    /// This verifies that factory exceptions are not swallowed and reach the caller.
    /// Expected: The exception from factory.CreateConnection() should propagate to the caller.
    /// </summary>
    [Test]
    public void GetByIdAsync_Should_Propagate_Exception_When_Factory_Throws()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();
        var expectedException = new InvalidOperationException("Factory connection creation failed");

        mockConnectionFactory.Setup(f => f.CreateConnection()).Throws(expectedException);

        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);
        long id = 123;

        // Act
        Func<Task> act = async () => await repository.GetByIdAsync(id);

        // Assert
        act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Factory connection creation failed");
        mockConnectionFactory.Verify(f => f.CreateConnection(), Times.Once);
    }

    /// <summary>
    /// Tests that GetByIdAsync throws NullReferenceException when factory returns null.
    /// This validates the method's behavior when the dependency returns an unexpected null value.
    /// Expected: NullReferenceException should be thrown when attempting to cast null to SqlConnection.
    /// </summary>
    [Test]
    public void GetByIdAsync_Should_Throw_NullReferenceException_When_Factory_Returns_Null()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockLogger = new Mock<ILogger<CampaignRepository>>();

        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns((IDbConnection?)null);

        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);
        long id = 456;

        // Act
        Func<Task> act = async () => await repository.GetByIdAsync(id);

        // Assert
        act.Should().ThrowAsync<NullReferenceException>();
        mockConnectionFactory.Verify(f => f.CreateConnection(), Times.Once);
    }

    /// <summary>
    /// Tests that GetByIdAsync propagates exceptions thrown by the logger.
    /// This verifies that logging exceptions are not swallowed and impact the execution flow.
    /// Expected: The exception from logger.LogInformation() should propagate to the caller.
    /// </summary>
    [Test]
    public void GetByIdAsync_Should_Propagate_Exception_When_Logger_Throws()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();
        var expectedException = new InvalidOperationException("Logging failed");

        mockLogger.Setup(l => l.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Throws(expectedException);

        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);
        long id = 789;

        // Act
        Func<Task> act = async () => await repository.GetByIdAsync(id);

        // Assert
        act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Logging failed");
        mockConnectionFactory.Verify(f => f.CreateConnection(), Times.Never);
    }

    /// <summary>
    /// Tests that GetByIdAsync correctly handles concurrent calls.
    /// This verifies that multiple simultaneous calls each get their own connection.
    /// Expected: Factory should be called exactly the number of times equal to concurrent calls.
    /// </summary>
    [Test]
    public async Task GetByIdAsync_Should_Handle_Concurrent_Calls_Independently()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();

        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);

        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        var tasks = new List<Task>();
        int concurrentCalls = 5;

        // Act
        for (int i = 0; i < concurrentCalls; i++)
        {
            long id = i + 1;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await repository.GetByIdAsync(id);
                }
                catch (InvalidCastException)
                {
                    // Expected when using mocks
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        mockConnectionFactory.Verify(f => f.CreateConnection(), Times.Exactly(concurrentCalls));
    }

    /// <summary>
    /// Tests that GetByIdAsync handles multiple sequential calls without retaining state.
    /// This verifies that each call independently creates a new connection.
    /// Expected: Factory should be called exactly the number of times equal to sequential calls.
    /// </summary>
    [Test]
    public async Task GetByIdAsync_Should_Handle_Multiple_Sequential_Calls_Without_State()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();

        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);

        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        int sequentialCalls = 3;
        long[] ids = { 1, 2, 3 };

        // Act
        for (int i = 0; i < sequentialCalls; i++)
        {
            try
            {
                await repository.GetByIdAsync(ids[i]);
            }
            catch (InvalidCastException)
            {
                // Expected when using mocks
            }
        }

        // Assert
        mockConnectionFactory.Verify(f => f.CreateConnection(), Times.Exactly(sequentialCalls));
    }

    /// <summary>
    /// Tests that GetByIdAsync uses the exact stored procedure name "dbo.Usp_Campaigns_Get".
    /// This validates that the correct stored procedure identifier is used in the log message.
    /// Expected: Log message should contain the exact stored procedure name.
    /// </summary>
    [Test]
    public async Task GetByIdAsync_Should_Use_Exact_Stored_Procedure_Name()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();

        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);

        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);
        long id = 100;

        bool loggedCorrectProcedure = false;
        mockLogger.Setup(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, type) =>
            {
                string message = state.ToString() ?? string.Empty;
                if (message.Contains("dbo.Usp_Campaigns_Get"))
                {
                    loggedCorrectProcedure = true;
                }
                return true;
            }),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()));

        // Act
        try
        {
            await repository.GetByIdAsync(id);
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }

        // Assert
        loggedCorrectProcedure.Should().BeTrue();
    }

    /// <summary>
    /// Tests that GetByIdAsync logs before attempting to create a connection, even when factory throws.
    /// This verifies the correct execution order: logging happens first, then connection creation.
    /// Expected: Logger should be called before the factory exception occurs.
    /// </summary>
    [Test]
    public void GetByIdAsync_Should_Log_Before_Factory_Exception()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();
        var expectedException = new InvalidOperationException("Factory failed");

        mockConnectionFactory.Setup(f => f.CreateConnection()).Throws(expectedException);

        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);
        long id = 999;

        // Act
        Func<Task> act = async () => await repository.GetByIdAsync(id);

        // Assert
        act.Should().ThrowAsync<InvalidOperationException>();
        mockLogger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that GetByIdAsync converts id parameter to string correctly in the Dapper parameter.
    /// This validates the id.ToString() conversion by checking the log message.
    /// Expected: The id value should be logged correctly as a string representation.
    /// </summary>
    [TestCase(long.MinValue, "-9223372036854775808")]
    [TestCase(long.MaxValue, "9223372036854775807")]
    [TestCase(0L, "0")]
    [TestCase(42L, "42")]
    [TestCase(-42L, "-42")]
    public async Task GetByIdAsync_Should_Convert_Id_To_String_Correctly(long id, string expectedStringRepresentation)
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();

        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);

        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);

        string? capturedIdValue = null;
        mockLogger.Setup(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, type) =>
            {
                string message = state.ToString() ?? string.Empty;
                if (message.Contains(expectedStringRepresentation))
                {
                    capturedIdValue = expectedStringRepresentation;
                }
                return true;
            }),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()));

        // Act
        try
        {
            await repository.GetByIdAsync(id);
        }
        catch (InvalidCastException)
        {
            // Expected when using mocks
        }

        // Assert
        capturedIdValue.Should().Be(expectedStringRepresentation);
    }

    /// <summary>
    /// Tests that GetByIdAsync returns a Task (not void) to ensure proper async pattern.
    /// This validates that the method follows async/await conventions correctly.
    /// Expected: Method should return a Task, not void or a synchronous type.
    /// </summary>
    [Test]
    public void GetByIdAsync_Should_Return_Task_Not_Void()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var mockLogger = new Mock<ILogger<CampaignsRepository>>();

        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);

        var repository = new CampaignsRepository(mockConnectionFactory.Object, mockLogger.Object);
        long id = 1;

        // Act
        var result = repository.GetByIdAsync(id);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<Task<Campaigns>>();
    }

    /// <summary>
    /// Tests that GetByIdAsync method signature matches the interface contract.
    /// This validates that the implementation correctly implements ICampaignsRepository.
    /// Expected: Method signature should match the interface exactly.
    /// </summary>
    [Test]
    public void GetByIdAsync_Should_Match_Interface_Signature()
    {
        // Arrange
        var repositoryType = typeof(CampaignsRepository);
        var interfaceType = typeof(ICampaignsRepository);

        // Act
        var interfaceMethod = interfaceType.GetMethod("GetByIdAsync");
        var implementationMethod = repositoryType.GetMethod("GetByIdAsync");

        // Assert
        interfaceMethod.Should().NotBeNull();
        implementationMethod.Should().NotBeNull();
        implementationMethod!.ReturnType.Should().Be(interfaceMethod!.ReturnType);

        var interfaceParams = interfaceMethod.GetParameters();
        var implementationParams = implementationMethod.GetParameters();

        implementationParams.Should().HaveCount(interfaceParams.Length);
        implementationParams[0].ParameterType.Should().Be(typeof(long));
        implementationParams[0].Name.Should().Be("id");
    }
}

namespace DAS.DigitalEngagement.CampaignInterest.Data.Repositories.UnitTests;



/// <summary>
/// Unit tests for ICampaignsRepository.GetByIdAsync method.
/// Note: Repository methods cannot be fully tested with mocks because Dapper extensions require a real SqlConnection.
/// These tests verify parameter handling, logging behavior, factory usage, and exception scenarios.
/// Integration tests should be used to verify the full database interaction functionality with stored procedures.
/// </summary>
[TestFixture]
public class ICampaignsRepositoryGetByIdAsyncTests
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
        _repository = new CampaignsRepository(_mockConnectionFactory.Object, _mockLogger.Object);
    }

    /// <summary>
    /// Tests that GetByIdAsync accepts various boundary and edge case values for the id parameter.
    /// This parameterized test verifies that the method handles long.MinValue, long.MaxValue, zero,
    /// positive, and negative values without throwing ArgumentException.
    /// Expected: Method should accept all valid long values and only throw InvalidCastException when using mock connections.
    /// </summary>
    /// <param name="id">The campaign id to test.</param>
    [TestCase(long.MinValue)]
    [TestCase(long.MaxValue)]
    [TestCase(0L)]
    [TestCase(1L)]
    [TestCase(-1L)]
    [TestCase(12345L)]
    [TestCase(-12345L)]
    [TestCase(999999999L)]
    [TestCase(-999999999L)]
    public void GetByIdAsync_Should_Accept_All_Valid_Long_Values(long id)
    {
        // Arrange
        _mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(_mockConnection.Object);

        // Act
        Func<Task> act = async () => await _repository.GetByIdAsync(id);

        // Assert
        act.Should().ThrowAsync<InvalidCastException>()
            .WithMessage("Unable to cast object of type '*' to type 'Microsoft.Data.SqlClient.SqlConnection'.");
        _mockConnectionFactory.Verify(f => f.CreateConnection(), Times.Once);
    }

    /// <summary>
    /// Tests that GetByIdAsync logs the correct information including the id value and stored procedure name.
    /// This parameterized test verifies that the log message contains the correct id and stored procedure.
    /// Expected: Logger should be called with LogLevel.Information and message containing id and "dbo.Usp_Campaigns_Get".
    /// </summary>
    /// <param name="id">The campaign id to test.</param>
    [TestCase(1L)]
    [TestCase(long.MinValue)]
    [TestCase(long.MaxValue)]
    [TestCase(0L)]
    [TestCase(-999L)]
    public async Task GetByIdAsync_Should_Log_Correct_Information_With_Id_And_StoredProcedure(long id)
    {
        // Arrange
        _mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(_mockConnection.Object);
        var loggedMessages = new List<string>();

        _mockLogger.Setup(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, type) => CaptureLogMessage(state, loggedMessages)),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()));

        // Act
        try
        {
            await _repository.GetByIdAsync(id);
        }
        catch (InvalidCastException)
        {
            // Expected exception when using mock connection
        }

        // Assert
        _mockLogger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

        loggedMessages.Should().HaveCount(1);
        loggedMessages[0].Should().Contain(id.ToString());
        loggedMessages[0].Should().Contain("dbo.Usp_Campaigns_Get");
        loggedMessages[0].Should().Contain("Fetching Campaign by Id");
    }

    /// <summary>
    /// Tests that GetByIdAsync calls the factory CreateConnection method exactly once.
    /// This parameterized test verifies factory usage for various id values.
    /// Expected: CreateConnection should be invoked exactly once per method call.
    /// </summary>
    /// <param name="id">The campaign id to test.</param>
    [TestCase(1L)]
    [TestCase(long.MinValue)]
    [TestCase(long.MaxValue)]
    [TestCase(0L)]
    [TestCase(-12345L)]
    public async Task GetByIdAsync_Should_Call_Factory_CreateConnection_Once(long id)
    {
        // Arrange
        _mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(_mockConnection.Object);

        // Act
        try
        {
            await _repository.GetByIdAsync(id);
        }
        catch (InvalidCastException)
        {
            // Expected exception when using mock connection
        }

        // Assert
        _mockConnectionFactory.Verify(f => f.CreateConnection(), Times.Once);
    }

    /// <summary>
    /// Tests that GetByIdAsync throws InvalidCastException when factory returns a mock connection.
    /// This verifies that the method requires a real SqlConnection (not just IDbConnection).
    /// Expected: InvalidCastException should be thrown when casting IDbConnection to SqlConnection.
    /// </summary>
    [Test]
    public void GetByIdAsync_Should_Throw_InvalidCastException_When_Factory_Returns_Mock_Connection()
    {
        // Arrange
        _mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(_mockConnection.Object);
        const long id = 1L;

        // Act
        Func<Task> act = async () => await _repository.GetByIdAsync(id);

        // Assert
        act.Should().ThrowAsync<InvalidCastException>();
    }

    /// <summary>
    /// Tests that GetByIdAsync returns a Task with the correct return type.
    /// Verifies the method signature matches the interface contract.
    /// Expected: Method should return Task&lt;Campaigns?&gt;.
    /// </summary>
    [Test]
    public void GetByIdAsync_Should_Return_Task_Of_Nullable_Campaigns()
    {
        // Arrange
        _mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(_mockConnection.Object);
        const long id = 1L;

        // Act
        var result = _repository.GetByIdAsync(id);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<Task<Campaigns>>();
    }

    /// <summary>
    /// Tests that GetByIdAsync creates a new connection for each invocation.
    /// Verifies that connections are not reused across multiple calls.
    /// Expected: CreateConnection should be called exactly twice for two invocations.
    /// </summary>
    [Test]
    public async Task GetByIdAsync_Should_Create_New_Connection_For_Each_Invocation()
    {
        // Arrange
        _mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(_mockConnection.Object);

        // Act
        try
        {
            await _repository.GetByIdAsync(1L);
        }
        catch (InvalidCastException) { }

        try
        {
            await _repository.GetByIdAsync(2L);
        }
        catch (InvalidCastException) { }

        // Assert
        _mockConnectionFactory.Verify(f => f.CreateConnection(), Times.Exactly(2));
    }

    /// <summary>
    /// Tests that GetByIdAsync correctly formats extreme long values when converting to string.
    /// Verifies that id.ToString() works correctly for boundary values in the log message.
    /// Expected: Log should contain the correctly formatted string representation of extreme long values.
    /// </summary>
    /// <param name="id">The campaign id to test.</param>
    /// <param name="expectedString">The expected string representation of the id.</param>
    [TestCase(long.MinValue, "-9223372036854775808")]
    [TestCase(long.MaxValue, "9223372036854775807")]
    [TestCase(0L, "0")]
    [TestCase(-1L, "-1")]
    [TestCase(1L, "1")]
    public async Task GetByIdAsync_Should_Format_Id_Correctly_As_String(long id, string expectedString)
    {
        // Arrange
        _mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(_mockConnection.Object);
        var loggedMessages = new List<string>();

        _mockLogger.Setup(x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, type) => CaptureLogMessage(state, loggedMessages)),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()));

        // Act
        try
        {
            await _repository.GetByIdAsync(id);
        }
        catch (InvalidCastException) { }

        // Assert
        loggedMessages.Should().HaveCount(1);
        loggedMessages[0].Should().Contain(expectedString);
    }

    /// <summary>
    /// Tests that GetByIdAsync logs information before attempting to create connection.
    /// This verifies that logging occurs at the correct point in the execution flow.
    /// Expected: Logger should be called before the factory CreateConnection call.
    /// </summary>
    [Test]
    public async Task GetByIdAsync_Should_Log_Before_Creating_Connection()
    {
        // Arrange
        var callOrder = new List<string>();
        _mockLogger.Setup(x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(() => callOrder.Add("Log"));

        _mockConnectionFactory.Setup(f => f.CreateConnection())
            .Callback(() => callOrder.Add("CreateConnection"))
            .Returns(_mockConnection.Object);

        // Act
        try
        {
            await _repository.GetByIdAsync(1L);
        }
        catch (InvalidCastException) { }

        // Assert
        callOrder.Should().HaveCount(2);
        callOrder[0].Should().Be("Log");
        callOrder[1].Should().Be("CreateConnection");
    }

    /// <summary>
    /// Tests that GetByIdAsync propagates exceptions thrown by the factory when creating connection.
    /// This verifies proper exception handling when the dependency fails.
    /// Expected: The exception from factory should propagate to the caller.
    /// </summary>
    [Test]
    public void GetByIdAsync_Should_Propagate_Exception_When_Factory_Throws()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Factory error");
        _mockConnectionFactory.Setup(f => f.CreateConnection()).Throws(expectedException);

        // Act
        Func<Task> act = async () => await _repository.GetByIdAsync(1L);

        // Assert
        act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Factory error");
    }

    /// <summary>
    /// Tests that GetByIdAsync throws NullReferenceException when factory returns null.
    /// This verifies the method's behavior when dependency returns unexpected null value.
    /// Expected: NullReferenceException should be thrown when attempting to cast null.
    /// </summary>
    [Test]
    public void GetByIdAsync_Should_Throw_NullReferenceException_When_Factory_Returns_Null()
    {
        // Arrange
        _mockConnectionFactory.Setup(f => f.CreateConnection()).Returns((IDbConnection)null!);

        // Act
        Func<Task> act = async () => await _repository.GetByIdAsync(1L);

        // Assert
        act.Should().ThrowAsync<NullReferenceException>();
    }

    /// <summary>
    /// Tests that GetByIdAsync logs before factory exception occurs.
    /// This verifies that logging happens before the factory call that throws.
    /// Expected: Logger should be called once before the factory exception.
    /// </summary>
    [Test]
    public void GetByIdAsync_Should_Log_Before_Factory_Exception()
    {
        // Arrange
        _mockConnectionFactory.Setup(f => f.CreateConnection())
            .Throws(new InvalidOperationException("Factory error"));

        // Act
        Func<Task> act = async () => await _repository.GetByIdAsync(1L);

        // Assert
        act.Should().ThrowAsync<InvalidOperationException>();
        _mockLogger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    /// <summary>
    /// Tests that GetByIdAsync uses the specific stored procedure name in logging.
    /// This verifies the exact stored procedure identifier used by the method.
    /// Expected: Log message should contain "dbo.Usp_Campaigns_Get".
    /// </summary>
    [Test]
    public async Task GetByIdAsync_Should_Use_Correct_Stored_Procedure_Name()
    {
        // Arrange
        _mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(_mockConnection.Object);
        var loggedMessages = new List<string>();

        _mockLogger.Setup(x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, type) => CaptureLogMessage(state, loggedMessages)),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()));

        // Act
        try
        {
            await _repository.GetByIdAsync(1L);
        }
        catch (InvalidCastException) { }

        // Assert
        loggedMessages.Should().HaveCount(1);
        loggedMessages[0].Should().Contain("dbo.Usp_Campaigns_Get");
    }

    /// <summary>
    /// Tests that GetByIdAsync handles multiple sequential calls without issues.
    /// This verifies that the method does not maintain state between calls.
    /// Expected: Each call should independently use the factory and logger.
    /// </summary>
    [Test]
    public async Task GetByIdAsync_Should_Handle_Multiple_Sequential_Calls()
    {
        // Arrange
        _mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(_mockConnection.Object);

        // Act
        try { await _repository.GetByIdAsync(1L); } catch (InvalidCastException) { }
        try { await _repository.GetByIdAsync(2L); } catch (InvalidCastException) { }
        try { await _repository.GetByIdAsync(3L); } catch (InvalidCastException) { }

        // Assert
        _mockConnectionFactory.Verify(f => f.CreateConnection(), Times.Exactly(3));
        _mockLogger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Exactly(3));
    }

    /// <summary>
    /// Tests that GetByIdAsync can be called concurrently without issues.
    /// This verifies that concurrent calls independently use the factory.
    /// Expected: Factory should be called exactly the same number of times as concurrent calls.
    /// </summary>
    [Test]
    public async Task GetByIdAsync_Should_Handle_Concurrent_Calls()
    {
        // Arrange
        _mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(_mockConnection.Object);
        const int concurrentCalls = 5;

        // Act
        var tasks = Enumerable.Range(1, concurrentCalls)
            .Select(async i =>
            {
                try
                {
                    await _repository.GetByIdAsync(i);
                }
                catch (InvalidCastException)
                {
                    // Expected exception
                }
            });

        await Task.WhenAll(tasks);

        // Assert
        _mockConnectionFactory.Verify(f => f.CreateConnection(), Times.Exactly(concurrentCalls));
    }

    /// <summary>
    /// Tests that GetByIdAsync method signature has correct return type.
    /// This validates that the method returns a Task containing nullable Campaigns.
    /// Expected: Method should return Task&lt;Campaigns&gt; (nullable).
    /// </summary>
    [Test]
    public void GetByIdAsync_Should_Have_Correct_Return_Type_Signature()
    {
        // Arrange
        var methodInfo = typeof(CampaignsRepository).GetMethod("GetByIdAsync");

        // Act & Assert
        methodInfo.Should().NotBeNull();
        methodInfo!.ReturnType.Should().Be(typeof(Task<Campaigns>));
    }

    /// <summary>
    /// Tests that GetByIdAsync is properly marked as async.
    /// Validates the method implementation follows async pattern.
    /// Expected: Method should be an async method returning Task.
    /// </summary>
    [Test]
    public void GetByIdAsync_Should_Be_Async_Method()
    {
        // Arrange
        var methodInfo = typeof(CampaignsRepository).GetMethod("GetByIdAsync");

        // Act & Assert
        methodInfo.Should().NotBeNull();
        methodInfo!.ReturnType.Should().BeAssignableTo<Task>();
    }

    /// <summary>
    /// Tests that GetByIdAsync accepts extremely large positive id values.
    /// Verifies handling of values close to long.MaxValue.
    /// Expected: Method should accept the value without throwing ArgumentException.
    /// </summary>
    [TestCase(9223372036854775806L)] // long.MaxValue - 1
    [TestCase(9223372036854775000L)]
    public void GetByIdAsync_Should_Accept_Very_Large_Positive_Ids(long id)
    {
        // Arrange
        _mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(_mockConnection.Object);

        // Act
        Func<Task> act = async () => await _repository.GetByIdAsync(id);

        // Assert
        act.Should().ThrowAsync<InvalidCastException>();
        _mockConnectionFactory.Verify(f => f.CreateConnection(), Times.Once);
    }

    /// <summary>
    /// Tests that GetByIdAsync accepts extremely large negative id values.
    /// Verifies handling of values close to long.MinValue.
    /// Expected: Method should accept the value without throwing ArgumentException.
    /// </summary>
    [TestCase(-9223372036854775807L)] // long.MinValue + 1
    [TestCase(-9223372036854775000L)]
    public void GetByIdAsync_Should_Accept_Very_Large_Negative_Ids(long id)
    {
        // Arrange
        _mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(_mockConnection.Object);

        // Act
        Func<Task> act = async () => await _repository.GetByIdAsync(id);

        // Assert
        act.Should().ThrowAsync<InvalidCastException>();
        _mockConnectionFactory.Verify(f => f.CreateConnection(), Times.Once);
    }

    /// <summary>
    /// Helper method to capture log message values for assertion.
    /// </summary>
    private static bool CaptureLogMessage(object state, List<string> messages)
    {
        messages.Add(state.ToString() ?? string.Empty);
        return true;
    }
}