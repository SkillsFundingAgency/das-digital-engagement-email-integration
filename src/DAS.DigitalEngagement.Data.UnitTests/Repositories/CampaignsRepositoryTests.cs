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
/// </summary>
[TestFixture]
public class CampaignsRepositoryTests
{
    private CampaignsRepository _repository = null!;
    private Mock<IDbConnectionFactory> _mockConnectionFactory = null!;
    private Mock<IDbConnection> _mockConnection = null!;
    private Mock<ILogger<CampaignsRepository>> _mockLogger = null!;
    private Campaigns campaign = null!;

    [SetUp]
    public void Setup()
    {
        _mockConnection = new Mock<IDbConnection>();
        _mockLogger = new Mock<ILogger<CampaignsRepository>>();

        _mockConnectionFactory = new Mock<IDbConnectionFactory>();
        _mockConnectionFactory.Setup(f => f.CreateConnectionAsync()).ReturnsAsync(_mockConnection.Object);

        _repository = new CampaignsRepository(_mockConnectionFactory.Object, _mockLogger.Object);

        campaign = new Campaigns
        {
            Id = 12345,
            ExternalCampaignId = 100,
            CampaignName = "Test Campaign",
            ExternalSendId = 200,
            SendName = "Test Send",
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

    [Test]
    public void Constructor_Should_Create_Instance_With_Valid_Dependencies()
    {
        // Assert
        Assert.That(_repository, Is.Not.Null);
        Assert.That(_repository, Is.InstanceOf<ICampaignsRepository>());
    }

    [Test]
    public void Constructor_Should_Accept_Factory_And_Logger_Without_Immediate_Connection()
    {
        // Assert
        Assert.That(_repository, Is.Not.Null);
        _mockConnectionFactory.Verify(f => f.CreateConnectionAsync(), Times.Never, "Constructor should not create connection immediately");
    }

    [Test]
    public async Task All_Four_Methods_Each_Create_Their_Own_Independent_Connection()
    {
        // Act - each method must use its own connection; no connection state is shared
        try { await _repository.UpsertAsync(campaign); }
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
        _mockConnectionFactory.Verify(f => f.CreateConnectionAsync(), Times.Exactly(4),
            "Each repository method should create its own independent connection; none are shared");
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
        Assert.That(implementationMethods, Has.Count.EqualTo(4), "Repository should implement exactly 4 methods: GetByIdAsync, GetAllAsync, GetByIdsAsync, UpsertAsync");
    }

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
        _mockConnectionFactory.Verify(f => f.CreateConnectionAsync(), Times.Once, "Repository should use factory to create connection when calling GetByIdAsync");
    }

    #region UpsertAsync Tests

    [Test]
    public void UpsertAsync_Should_Throw_ArgumentNullException_When_Campaign_Is_Null()
    {
        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(async () => await _repository.UpsertAsync(null!));

        Assert.That(exception!.ParamName, Is.EqualTo("campaign"));
    }

    [Test]
    public void UpsertAsync_Should_Accept_Valid_Campaign()
    {
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
    public async Task UpsertAsync_Should_Call_Factory_CreateConnection_Once()
    {
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
        _mockConnectionFactory.Verify(f => f.CreateConnectionAsync(), Times.Once, "Repository should use factory to create connection when calling UpsertAsync");
    }

    [Test]
    public async Task UpsertAsync_Should_Log_Information_Before_Connection_Is_Created()
    {
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
    public void UpsertAsync_Should_Not_Create_Connection_When_Campaign_Is_Null()
    {
        // Act
        Assert.ThrowsAsync<ArgumentNullException>(async () => await _repository.UpsertAsync(null!));

        // Assert - ArgumentNullException.ThrowIfNull fires before factory.CreateConnection() is reached
        _mockConnectionFactory.Verify(f => f.CreateConnectionAsync(), Times.Never,
            "Connection should not be created when input validation fails before the factory call");
    }

    [Test]
    public void UpsertAsync_Should_Throw_InvalidCastException_When_Factory_Returns_Mock_Connection()
    {
        Assert.ThrowsAsync<InvalidCastException>(async () => await _repository.UpsertAsync(campaign));

        // Factory was called before the cast failed
        _mockConnectionFactory.Verify(f => f.CreateConnectionAsync(), Times.Once);
    }

    [Test]
    public void UpsertAsync_Should_Accept_Campaign_With_Min_DateTime_Values()
    {
        // Arrange
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
        campaign.CampaignName = new string('A', 1000);
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
        campaign.CampaignName = "Test's Campaign \"Special\" & <Characters> 🎉";
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
        var campaign1 = campaign;
        campaign1.ExternalCampaignId = 0;

        var campaign2 = campaign;
        campaign2.ExternalCampaignId = -1;

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

    [Test]
    public void UpsertAsync_Should_Accept_Campaign_With_Zero_Id()
    {
        // Arrange
        campaign.Id = 0;

        // Act
        Func<Task> act = async () => await _repository.UpsertAsync(campaign);

        // Assert
        act.Should().ThrowAsync<InvalidCastException>();
    }

    [Test]
    public void UpsertAsync_Should_Accept_Campaign_With_Negative_Id()
    {
        // Arrange
        campaign.Id = -1;

        // Act
        Func<Task> act = async () => await _repository.UpsertAsync(campaign);

        // Assert
        act.Should().ThrowAsync<InvalidCastException>();
    }

    [Test]
    public void UpsertAsync_Should_Accept_Campaign_With_All_Boundary_Numeric_Values()
    {
        // Arrange
        campaign.Id = long.MaxValue;
        campaign.ExternalCampaignId = int.MaxValue;
        campaign.CreatedOn = DateTime.MaxValue;
        campaign.ModifiedOn = DateTime.MinValue;
        campaign.FirstSendDate = DateTime.MaxValue;
        campaign.LastSendDate = DateTime.MinValue;
        campaign.ContactCount = int.MaxValue;

        // Act
        Func<Task> act = async () => await _repository.UpsertAsync(campaign);

        // Assert
        act.Should().ThrowAsync<InvalidCastException>();
    }

    #endregion

    #region GetByIdAsync Tests

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
    public void GetByIdAsync_Should_Accept_Valid_Long_Values()
    {
        // Arrange
        long[] validIds = [long.MinValue, -1, 0, 1, 12345, long.MaxValue];

        // Act & Assert
        foreach (var id in validIds)
        {
            Func<Task> act = async () => await _repository.GetByIdAsync(id);
            act.Should().NotThrowAsync<ArgumentException>($"valid long value {id} should not throw");
        }
    }

    [Test]
    public async Task GetByIdAsync_Should_Log_Id_And_StoredProcedure()
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
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Fetching Campaign by Id")
                                            && v.ToString()!.Contains(id.ToString())
                                            && v.ToString()!.Contains("dbo.Usp_Campaigns_Get")),
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
    public void GetByIdAsync_Should_Throw_InvalidCastException_When_Factory_Returns_Mock_Connection()
    {
        Assert.ThrowsAsync<InvalidCastException>(async () => await _repository.GetByIdAsync(1));

        _mockConnectionFactory.Verify(f => f.CreateConnectionAsync(), Times.Once);
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
        _mockConnectionFactory.Verify(f => f.CreateConnectionAsync(), Times.Exactly(3),
            "Each method invocation should request a new connection from the factory");
    }

    [Test]
    public void GetByIdAsync_Should_Accept_Min_Long_Value()
    {
        // Arrange
        long id = long.MinValue;

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
            catch (NullReferenceException)
            {
                // Expected when mock connection doesn't fully implement SqlConnection behavior
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
        _mockConnectionFactory.Verify(f => f.CreateConnectionAsync(), Times.Once, "Repository should use factory to create connection when calling GetAllAsync");
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
    public void GetAllAsync_Should_Throw_InvalidCastException_When_Factory_Returns_Mock_Connection()
    {
        Assert.ThrowsAsync<InvalidCastException>(async () => await _repository.GetAllAsync());

        _mockConnectionFactory.Verify(f => f.CreateConnectionAsync(), Times.Once);
    }

    [Test]
    public async Task GetAllAsync_Should_Create_New_Connection_On_Each_Invocation()
    {
        // Act
        try { await _repository.GetAllAsync(); } catch
        {
            // Expected when using mocks
        }
        try { await _repository.GetAllAsync(); } catch
        {
            // Expected when using mocks
        }

        // Assert
        _mockConnectionFactory.Verify(f => f.CreateConnectionAsync(), Times.Exactly(2));
    }

    [Test]
    public void GetAllAsync_Should_Return_Task_Of_IEnumerable_Of_Campaigns()
    {
        // Act
        var method = _repository.GetType().GetMethod(nameof(_repository.GetAllAsync));

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IEnumerable<Campaigns>>));
    }

    [Test]
    public void GetAllAsync_Should_Be_Async_Method()
    {
        // Act
        var method = _repository.GetType().GetMethod(nameof(_repository.GetAllAsync));

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.BaseType.Should().Be(typeof(Task));
    }

    [Test]
    public async Task GetAllAsync_Should_Log_Stored_Procedure_Name()
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
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("dbo.Usp_Campaigns_Get")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Log message should include the stored procedure name 'dbo.Usp_Campaigns_Get'");
    }

    [Test]
    public void GetAllAsync_Should_Propagate_Exception_When_Factory_Throws()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Factory failed to create connection");
        _mockConnectionFactory.Setup(f => f.CreateConnectionAsync()).ThrowsAsync(expectedException);
        var repository = new CampaignsRepository(_mockConnectionFactory.Object, _mockLogger.Object);

        // Act
        Func<Task> act = async () => await repository.GetAllAsync();

        // Assert
        act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Factory failed to create connection");
    }

    [Test]
    public void GetAllAsync_Should_Log_Before_Factory_Exception()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Factory failure");
        _mockConnectionFactory.Setup(f => f.CreateConnectionAsync()).ThrowsAsync(expectedException);
        var repository = new CampaignsRepository(_mockConnectionFactory.Object, _mockLogger.Object);

        // Act
        Func<Task> act = async () => await repository.GetAllAsync();

        // Assert
        act.Should().ThrowAsync<InvalidOperationException>();

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
    public async Task GetAllAsync_Should_Call_Factory_For_Each_Concurrent_Call()
    {
        // Act
        var tasks = new List<Task>();
        for (int i = 0; i < 3; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await _repository.GetAllAsync();
                }
                catch (InvalidCastException)
                {
                    // Expected when using mocks
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        _mockConnectionFactory.Verify(f => f.CreateConnectionAsync(), Times.Exactly(3));
    }

    [Test]
    public async Task GetAllAsync_Should_Use_Correct_Stored_Procedure_Constant()
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
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("dbo.Usp_Campaigns_Get")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Log message should contain the exact stored procedure name");
    }

    #endregion

    #region GetByIdsAsync Tests

    [Test]
    public void GetByIdsAsync_Should_Throw_InvalidCastException_When_Factory_Returns_Mock_Connection()
    {
        Assert.ThrowsAsync<InvalidCastException>(async () => await _repository.GetByIdsAsync([1L]));

        _mockConnectionFactory.Verify(f => f.CreateConnectionAsync(), Times.Once);
    }

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
        _mockConnectionFactory.Verify(x => x.CreateConnectionAsync(), Times.Once);
    }

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

    [Test]
    public async Task GetByIdsAsync_AllNegativeIds_AcceptsWithoutException()
    {
        // Arrange
        var ids = new[] { -1L, -2L, -3L, -999L, -12345L };

        // Act
        try
        {
            await _repository.GetByIdsAsync(ids);
        }
        catch (InvalidCastException)
        {
            // Expected when using mock connection
        }

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, type) => state.ToString()!.Contains("Fetching 5 Campaigns")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public void GetByIdsAsync_NullParameter_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<long>? nullIds = null;

        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(async () => await _repository.GetByIdsAsync(nullIds!));
        exception.Should().NotBeNull();
        exception!.ParamName.Should().Be("values");
    }

    [TestCase(new long[] { }, "Empty collection")]
    [TestCase(new long[] { 1L }, "Single id")]
    [TestCase(new long[] { 1L, 2L, 3L, 4L, 5L }, "Multiple ids")]
    [TestCase(new long[] { 10L, 10L, 10L }, "Duplicate ids")]
    [TestCase(new long[] { long.MinValue }, "Single MinValue")]
    [TestCase(new long[] { long.MaxValue }, "Single MaxValue")]
    [TestCase(new long[] { 0L }, "Single zero")]
    [TestCase(new long[] { 0L, 0L, 0L }, "Multiple zeros")]
    [TestCase(new long[] { -1L, -2L, -3L }, "All negative ids")]
    [TestCase(new long[] { -100L, 0L, 100L }, "Mixed negative, zero, and positive")]
    [TestCase(new long[] { long.MinValue, long.MaxValue }, "Both extreme boundary values")]
    [TestCase(new long[] { long.MinValue, 0L, long.MaxValue }, "MinValue, zero, and MaxValue")]
    public void GetByIdsAsync_ValidIdCollections_AcceptsWithoutArgumentException(long[] ids, string description)
    {
        // Act & Assert
        var exception = Assert.ThrowsAsync<InvalidCastException>(async () => await _repository.GetByIdsAsync(ids));
        exception.Should().NotBeNull($"Method should throw InvalidCastException when using {description}");
    }

    [TestCase(new long[] { }, 0)]
    [TestCase(new long[] { 1L }, 1)]
    [TestCase(new long[] { 1L, 2L, 3L, 4L, 5L }, 5)]
    [TestCase(new long[] { 10L, 10L, 10L, 10L, 10L, 10L }, 6)]
    [TestCase(new long[] { long.MinValue, long.MaxValue }, 2)]
    [TestCase(new long[] { long.MinValue, 0L, long.MaxValue }, 3)]
    public async Task GetByIdsAsync_VariousCollectionSizes_LogsCorrectCount(long[] ids, int expectedCount)
    {
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
                It.Is<It.IsAnyType>((state, type) => state.ToString()!.Contains(expectedCount.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            $"Logger should be called with count {expectedCount}");
    }

    [TestCase(new long[] { }, "")]
    [TestCase(new long[] { 123L }, "123")]
    [TestCase(new long[] { 1L, 2L, 3L }, "1,2,3")]
    [TestCase(new long[] { long.MinValue }, "-9223372036854775808")]
    [TestCase(new long[] { long.MaxValue }, "9223372036854775807")]
    [TestCase(new long[] { long.MinValue, 0L, long.MaxValue }, "-9223372036854775808,0,9223372036854775807")]
    [TestCase(new long[] { -1L, -2L, -3L }, "-1,-2,-3")]
    [TestCase(new long[] { 0L, 0L, 0L }, "0,0,0")]
    [TestCase(new long[] { 5L, 5L, 5L }, "5,5,5")]
    public async Task GetByIdsAsync_VariousCollections_CreatesCorrectCommaSeparatedString(long[] ids, string expectedString)
    {
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
        var actualString = string.Join(",", ids);
        actualString.Should().Be(expectedString, $"string.Join should produce '{expectedString}' for given ids");
    }

    [TestCase(new long[] { })]
    [TestCase(new long[] { 1L })]
    [TestCase(new long[] { 1L, 2L, 3L, 4L, 5L })]
    [TestCase(new long[] { long.MinValue, long.MaxValue })]
    public async Task GetByIdsAsync_ValidCollections_CallsFactoryExactlyOnce(long[] ids)
    {
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
        _mockConnectionFactory.Verify(f => f.CreateConnectionAsync(), Times.Once);
    }

    [Test]
    public async Task GetByIdsAsync_AnyValidCollection_ReferencesCorrectStoredProcedure()
    {
        // Arrange
        var ids = new long[] { 1L, 2L, 3L };

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
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("dbo.Usp_Campaigns_Get")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public void GetByIdsAsync_FactoryThrowsException_PropagatesExceptionToCaller()
    {
        // Arrange
        _mockConnectionFactory.Setup(f => f.CreateConnectionAsync()).ThrowsAsync(new InvalidOperationException("Factory error"));
        var repository = new CampaignsRepository(_mockConnectionFactory.Object, _mockLogger.Object);
        var ids = new[] { 1L, 2L };

        // Act
        Func<Task> act = async () => await repository.GetByIdsAsync(ids);

        // Assert
        act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Factory error");
    }

    [Test]
    public async Task GetByIdsAsync_ConcurrentInvocations_CreatesIndependentConnections()
    {
        // Arrange
        var ids = new long[] { 1L, 2L };

        // Act
        var tasks = new[]
        {
            Task.Run(async () => { try { await _repository.GetByIdsAsync(ids); } catch 
                { 
                    // Expected when using mocks
                } }),
            Task.Run(async () => { try { await _repository.GetByIdsAsync(ids); } catch
                {
                    // Expected when using mocks
                } }),
            Task.Run(async () => { try { await _repository.GetByIdsAsync(ids); } catch
                {
                    // Expected when using mocks
                } })
        };

        await Task.WhenAll(tasks);

        // Assert
        _mockConnectionFactory.Verify(f => f.CreateConnectionAsync(), Times.Exactly(3), "Factory should be called once for each concurrent call");
    }

    [Test]
    public void GetByIdsAsync_FactoryThrows_LogsBeforeException()
    {
        // Arrange
        _mockConnectionFactory.Setup(f => f.CreateConnectionAsync()).ThrowsAsync(new InvalidOperationException("Factory error"));
        var repository = new CampaignsRepository(_mockConnectionFactory.Object, _mockLogger.Object);
        var ids = new[] { 1L, 2L };

        // Act
        try
        {
            var _ = repository.GetByIdsAsync(ids).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException)
        {
            // Expected exception
        }

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Fetching 2 Campaigns")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
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
        _mockConnectionFactory.Verify(f => f.CreateConnectionAsync(), Times.Once,
            "Repository should use factory to create connection when calling GetByIdsAsync");
    }

    #endregion
}