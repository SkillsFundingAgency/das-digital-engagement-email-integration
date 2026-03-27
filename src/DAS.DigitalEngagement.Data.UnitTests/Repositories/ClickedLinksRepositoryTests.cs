using DAS.DigitalEngagement.CampaignInterest.Data.Models;
using DAS.DigitalEngagement.CampaignInterest.Data.Repositories;
using DAS.DigitalEngagement.CampaignInterest.Data.Service;
using Moq;

namespace DAS.DigitalEngagement.CampaignInterest.Data.UnitTests.Repositories;

[TestFixture]
public class ClickedLinksRepositoryTests
{
    private ClickedLinksRepository _clickedLinksRepository = null!;
    private Mock<IBulkInsertService> _mockBulkInsertService = null!;
    private List<ClickedLinks> _clickedLinks;

    [SetUp]
    public void Setup()
    {
        _clickedLinks =
        [
            new ClickedLinks { Id = 1, ContactEmail = "Test1@test.com" },
            new ClickedLinks { Id = 2, ContactEmail = "Test2@test.com" }
        ];

        _mockBulkInsertService = new Mock<IBulkInsertService>();
    }

    [Test]
    public async Task BulkInsertAsync_Should_Commit_When_Successful()
    {
        // Arrange
        _clickedLinksRepository = new ClickedLinksRepository(_mockBulkInsertService.Object);

        // Act
        await _clickedLinksRepository.BulkInsertAsync(_clickedLinks);

        // Assert
        _mockBulkInsertService.Verify(x => x.BulkInsertAsync(It.IsAny<IEnumerable<ClickedLinks>>(), It.IsAny<string>()), Times.Once);
    }
}
