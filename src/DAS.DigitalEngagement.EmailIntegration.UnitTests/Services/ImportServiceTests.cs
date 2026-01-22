using DAS.DigitalEngagement.Application.Repositories;
using DAS.DigitalEngagement.Application.Services;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.Models.Import;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAS.DigitalEngagement.EmailIntegration.UnitTests.Services
{
    [TestFixture]
    public class ImportServiceTests
    {
        private Mock<IExternalApiService> _mockExternalApiService;
        private Mock<ILogger<ImportService>> _mockLogger;
        private ImportService _importService;

        [SetUp]
        public void SetUp()
        {
            _mockExternalApiService = new Mock<IExternalApiService>();
            _mockLogger = new Mock<ILogger<ImportService>>();
            _importService = new ImportService(_mockExternalApiService.Object, _mockLogger.Object);
        }

        [Test]
        public async Task ImportEmployeeRegistration_ShouldCallExternalApiAndLogInformation()
        {
            // Arrange
            var leads = new List<string> { "Lead1", "Lead2" };
            _mockExternalApiService
                .Setup(service => service.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(string.Empty); // Fix: ReturnsAsync for Task<string>

            // Act
            var result = await _importService.ImportEmployeeRegistration(leads);

            // Assert
            _mockExternalApiService.Verify(service => service.GetDataAsync("Contacts/Export/?$filter=ID eq 182"), Times.Once);
            _mockLogger.Verify(logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Called External API to import employee registrations.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Container, Is.EqualTo("Test"));
            Assert.That(result.Id, Is.EqualTo("1"));
            Assert.That(result.Name, Is.EqualTo("Test"));
            Assert.That(result.ValidationError, Is.EqualTo("Test"));
            Assert.That(result.BulkImportJobs.Count, Is.EqualTo(1));
            Assert.That(result.BulkImportJobs[0].Status, Is.EqualTo("Failed"));
        }
    }
}