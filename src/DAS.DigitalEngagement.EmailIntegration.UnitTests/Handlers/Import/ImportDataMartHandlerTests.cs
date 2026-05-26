using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DAS.DigitalEngagement.Application.Import.Handlers;
using DAS.DigitalEngagement.Application.Repositories.Interfaces;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.Models.Import;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace DAS.DigitalEngagement.EmailIntegration.UnitTests.Handlers.Import
{
    [TestFixture]
    public class ImportDataMartHandlerTests
    {
        private Mock<IDataMartRepository> _dataMartRepositoryMock;
        private Mock<ILogger<ImportDataMartHandler>> _loggerMock;
        private Mock<IImportService> _importServiceMock;
        private ImportDataMartHandler _handler;
        private static readonly int[] TemplatedUploadIds = new[] { 1 };

        [SetUp]
        public void SetUp()
        {
            _dataMartRepositoryMock = new Mock<IDataMartRepository>();
            _loggerMock = new Mock<ILogger<ImportDataMartHandler>>();
            _importServiceMock = new Mock<IImportService>();
            _handler = new ImportDataMartHandler(_loggerMock.Object, _importServiceMock.Object, _dataMartRepositoryMock.Object);
        }

        [Test]
        public async Task Handle_ReturnsFailed_WhenConfigDoesNotContainLead()
        {
            // Arrange
            var config = new List<DataMartSettings>
            {
                new DataMartSettings
                {
                    ObjectName = "NotLead",
                    ViewName = "SomeView",
                    FieldMapping = "SomeFieldMapping",
                    TemplatedUploadId = TemplatedUploadIds
                }
            };

            // Act
            var result = await _handler.Handle(config);

            // Assert
            Assert.That(result.Status, Is.EqualTo("Failed"));
            Assert.That(result.Messages, Does.Contain("Expected Object name is configured in the Configuration"));
            Assert.That(result.StartTime, Is.Not.Null);
            Assert.That(result.EndTime, Is.Not.Null);
        }

        [Test]
        public async Task Handle_ReturnsFailed_WhenContactImportTemplateDoesNotExist()
        {
            // Arrange
            var config = new List<DataMartSettings>
            {
                new DataMartSettings
                {
                    ObjectName = "Lead",
                    ViewName = "SomeView",
                    FieldMapping = "SomeFieldMapping",
                    TemplatedUploadId = TemplatedUploadIds
                }
            };
            _importServiceMock.Setup(x => x.IsContactImportTemplatesExist()).ReturnsAsync(false);

            // Act
            var result = await _handler.Handle(config);

            // Assert
            Assert.That(result.Status, Is.EqualTo("Failed"));
            Assert.That(result.Messages, Does.Contain("Contact import template is not available in E-shot."));
            Assert.That(result.StartTime, Is.Not.Null);
            Assert.That(result.EndTime, Is.Not.Null);
        }

        [Test]
        public async Task Handle_ReturnsImportSummaryResultFromImportService_WhenDataExists()
        {
            // Arrange
            var config = new List<DataMartSettings>
            {
                new DataMartSettings
                {
                    ObjectName = "Lead",
                    ViewName = "SomeView",
                    FieldMapping = "SomeFieldMapping",
                    TemplatedUploadId = new[] { 1 }
                }
            };
            _importServiceMock.Setup(x => x.IsContactImportTemplatesExist()).ReturnsAsync(true);
            var data = new List<object> { new object() };
            _dataMartRepositoryMock.Setup(x => x.RetrieveEmployeeRegistrationData()).ReturnsAsync(data);
            var expectedSummary = new ImportSummaryResult { Status = "Completed", Messages = new List<string> { "Imported" } };
            _importServiceMock.Setup(x => x.ImportEmployeeRegistration(data)).ReturnsAsync(expectedSummary);

            // Act
            var result = await _handler.Handle(config);

            // Assert
            Assert.That(result.Status, Is.EqualTo(expectedSummary.Status));
            Assert.That(result.Messages, Is.EqualTo(expectedSummary.Messages));
        }

        [Test]
        public async Task Handle_ReturnsCompleted_WhenNoDataToImport()
        {
            // Arrange
            var config = new List<DataMartSettings>
            {
                new DataMartSettings
                {
                    ObjectName = "Lead",
                    ViewName = "SomeView",
                    FieldMapping = "SomeFieldMapping",
                    TemplatedUploadId = TemplatedUploadIds
                }
            };
            _importServiceMock.Setup(x => x.IsContactImportTemplatesExist()).ReturnsAsync(true);
            _dataMartRepositoryMock.Setup(x => x.RetrieveEmployeeRegistrationData()).ReturnsAsync(new List<object>());

            // Act
            var result = await _handler.Handle(config);

            // Assert
            Assert.That(result.Status, Is.EqualTo("Completed"));
            Assert.That(result.Messages, Does.Contain("No records to import."));
            Assert.That(result.StartTime, Is.Not.Null);
            Assert.That(result.EndTime, Is.Not.Null);
        }
    }
}