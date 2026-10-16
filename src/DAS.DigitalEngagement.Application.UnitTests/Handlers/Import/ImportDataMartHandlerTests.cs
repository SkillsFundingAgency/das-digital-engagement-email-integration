using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DAS.DigitalEngagement.Application.Import.Handlers;
using DAS.DigitalEngagement.CampaignInterest.Data.Repositories.Interfaces;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.Models.Import;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace DAS.DigitalEngagement.Application.UnitTests.Handlers.Import
{
    [TestFixture]
    public class ImportDataMartHandlerTests
    {
        private Mock<IDataMartRepository> _dataMartRepositoryMock;
        private Mock<IImportService> _importServiceMock;
        private Mock<ILogger<ImportDataMartHandler>> _loggerMock;
        private ImportDataMartHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _dataMartRepositoryMock = new Mock<IDataMartRepository>();
            _importServiceMock = new Mock<IImportService>();
            _loggerMock = new Mock<ILogger<ImportDataMartHandler>>();
            _handler = new ImportDataMartHandler(_loggerMock.Object, _importServiceMock.Object, _dataMartRepositoryMock.Object);
        }

        [Test]
        public async Task Handle_ReturnsFailed_WhenObjectNameIsNotLead()
        {
            var config = new DataMartSettings { ObjectName = "NotLead", ViewName = "View", FieldMapping = "Mapping", TemplatedUploadId = [1] };

            var result = await _handler.Handle(new List<DataMartSettings> { config });

            Assert.AreEqual("Failed", result.Status);
            Assert.That(result.Messages, Does.Contain("Expected Object name is configured in the Configuration"));
        }

        [Test]
        public async Task Handle_ReturnsFailed_WhenContactImportTemplateDoesNotExist()
        {
            var config = new DataMartSettings { ObjectName = "Lead", ViewName = "View", FieldMapping = "Mapping", TemplatedUploadId = [1] };
            _importServiceMock.Setup(x => x.IsContactImportTemplatesExist()).ReturnsAsync(false);

            var result = await _handler.Handle(new List<DataMartSettings> { config });

            Assert.AreEqual("Failed", result.Status);
            Assert.That(result.Messages, Does.Contain("Contact import template is not available in E-shot."));
        }

        [Test]
        public async Task Handle_ReturnsCompleted_WhenNoRecordsToImport()
        {
            var config = new DataMartSettings { ObjectName = "Lead", ViewName = "View", FieldMapping = "Mapping", TemplatedUploadId = [1] };
            _importServiceMock.Setup(x => x.IsContactImportTemplatesExist()).ReturnsAsync(true);
            _dataMartRepositoryMock.Setup(x => x.RetrieveEmployeeRegistrationData()).ReturnsAsync(new List<object>());

            var result = await _handler.Handle(new List<DataMartSettings> { config });

            Assert.AreEqual("Completed", result.Status);
            Assert.That(result.Messages, Does.Contain("No records to import."));
            Assert.AreEqual(0, result.TotalRecordsProcessed);
        }

        [Test]
        public async Task Handle_ReturnsCompleted_WhenRecordsAreImported()
        {
            var config = new DataMartSettings { ObjectName = "Lead", ViewName = "View", FieldMapping = "Mapping", TemplatedUploadId = [1] };
            _importServiceMock.Setup(x => x.IsContactImportTemplatesExist()).ReturnsAsync(true);
            var data = new List<object> { new object(), new object() };
            _dataMartRepositoryMock.Setup(x => x.RetrieveEmployeeRegistrationData()).ReturnsAsync(data);
            _importServiceMock.Setup(x => x.ImportEmployeeRegistration(data)).ReturnsAsync(new ImportSummaryResult { Status = BatchStatus.Completed });

            var result = await _handler.Handle(new List<DataMartSettings> { config });

            Assert.AreEqual("Completed", result.Status);
            Assert.That(result.Messages, Does.Contain("Import completed."));
            Assert.AreEqual(2, result.TotalRecordsProcessed);
        }
    }
}