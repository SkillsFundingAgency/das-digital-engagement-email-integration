using DAS.DigitalEngagement.Application.Services;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.Models.Import;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace DAS.DigitalEngagement.EmailIntegration.UnitTests.Services
{
    [TestFixture]
    public class ImportServiceTests
    {
        private Mock<IExternalApiService> _externalApiServiceMock;
        private Mock<ILogger<ImportService>> _loggerMock;
        private Mock<IPayLoadMapper> _payLoadMapperMock;
        private Mock<IChunkingService> _chunkingServiceMock;
        private Mock<ICsvService> _csvServiceMock;
        private IList<DataMartSettings> _dataMartSettings;
        private ImportService _service;

            [SetUp]
            public void SetUp()
        {
            _externalApiServiceMock = new Mock<IExternalApiService>();
            _loggerMock = new Mock<ILogger<ImportService>>();
            _payLoadMapperMock = new Mock<IPayLoadMapper>();
            _chunkingServiceMock = new Mock<IChunkingService>();
            _csvServiceMock = new Mock<ICsvService>();
            _dataMartSettings = new List<DataMartSettings>
            {
                new DataMartSettings
                {
                    ObjectName = "Lead",
                    TemplatedUploadId = 123,
                    ViewName = "LeadView",
                    FieldMapping = "DefaultFieldMapping"
                }
            };

            _service = new ImportService(
                _externalApiServiceMock.Object,
                _loggerMock.Object,
                _payLoadMapperMock.Object,
                _chunkingServiceMock.Object,
                _csvServiceMock.Object,
                _dataMartSettings
            );
        }

        [Test]
        public async Task IsContactImportTemplatesExist_ReturnsTrue_WhenTemplateExists()
        {
            var json = "{\"value\":[{\"id\":123}]}";
            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(json);

            var result = await _service.IsContactImportTemplatesExist();

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task IsContactImportTemplatesExist_ReturnsFalse_WhenTemplateDoesNotExist()
        {
            var json = "{\"value\":[]}";
            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(json);

            var result = await _service.IsContactImportTemplatesExist();

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task ImportEmployeeRegistration_ReturnsCompleted_WhenAllBatchesCompleted()
        {
            var leads = new List<string> { "lead1", "lead2" };
            var chunkedLeads = new List<IList<string>> { leads };
            var payload = new List<ExpandoObject>
            {
                new ExpandoObject(),
                new ExpandoObject()
            };
            var csvString = "csv";
            var batchResult = new BatchResultDetail { Status = "Completed", TokenFromEshot = "{\"Token\":\"abc\"}" };

            _csvServiceMock.Setup(x => x.GetByteCount(It.IsAny<IList<string>>())).Returns(10);
            _chunkingServiceMock.Setup(x => x.GetChunks(It.IsAny<int>(), It.IsAny<IList<string>>())).Returns(chunkedLeads);
            _payLoadMapperMock.Setup(x => x.MapToPayload(It.IsAny<IList<string>>(), It.IsAny<string>())).Returns(payload);
            _csvServiceMock.Setup(x => x.ToCsv(It.IsAny<IList<ExpandoObject>>())).Returns(csvString);
            _externalApiServiceMock.Setup(x => x.PostDataAsync(It.IsAny<string>(), csvString))
                .ReturnsAsync(batchResult);

            // Mock the API call inside VerifyContactImport
            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.Is<string>(s => s.Contains("ContactImports"))))
                .ReturnsAsync("{\"value\":[{\"ContactsImported\":2,\"ImportStatus\":\"Completed\",\"AdditionalInfo\":null}]}");

            var summary = await _service.ImportEmployeeRegistration(leads);

            Assert.That(summary.Status, Is.EqualTo("Completed"));
            Assert.That(summary.TotalRecordsFromDb, Is.EqualTo(leads.Count));
            Assert.That(summary.Messages, Does.Contain("Import completed."));
            Assert.That(summary.BatchResults.All(b => b.Status == "Completed"));
        }

        [Test]
        public async Task ImportEmployeeRegistration_ReturnsFailed_WhenExceptionThrown()
        {
            var leads = new List<string> { "lead1" };
            var csvString = "csv";

            _csvServiceMock.Setup(x => x.GetByteCount(It.IsAny<IList<string>>())).Throws(new Exception("Test exception"));
            _externalApiServiceMock.SetupSequence(x => x.PostDataAsync(It.IsAny<string>(), csvString))
               .ReturnsAsync(new BatchResultDetail { Status = "Completed" })
               .ReturnsAsync(new BatchResultDetail { Status = "Failed" });

            var summary = await _service.ImportEmployeeRegistration(leads);

            Assert.That(summary.Status, Is.EqualTo("Failed"));
            Assert.That(summary.Messages.First(), Does.Contain("Import failed: Test exception"));
            Assert.That(summary.Messages.Last(), Does.Contain("Import completed."));
        }

        [Test]
        public async Task ImportEmployeeRegistration_ReturnsPartial_WhenOneBatchFailed()
        {
            var leads = new List<string> { "lead1", "lead2" };
            var chunkedLeads = new List<IList<string>>
    {
        new List<string> { "lead1" },
        new List<string> { "lead2" }
    };
            var payload = new List<ExpandoObject> { new ExpandoObject() };
            var csvString = "csv,t";

            _csvServiceMock.Setup(x => x.GetByteCount(It.IsAny<IList<string>>())).Returns(10);
            _chunkingServiceMock.Setup(x => x.GetChunks<string>(It.IsAny<long>(), It.IsAny<IList<string>>()))
                .Returns(chunkedLeads);
            _payLoadMapperMock.Setup(x => x.MapToPayload(It.IsAny<IList<string>>(), It.IsAny<string>())).Returns(payload);
            _csvServiceMock.Setup(x => x.ToCsv(It.IsAny<IList<ExpandoObject>>())).Returns(csvString);

            // Mock PostDataAsync for each batch
            _externalApiServiceMock.SetupSequence(x => x.PostDataAsync(It.IsAny<string>(), csvString))
                .ReturnsAsync(new BatchResultDetail { Status = "Completed", TokenFromEshot = "{\"Token\":\"abc\"}" })
                .ReturnsAsync(new BatchResultDetail { Status = "Failed", TokenFromEshot = "{\"Token\":\"def\"}" });

            // Mock GetDataAsync for VerifyContactImport for both batches
            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.Is<string>(s => s.Contains("ContactImports") && s.Contains("abc"))))
                .ReturnsAsync("{\"value\":[{\"ContactsImported\":1,\"ImportStatus\":\"Completed\",\"AdditionalInfo\":null}]}");
            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.Is<string>(s => s.Contains("ContactImports") && s.Contains("def"))))
                .ReturnsAsync("{\"value\":[{\"ContactsImported\":1,\"ImportStatus\":\"Error\",\"AdditionalInfo\":\"Some error\"}]}");

            var summary = await _service.ImportEmployeeRegistration(leads);

            Assert.That(summary.Status, Is.EqualTo("Partial"));
            Assert.That(summary.BatchResults.Any(b => b.Status == "Failed"));
            Assert.That(summary.Messages, Does.Contain("Import completed."));
        }

        [Test]
        public void GetDataMartConfig_ReturnsConfig_WhenObjectNameExists()
        {
            var config = _service.GetType()
                .GetMethod("GetDataMartConfig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(_service, new object[] { "Lead" }) as DataMartSettings;

            Assert.That(config, Is.Not.Null);
            Assert.That(config.ObjectName, Is.EqualTo("Lead"));
        }

        [Test]
        public void GetDataMartConfig_ThrowsException_WhenObjectNameMissing()
        {
            var ex = Assert.Throws<TargetInvocationException>(() =>
                _service.GetType()
                    .GetMethod("GetDataMartConfig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .Invoke(_service, new object[] { "Missing" }));

            Assert.That(ex.InnerException.Message, Does.Contain("Employee registration config is missing"));
        }

        [Test]
        public async Task ImportEmployeeRegistration_ReturnsPartial_WhenAnyBatchNotCompleted()
        {
            var leads = new List<string> { "lead1", "lead2", "lead3", "lead4" };
            var chunkedLeads = new List<IList<string>>
                    {
                        new List<string> { "lead1", "lead2" },
                        new List<string> { "lead3", "lead4" }
                    };
            var payload = new List<ExpandoObject> { new ExpandoObject() };
            var csvString = "csv";

            _csvServiceMock.Setup(x => x.GetByteCount(It.IsAny<IList<string>>())).Returns(1);
            _chunkingServiceMock.Setup(x => x.GetChunks<string>(It.IsAny<long>(), It.IsAny<IList<string>>()))
                .Returns(chunkedLeads);
            _payLoadMapperMock.Setup(x => x.MapToPayload(It.IsAny<IList<string>>(), It.IsAny<string>())).Returns(payload);
            _csvServiceMock.Setup(x => x.ToCsv<ExpandoObject>(It.IsAny<IList<ExpandoObject>>())).Returns(csvString);

            // Mock PostDataAsync for each batch
            _externalApiServiceMock.SetupSequence(x => x.PostDataAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new BatchResultDetail { Status = "Completed", TokenFromEshot = "{\"Token\":\"abc\"}" })
                .ReturnsAsync(new BatchResultDetail { Status = "Failed", TokenFromEshot = "{\"Token\":\"def\"}" });

            // Mock GetDataAsync for VerifyContactImport for both batches
            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.Is<string>(s => s.Contains("ContactImports") && s.Contains("abc"))))
                .ReturnsAsync("{\"value\":[{\"ContactsImported\":2,\"ImportStatus\":\"Completed\",\"AdditionalInfo\":null}]}");
            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.Is<string>(s => s.Contains("ContactImports") && s.Contains("def"))))
                .ReturnsAsync("{\"value\":[{\"ContactsImported\":2,\"ImportStatus\":\"Error\",\"AdditionalInfo\":\"Some error\"}]}");

            var summary = await _service.ImportEmployeeRegistration(leads);

            Assert.That(summary.Status, Is.EqualTo("Partial"));
            Assert.That(summary.BatchResults.Count, Is.EqualTo(2));
            Assert.That(summary.Messages, Does.Contain("Import completed."));
        }

        [Test]
        public async Task ImportEmployeeRegistration_HandlesNullLeads()
        {
            // Arrange
            var chunkedLeads = new List<List<string>>();
            var payload = new List<ExpandoObject> { new ExpandoObject() };
            var csvString = "csv";
          
            _csvServiceMock.Setup(x => x.GetByteCount(It.IsAny<IList<string>>())).Returns(0);
            _chunkingServiceMock.Setup(x => x.GetChunks(It.IsAny<int>(), It.IsAny<IList<string>>()))
                .Returns(chunkedLeads);
            _payLoadMapperMock.Setup(x => x.MapToPayload(It.IsAny<IList<string>>(), It.IsAny<string>()))
                .Returns(payload);
            _csvServiceMock.Setup(x => x.ToCsv(It.IsAny<List<string>>())).Returns(csvString);
            _csvServiceMock.Setup(x => x.GenerateStreamFromString(It.IsAny<string>())).Returns(new System.IO.MemoryStream());
            _externalApiServiceMock.SetupSequence(x => x.PostDataAsync(It.IsAny<string>(), csvString))
           .ReturnsAsync(new BatchResultDetail { Status = "Completed" })
           .ReturnsAsync(new BatchResultDetail { Status = "Failed" });

            // Act
            var summary = await _service.ImportEmployeeRegistration<string>(null);

            // Assert
            Assert.That(summary.TotalRecordsFromDb, Is.EqualTo(0));
            Assert.That(summary.Status, Is.EqualTo("Completed"));
            Assert.That(summary.Messages, Does.Contain("Import completed."));
        }
    }
}