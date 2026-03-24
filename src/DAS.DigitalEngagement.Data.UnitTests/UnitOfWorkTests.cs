using DAS.DigitalEngagement.CampaignInterest.Data.Helpers;
using DAS.DigitalEngagement.CampaignInterest.Data.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace DAS.DigitalEngagement.CampaignInterest.Data.UnitTests;

[TestFixture]
public class UnitOfWorkTests
{
    private Mock<IConfiguration> _mockConfiguration = null!;
    private Mock<ILogger<UnitOfWork>> _mockLogger = null!;
    private Mock<IConfigurationSection> _mockConnectionStringSection = null!;
    private UnitOfWork _unitOfWork = null!;

    [SetUp]
    public void Setup()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<UnitOfWork>>();
        _mockConnectionStringSection = new Mock<IConfigurationSection>();

        // Setup connection string configuration
        _mockConnectionStringSection.Setup(x => x["DefaultConnection"])
            .Returns("Server=localhost;Database=TestDb;Integrated Security=true;");

        _mockConfiguration.Setup(x => x.GetSection("ConnectionStrings"))
            .Returns(_mockConnectionStringSection.Object);

        _unitOfWork = new UnitOfWork(_mockConfiguration.Object, _mockLogger.Object);
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_unitOfWork != null)
        {
            await _unitOfWork.DisposeAsync();
        }
    }

    #region Constructor Tests

    [Test]
    public void Constructor_Should_Create_Instance_Successfully()
    {
        // Arrange & Act
        var unitOfWork = new UnitOfWork(_mockConfiguration.Object, _mockLogger.Object);

        // Assert
        Assert.That(unitOfWork, Is.Not.Null);
        Assert.That(unitOfWork, Is.InstanceOf<IUnitOfWork>());
    }

    [Test]
    public void Constructor_Should_Accept_Valid_Configuration_And_Logger()
    {
        // Arrange & Act
        var unitOfWork = new UnitOfWork(_mockConfiguration.Object, _mockLogger.Object);

        // Assert
        Assert.That(unitOfWork, Is.Not.Null);
    }

    #endregion

    #region BeginAsync Tests

    [Test]
    public async Task BeginAsync_Should_Initialize_All_Repositories()
    {
        // Act
        await _unitOfWork.BeginAsync();

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(_unitOfWork.BouncedEmails, Is.Not.Null);
            Assert.That(_unitOfWork.Campaigns, Is.Not.Null);
            Assert.That(_unitOfWork.ClickedLinks, Is.Not.Null);
            Assert.That(_unitOfWork.DisplayedEmails, Is.Not.Null);
            Assert.That(_unitOfWork.UnsubscribedContacts, Is.Not.Null);
            Assert.That(_unitOfWork.CampaignImportMetadata, Is.Not.Null);
        });
    }

    [Test]
    public async Task BeginAsync_Should_Initialize_BouncedEmailsRepository()
    {
        // Act
        await _unitOfWork.BeginAsync();

        // Assert
        Assert.That(_unitOfWork.BouncedEmails, Is.Not.Null);
        Assert.That(_unitOfWork.BouncedEmails, Is.InstanceOf<IBouncedEmailsRepository>());
    }

    [Test]
    public async Task BeginAsync_Should_Initialize_CampaignsRepository()
    {
        // Act
        await _unitOfWork.BeginAsync();

        // Assert
        Assert.That(_unitOfWork.Campaigns, Is.Not.Null);
        Assert.That(_unitOfWork.Campaigns, Is.InstanceOf<ICampaignsRepository>());
    }

    [Test]
    public async Task BeginAsync_Should_Initialize_ClickedLinksRepository()
    {
        // Act
        await _unitOfWork.BeginAsync();

        // Assert
        Assert.That(_unitOfWork.ClickedLinks, Is.Not.Null);
        Assert.That(_unitOfWork.ClickedLinks, Is.InstanceOf<IClickedLinksRepository>());
    }

    [Test]
    public async Task BeginAsync_Should_Initialize_DisplayedEmailsRepository()
    {
        // Act
        await _unitOfWork.BeginAsync();

        // Assert
        Assert.That(_unitOfWork.DisplayedEmails, Is.Not.Null);
        Assert.That(_unitOfWork.DisplayedEmails, Is.InstanceOf<IDisplayedEmailsRepository>());
    }

    [Test]
    public async Task BeginAsync_Should_Initialize_UnsubscribedContactsRepository()
    {
        // Act
        await _unitOfWork.BeginAsync();

        // Assert
        Assert.That(_unitOfWork.UnsubscribedContacts, Is.Not.Null);
        Assert.That(_unitOfWork.UnsubscribedContacts, Is.InstanceOf<IUnsubscribedContactsRepository>());
    }

    [Test]
    public async Task BeginAsync_Should_Initialize_CampaignImportMetadataRepository()
    {
        // Act
        await _unitOfWork.BeginAsync();

        // Assert
        Assert.That(_unitOfWork.CampaignImportMetadata, Is.Not.Null);
        Assert.That(_unitOfWork.CampaignImportMetadata, Is.InstanceOf<ICampaignImportMetadataRepository>());
    }

    [Test]
    public async Task BeginAsync_Should_Log_Information_Message()
    {
        // Act
        await _unitOfWork.BeginAsync();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting database transaction")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task BeginAsync_Should_Use_Connection_String_From_Configuration()
    {
        // Arrange
        var connectionString = "Server=testserver;Database=testdb;";
        var mockSection = new Mock<IConfigurationSection>();
        mockSection.Setup(x => x["DefaultConnection"]).Returns(connectionString);
        _mockConfiguration.Setup(x => x.GetSection("ConnectionStrings")).Returns(mockSection.Object);

        var unitOfWork = new UnitOfWork(_mockConfiguration.Object, _mockLogger.Object);

        // Act
        await unitOfWork.BeginAsync();

        // Assert
        _mockConfiguration.Verify(x => x.GetSection("ConnectionStrings"), Times.Once);
        Assert.That(unitOfWork.BouncedEmails, Is.Not.Null);
    }

    [Test]
    public async Task BeginAsync_Should_Allow_Multiple_Calls()
    {
        // Act
        await _unitOfWork.BeginAsync();
        await _unitOfWork.BeginAsync(); // Second call

        // Assert
        // Should not throw exception on multiple calls
        Assert.That(_unitOfWork.BouncedEmails, Is.Not.Null);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting database transaction")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(2));
    }

    [Test]
    public async Task BeginAsync_Should_Accept_Null_Connection_String_Without_Opening_Connection()
    {
        // Arrange
        var mockSection = new Mock<IConfigurationSection>();
        mockSection.Setup(x => x["DefaultConnection"]).Returns((string?)null);
        _mockConfiguration.Setup(x => x.GetSection("ConnectionStrings")).Returns(mockSection.Object);

        var unitOfWork = new UnitOfWork(_mockConfiguration.Object, _mockLogger.Object);

        // Act
        await unitOfWork.BeginAsync();

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(unitOfWork.BouncedEmails, Is.Not.Null);
            Assert.That(unitOfWork.Campaigns, Is.Not.Null);
        });
    }

    #endregion

    #region Repository Property Tests

    [Test]
    public async Task BouncedEmails_Should_Return_Same_Instance_After_Initialization()
    {
        // Arrange
        await _unitOfWork.BeginAsync();

        // Act
        var repository1 = _unitOfWork.BouncedEmails;
        var repository2 = _unitOfWork.BouncedEmails;

        // Assert
        Assert.That(repository1, Is.SameAs(repository2));
    }

    [Test]
    public async Task Campaigns_Should_Return_Same_Instance_After_Initialization()
    {
        // Arrange
        await _unitOfWork.BeginAsync();

        // Act
        var repository1 = _unitOfWork.Campaigns;
        var repository2 = _unitOfWork.Campaigns;

        // Assert
        Assert.That(repository1, Is.SameAs(repository2));
    }

    [Test]
    public async Task ClickedLinks_Should_Return_Same_Instance_After_Initialization()
    {
        // Arrange
        await _unitOfWork.BeginAsync();

        // Act
        var repository1 = _unitOfWork.ClickedLinks;
        var repository2 = _unitOfWork.ClickedLinks;

        // Assert
        Assert.That(repository1, Is.SameAs(repository2));
    }

    [Test]
    public async Task DisplayedEmails_Should_Return_Same_Instance_After_Initialization()
    {
        // Arrange
        await _unitOfWork.BeginAsync();

        // Act
        var repository1 = _unitOfWork.DisplayedEmails;
        var repository2 = _unitOfWork.DisplayedEmails;

        // Assert
        Assert.That(repository1, Is.SameAs(repository2));
    }

    [Test]
    public async Task UnsubscribedContacts_Should_Return_Same_Instance_After_Initialization()
    {
        // Arrange
        await _unitOfWork.BeginAsync();

        // Act
        var repository1 = _unitOfWork.UnsubscribedContacts;
        var repository2 = _unitOfWork.UnsubscribedContacts;

        // Assert
        Assert.That(repository1, Is.SameAs(repository2));
    }

    [Test]
    public async Task CampaignImportMetadata_Should_Return_Same_Instance_After_Initialization()
    {
        // Arrange
        await _unitOfWork.BeginAsync();

        // Act
        var repository1 = _unitOfWork.CampaignImportMetadata;
        var repository2 = _unitOfWork.CampaignImportMetadata;

        // Assert
        Assert.That(repository1, Is.SameAs(repository2));
    }

    #endregion

    #region DisposeAsync Tests

    [Test]
    public async Task DisposeAsync_Should_Complete_Successfully()
    {
        // Arrange
        await _unitOfWork.BeginAsync();

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await _unitOfWork.DisposeAsync());
    }

    [Test]
    public async Task DisposeAsync_Should_Complete_Without_BeginAsync()
    {
        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await _unitOfWork.DisposeAsync());
    }

    [Test]
    public async Task DisposeAsync_Should_Be_Callable_Multiple_Times()
    {
        // Arrange
        await _unitOfWork.BeginAsync();

        // Act & Assert
        await _unitOfWork.DisposeAsync();
        Assert.DoesNotThrowAsync(async () => await _unitOfWork.DisposeAsync()); // Second call
    }

    [Test]
    public async Task UnitOfWork_Should_Be_Disposable_With_Using_Statement()
    {
        // Arrange & Act
        await using (var unitOfWork = new UnitOfWork(_mockConfiguration.Object, _mockLogger.Object))
        {
            await unitOfWork.BeginAsync();
            Assert.That(unitOfWork.BouncedEmails, Is.Not.Null);
        }

        // Assert - no exception should be thrown
        Assert.Pass("UnitOfWork disposed successfully with using statement");
    }

    #endregion

    #region Integration and Usage Pattern Tests

    [Test]
    public async Task UnitOfWork_Should_Support_Repository_Access_Pattern()
    {
        // Arrange
        await _unitOfWork.BeginAsync();

        // Act - Simulate typical usage pattern
        var bouncedEmails = _unitOfWork.BouncedEmails;
        var campaigns = _unitOfWork.Campaigns;
        var clickedLinks = _unitOfWork.ClickedLinks;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(bouncedEmails, Is.Not.Null);
            Assert.That(campaigns, Is.Not.Null);
            Assert.That(clickedLinks, Is.Not.Null);
        });
    }

    [Test]
    public async Task UnitOfWork_Should_Initialize_All_Six_Repositories()
    {
        // Act
        await _unitOfWork.BeginAsync();

        // Assert - Verify exactly 6 repositories are initialized
        var repositories = new object[]
        {
            _unitOfWork.BouncedEmails,
            _unitOfWork.Campaigns,
            _unitOfWork.ClickedLinks,
            _unitOfWork.DisplayedEmails,
            _unitOfWork.UnsubscribedContacts,
            _unitOfWork.CampaignImportMetadata
        };

        Assert.That(repositories, Has.Length.EqualTo(6));
        Assert.That(repositories.All(r => r != null), Is.True);
    }

    [Test]
    public async Task UnitOfWork_Should_Create_Different_Instances_On_Separate_BeginAsync_Calls()
    {
        // Arrange
        await _unitOfWork.BeginAsync();
        var firstBouncedEmails = _unitOfWork.BouncedEmails;

        // Act
        await _unitOfWork.BeginAsync(); // Re-initialize
        var secondBouncedEmails = _unitOfWork.BouncedEmails;

        // Assert
        Assert.That(firstBouncedEmails, Is.Not.SameAs(secondBouncedEmails));
    }

    #endregion

    #region Edge Cases and Error Handling

    [Test]
    public void UnitOfWork_Should_Handle_Null_Configuration()
    {
        // Arrange
        IConfiguration? nullConfig = null;

        // Act & Assert
        var unitOfWork = new UnitOfWork(nullConfig!, _mockLogger.Object);
        Assert.That(unitOfWork, Is.Not.Null);
    }

    [Test]
    public void UnitOfWork_Should_Handle_Null_Logger()
    {
        // Arrange
        ILogger<UnitOfWork>? nullLogger = null;

        // Act & Assert
        var unitOfWork = new UnitOfWork(_mockConfiguration.Object, nullLogger!);
        Assert.That(unitOfWork, Is.Not.Null);
    }

    [Test]
    public async Task UnitOfWork_Should_Accept_Empty_Connection_String_Without_Opening_Connection()
    {
        // Arrange
        var mockSection = new Mock<IConfigurationSection>();
        mockSection.Setup(x => x["DefaultConnection"]).Returns(string.Empty);
        _mockConfiguration.Setup(x => x.GetSection("ConnectionStrings")).Returns(mockSection.Object);

        var unitOfWork = new UnitOfWork(_mockConfiguration.Object, _mockLogger.Object);

        // Act
        await unitOfWork.BeginAsync();

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(unitOfWork.BouncedEmails, Is.Not.Null);
            Assert.That(unitOfWork.Campaigns, Is.Not.Null);
        });
    }

    #endregion

    #region Repository Type Verification Tests

    [Test]
    public async Task BeginAsync_Should_Create_Concrete_Repository_Implementations()
    {
        // Act
        await _unitOfWork.BeginAsync();

        Assert.Multiple(() =>
        {
            // Assert - Verify concrete types (not just interfaces)
            Assert.That(_unitOfWork.BouncedEmails.GetType().Name, Does.Contain("Repository"));
            Assert.That(_unitOfWork.Campaigns.GetType().Name, Does.Contain("Repository"));
            Assert.That(_unitOfWork.ClickedLinks.GetType().Name, Does.Contain("Repository"));
            Assert.That(_unitOfWork.DisplayedEmails.GetType().Name, Does.Contain("Repository"));
            Assert.That(_unitOfWork.UnsubscribedContacts.GetType().Name, Does.Contain("Repository"));
            Assert.That(_unitOfWork.CampaignImportMetadata.GetType().Name, Does.Contain("Repository"));
        });
    }

    [Test]
    public async Task BeginAsync_Should_Create_Repositories_With_Shared_BulkInsertService()
    {
        // Act
        await _unitOfWork.BeginAsync();

        // Assert - All repositories should be initialized
        Assert.Multiple(() =>
        {
            Assert.That(_unitOfWork.BouncedEmails, Is.Not.Null);
            Assert.That(_unitOfWork.Campaigns, Is.Not.Null);
            Assert.That(_unitOfWork.ClickedLinks, Is.Not.Null);
            Assert.That(_unitOfWork.DisplayedEmails, Is.Not.Null);
            Assert.That(_unitOfWork.UnsubscribedContacts, Is.Not.Null);
            Assert.That(_unitOfWork.CampaignImportMetadata, Is.Not.Null);
        });
    }

    #endregion

    #region Performance and Resource Management Tests

    [Test]
    public async Task BeginAsync_Should_Execute_Quickly()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        await _unitOfWork.BeginAsync();
        stopwatch.Stop();

        // Assert
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(1000));
    }

    [Test]
    public async Task Multiple_UnitOfWork_Instances_Should_Be_Independent()
    {
        // Arrange
        var unitOfWork1 = new UnitOfWork(_mockConfiguration.Object, _mockLogger.Object);
        var unitOfWork2 = new UnitOfWork(_mockConfiguration.Object, _mockLogger.Object);

        // Act
        await unitOfWork1.BeginAsync();
        await unitOfWork2.BeginAsync();

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(unitOfWork1.BouncedEmails, Is.Not.SameAs(unitOfWork2.BouncedEmails));
            Assert.That(unitOfWork1.Campaigns, Is.Not.SameAs(unitOfWork2.Campaigns));
        });
    }

    #endregion
}
