using DAS.DigitalEngagement.CampaignInterest.Data.Models;
using DAS.DigitalEngagement.CampaignInterest.Data.Repositories;
using DAS.DigitalEngagement.CampaignInterest.Data.Service;
using Moq;

namespace DAS.DigitalEngagement.CampaignInterest.Data.UnitTests.Repositories;

[TestFixture]
public class DisplayedEmailsRepositoryTests
{
    private DisplayedEmailsRepository _campaignsRepository = null!;
    private Mock<IBulkInsertService> _mockBulkInsertService = null!;
    private List<DisplayedEmails> _campaigns;

    [SetUp]
    public void Setup()
    {
        _campaigns =
        [
            new DisplayedEmails { Id = 1, ContactEmail = "Test1@test.com" },
            new DisplayedEmails { Id = 2, ContactEmail = "Test2@test.com" }
        ];

        _mockBulkInsertService = new Mock<IBulkInsertService>();
    }

    [Test]
    public async Task BulkInsertAsync_Should_Commit_When_Successful()
    {
        // Arrange
        _campaignsRepository = new DisplayedEmailsRepository(_mockBulkInsertService.Object);

        // Act
        await _campaignsRepository.BulkInsertAsync(_campaigns);

        // Assert
        _mockBulkInsertService.Verify(x => x.BulkInsertAsync(It.IsAny<IEnumerable<DisplayedEmails>>(), It.IsAny<string>()), Times.Once);
    }
}
