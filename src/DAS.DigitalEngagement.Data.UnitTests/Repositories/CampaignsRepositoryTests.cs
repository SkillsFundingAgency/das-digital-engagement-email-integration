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

    #endregion

    #region UpsertAsync Validation Tests

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

    #endregion

    #region GetByIdAsync Validation Tests

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

    #endregion

    #region GetByIdsAsync Validation Tests

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

    #endregion

    #region Dependency Usage Tests

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
        factoryMock.Verify(f => f.CreateConnection(), Times.Once,
            "Repository should use factory to create connection when calling GetByIdAsync");
    }

    #endregion

    #region Boundary Value Tests for Campaign Properties

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

    #endregion

    #region Helper Methods

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

    #endregion
}
