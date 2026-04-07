using Azure.Core;
using DAS.DigitalEngagement.CampaignInterest.Data.Helpers;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace DAS.DigitalEngagement.CampaignInterest.Data.UnitTests;

[TestFixture]
public class UnitOfWorkTests
{
    private Mock<TokenCredential> _mockTokenCredential = null!;
    private Mock<ILoggerFactory> _mockLoggerFactory = null!;
    private Mock<IOptions<ConnectionString>> _connectionStringMock = null!;
    private UnitOfWork _unitOfWork = null!;

    [SetUp]
    public void Setup()
    {
        _mockTokenCredential = new Mock<TokenCredential>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);

        _connectionStringMock = new Mock<IOptions<ConnectionString>>();

        _connectionStringMock
            .Setup(cs => cs.Value)
            .Returns(new ConnectionString { DataMart = "", CampaignsDatabase = "FakeConnectionString" });

        _unitOfWork = new UnitOfWork(_mockTokenCredential.Object, _connectionStringMock.Object, _mockLoggerFactory.Object);
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
        var unitOfWork = new UnitOfWork(_mockTokenCredential.Object, _connectionStringMock.Object, _mockLoggerFactory.Object);

        // Assert
        Assert.That(unitOfWork, Is.Not.Null);
        Assert.That(unitOfWork, Is.InstanceOf<IUnitOfWork>());
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
    public async Task BeginAsync_Should_Log_Information_Message()
    {
        // Act
        await _unitOfWork.BeginAsync();

        // Assert
        _mockLoggerFactory.Verify(x => x.CreateLogger(It.IsAny<string>()), Times.AtLeastOnce);
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
    }

    #endregion

    #region Repository Property Tests

    [Test]
    public async Task Repository_Properties_Should_Return_Same_Instance_After_Initialization()
    {
        // Arrange
        await _unitOfWork.BeginAsync();

        // Act
        var firstBouncedEmails = _unitOfWork.BouncedEmails;
        var firstCampaigns = _unitOfWork.Campaigns;
        var firstClickedLinks = _unitOfWork.ClickedLinks;
        var firstDisplayedEmails = _unitOfWork.DisplayedEmails;
        var firstUnsubscribedContacts = _unitOfWork.UnsubscribedContacts;
        var firstCampaignImportMetadata = _unitOfWork.CampaignImportMetadata;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_unitOfWork.BouncedEmails, Is.SameAs(firstBouncedEmails));
            Assert.That(_unitOfWork.Campaigns, Is.SameAs(firstCampaigns));
            Assert.That(_unitOfWork.ClickedLinks, Is.SameAs(firstClickedLinks));
            Assert.That(_unitOfWork.DisplayedEmails, Is.SameAs(firstDisplayedEmails));
            Assert.That(_unitOfWork.UnsubscribedContacts, Is.SameAs(firstUnsubscribedContacts));
            Assert.That(_unitOfWork.CampaignImportMetadata, Is.SameAs(firstCampaignImportMetadata));
        });
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
        await using (var unitOfWork = new UnitOfWork(_mockTokenCredential.Object, _connectionStringMock.Object, _mockLoggerFactory.Object))
        {
            await unitOfWork.BeginAsync();
            Assert.That(unitOfWork.BouncedEmails, Is.Not.Null);
        }

        // Assert - no exception should be thrown
        Assert.Pass("UnitOfWork disposed successfully with using statement");
    }

    #endregion

    #region Integration Tests

    [Test]
    public async Task UnitOfWork_Should_Initialize_All_Six_Repositories()
    {
        // Act
        await _unitOfWork.BeginAsync();

        // Assert
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

    #region Resource Management Tests

    [Test]
    public async Task Multiple_UnitOfWork_Instances_Should_Be_Independent()
    {
        // Arrange
        var unitOfWork1 = new UnitOfWork(_mockTokenCredential.Object, _connectionStringMock.Object, _mockLoggerFactory.Object);
        var unitOfWork2 = new UnitOfWork(_mockTokenCredential.Object, _connectionStringMock.Object, _mockLoggerFactory.Object);

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
