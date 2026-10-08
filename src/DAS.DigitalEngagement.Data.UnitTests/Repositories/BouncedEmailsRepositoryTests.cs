using DAS.DigitalEngagement.CampaignInterest.Data.Models;
//using DAS.DigitalEngagement.CampaignInterest.Data.Repositories;
using DAS.DigitalEngagement.CampaignInterest.Data.Service;
using Moq;

namespace DAS.DigitalEngagement.CampaignInterest.Data.UnitTests.Repositories;

[TestFixture]
public class BouncedEmailsRepositoryTests
{
    //private BouncedEmailsRepository _bouncedEmailsRepository = null!;
    //private Mock<IBulkInsertService> _mockBulkInsertService = null!;
    //private List<BouncedEmails> _bouncedEmails;

    //[SetUp]
    //public void Setup()
    //{
    //    _bouncedEmails =
    //    [
    //        new BouncedEmails { Id = 1, ContactEmail = "Test1@test.com" },
    //        new BouncedEmails { Id = 2, ContactEmail = "Test2@test.com" }
    //    ];

    //    _mockBulkInsertService = new Mock<IBulkInsertService>();
    //}

    //[Test]
    //public async Task BulkInsertAsync_Should_Commit_When_Successful()
    //{
    //    // Arrange
    //    _bouncedEmailsRepository = new BouncedEmailsRepository(_mockBulkInsertService.Object);

    //    // Act
    //    await _bouncedEmailsRepository.BulkInsertAsync(_bouncedEmails);

    //    // Assert
    //    _mockBulkInsertService.Verify(x => x.BulkInsertAsync(It.IsAny<IEnumerable<BouncedEmails>>(), It.IsAny<string>()), Times.Once);
    //}
}
