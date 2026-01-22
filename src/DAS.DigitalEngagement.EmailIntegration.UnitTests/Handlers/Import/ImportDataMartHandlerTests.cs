using DAS.DigitalEngagement.Application.Handlers.Import;
using DAS.DigitalEngagement.Application.Import.Handlers;
using DAS.DigitalEngagement.Application.Repositories.Interfaces;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.Models.Import;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DAS.DigitalEngagement.EmailIntegration.UnitTests.Handlers.Import
{
    [TestFixture]
    public class ImportDataMartHandlerTests
    {
        private Mock<ILogger<ImportDataMartHandler>> _mockLogger;
        private Mock<IDataMartRepository> _mockDataMartRepository;
        private Mock<IImportService> _mockImportService;
        private ImportDataMartHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _mockLogger = new Mock<ILogger<ImportDataMartHandler>>();
            _mockDataMartRepository = new Mock<IDataMartRepository>();
            _mockImportService = new Mock<IImportService>();

            _handler = new ImportDataMartHandler(
                _mockLogger.Object,
                _mockImportService.Object,
                _mockDataMartRepository.Object);
        }

        [Test]
        public async Task Handle_WhenObjectNameIsLead_ShouldCallImportEmployeeRegistrationAndReturnStatus()
        {
            // Arrange
            var config = new DataMartSettings
            {
                ObjectName = "Lead",
                ViewName = "TestView"
            };

            var mockData = new List<dynamic>();
            _mockDataMartRepository
                .Setup(repo => repo.RetrieveEmployeeRegistrationData(config.ViewName))
                .ReturnsAsync(mockData);

            var expectedStatus = new BulkImportStatus
            {
                Container = string.Empty,
                Name = string.Empty,
                Id = string.Empty,
                StartTime = DateTime.UtcNow,
                BulkImportJobs = new List<BulkImportJob>(),
                BulkImportJobStatus = new List<BulkImportJobStatus>(),
                ImportFileIsValid = true,
                ValidationError = string.Empty,
                HeaderErrors = Enumerable.Empty<string>()
            };
            _mockImportService
                .Setup(service => service.ImportEmployeeRegistration(mockData))
                .ReturnsAsync(expectedStatus);

            // Act
            var result = await _handler.Handle(config);

            // Assert
            Assert.That(result, Is.EqualTo(expectedStatus));
            _mockDataMartRepository.Verify(repo => repo.RetrieveEmployeeRegistrationData(config.ViewName), Times.Once);
            _mockImportService.Verify(service => service.ImportEmployeeRegistration(mockData), Times.Once);
        }

        [Test]
        public async Task Handle_WhenObjectNameIsNotLead_ShouldLogAndReturnDefaultBulkImportStatus()
        {
            // Arrange
            var config = new DataMartSettings
            {
                ObjectName = "NotLead",
                ViewName = "TestView"
            };

            var expectedStatus = new BulkImportStatus
            {
                Container = string.Empty,
                Name = string.Empty,
                Id = string.Empty,
                StartTime = DateTime.UtcNow,
                BulkImportJobs = new List<BulkImportJob>(),
                BulkImportJobStatus = new List<BulkImportJobStatus>(),
                ImportFileIsValid = false,
                ValidationError = "No Object name is configured in the Configuration",
                HeaderErrors = Enumerable.Empty<string>()
            };

            // Act
            var result = await _handler.Handle(config);

            // Assert
            Assert.That(result.ImportFileIsValid, Is.EqualTo(expectedStatus.ImportFileIsValid));
            Assert.That(result.ValidationError, Is.EqualTo(expectedStatus.ValidationError));

            _mockLogger.Verify(
            static logger => logger.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("No Object name is configured in the Configuration")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),Times.Once);
        }

        [Test]
        public void Constructor_WhenCalled_ShouldAssignDependencies()
        {
            // Act
            var handler = new ImportDataMartHandler(
                _mockLogger.Object,
                _mockImportService.Object,
                _mockDataMartRepository.Object);

            // Assert
            Assert.That(handler, Is.Not.Null);
        }


        [Test]
        public void Ctor_WithValidDependencies_DoesNotThrow()
        {
            // Arrange
            var logger = new Mock<ILogger<ImportDataMartHandler>>().Object;
            var importService = new Mock<IImportService>().Object;
            var repo = new Mock<IDataMartRepository>().Object;

            // Act & Assert
            Assert.DoesNotThrow(() => new ImportDataMartHandler(_mockLogger.Object,
                _mockImportService.Object,
                _mockDataMartRepository.Object));
        }


    }
}