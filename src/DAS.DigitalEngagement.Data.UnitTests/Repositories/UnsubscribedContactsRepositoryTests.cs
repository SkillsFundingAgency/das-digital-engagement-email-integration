using DAS.DigitalEngagement.CampaignInterest.Data.Models;
using DAS.DigitalEngagement.CampaignInterest.Data.Repositories;
using DAS.DigitalEngagement.CampaignInterest.Data.Service;
using Moq;

namespace DAS.DigitalEngagement.CampaignInterest.Data.UnitTests.Repositories;

[TestFixture]
public class UnsubscribedContactsRepositoryTests
{
    private UnsubscribedContactsRepository _unsubscribedContactsRepository = null!;
    private Mock<IBulkInsertService> _mockBulkInsertService = null!;
    private List<UnsubscribedContacts> _unsubscribedContacts;
    [SetUp]
    public void Setup()
    {
        _unsubscribedContacts =
        [
            new UnsubscribedContacts { Id = 1, ContactEmail = "Test1@test.com" },
            new UnsubscribedContacts { Id = 2, ContactEmail = "Test2@test.com" }
        ];

        _mockBulkInsertService = new Mock<IBulkInsertService>();
    }

    [Test]
    public async Task BulkInsertAsync_Should_Commit_When_Successful()
    {
        // Arrange
        _unsubscribedContactsRepository = new UnsubscribedContactsRepository(_mockBulkInsertService.Object);

        // Act
        await _unsubscribedContactsRepository.BulkInsertAsync(_unsubscribedContacts);

        // Assert
        _mockBulkInsertService.Verify(x => x.BulkInsertAsync(It.IsAny<IEnumerable<UnsubscribedContacts>>(), It.IsAny<string>()), Times.Once);
    }
}
