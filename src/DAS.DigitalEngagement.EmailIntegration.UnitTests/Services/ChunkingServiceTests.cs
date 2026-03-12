using DAS.DigitalEngagement.Application.Services;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace DAS.DigitalEngagement.EmailIntegration.UnitTests.Services
{
    [TestFixture]
    public class ChunkingServiceTests
    {
        private Mock<IOptions<EmailMarketingApi>> _mockOptions;
        private ChunkingService _service;

        [SetUp]
        public void SetUp()
        {
            _mockOptions = new Mock<IOptions<EmailMarketingApi>>();
            _mockOptions.Setup(x => x.Value).Returns(new EmailMarketingApi
            {
                ApiBaseUrl = "https://dummy.url",
                ApiKey = "dummy-client-id",
                ApiRetryCount = 1,
                ChunkSizeKB = 2
            });
            _service = new ChunkingService(_mockOptions.Object);
        }

        [Test]
        public void GetChunks_ReturnsAllItemsInChunks()
        {
            // Arrange
            var items = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            long totalSize = 8000; // 8 KB

            // Act
            var chunks = _service.GetChunks(totalSize, items);

            // Assert
            var allItems = new List<int>();
            foreach (var chunk in chunks)
                allItems.AddRange(chunk);

            Assert.That(allItems.Count, Is.EqualTo(items.Count));
            foreach (var item in items)
                Assert.That(allItems, Does.Contain(item));
        }

        [Test]
        public void GetChunks_EmptyList_ReturnsEmpty()
        {
            // Arrange
            var items = new List<int>();
            long totalSize = 1000;

            // Act
            var chunks = _service.GetChunks(totalSize, items);

            // Assert
            Assert.That(!chunks.Any());
        }

        [Test]
        public void GetChunks_ChunkSizeIsAtLeastOne()
        {
            // Arrange
            var items = new List<int> { 1, 2, 3 };
            long totalSize = 1000000; // Large blob, small list

            // Act
            var chunks = _service.GetChunks(totalSize, items);

            // Assert
            foreach (var chunk in chunks)
                Assert.That(chunk.Count, Is.GreaterThanOrEqualTo(1));
        }
    }
}
