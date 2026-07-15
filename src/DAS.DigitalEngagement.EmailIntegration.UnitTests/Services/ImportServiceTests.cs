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
using System.Threading.Tasks;

namespace DAS.DigitalEngagement.EmailIntegration.UnitTests.Services
{
    [TestFixture]
    public class ImportServiceTests
    {
        private Mock<IExternalApiService> _externalApiServiceMock;
        private Mock<ILogger<ImportService>> _loggerMock;
        private Mock<IPayLoadMapper> _payloadMapperMock;
        private Mock<IChunkingService> _chunkingServiceMock;
        private Mock<ICsvService> _csvServiceMock;

        private ImportService _sut;

        private List<DataMartSettings> _settings;
        private EmailMarketingApi _emailMarketingApi;
        private static readonly int[] TemplatedUploadIds = new[] { 100,200 };

        [SetUp]
        public void Setup()
        {
            _externalApiServiceMock = new Mock<IExternalApiService>();
            _loggerMock = new Mock<ILogger<ImportService>>();
            _payloadMapperMock = new Mock<IPayLoadMapper>();
            _chunkingServiceMock = new Mock<IChunkingService>();
            _csvServiceMock = new Mock<ICsvService>();

            _settings = new List<DataMartSettings>
            {
                new DataMartSettings
                {
                    ObjectName = "Lead",
                    TemplatedUploadId = TemplatedUploadIds,
                    FieldMapping = "field-map",
                    ViewName = "LeadView"
                }
            };

            _emailMarketingApi = new EmailMarketingApi
            {
                ApiRetryCount = 1,
                ApiBaseUrl = "https://dummy-url",
                ApiKey = "dummy-key",
                ChunkSizeKB = 100
            };

            _sut = new ImportService(
                _externalApiServiceMock.Object,
                _loggerMock.Object,
                _payloadMapperMock.Object,
                _chunkingServiceMock.Object,
                _csvServiceMock.Object,
                _settings,
                _emailMarketingApi);
        }

        [Test]
        public async Task IsContactImportTemplatesExist_ReturnsTrue_WhenAllTemplatesExist()
        {
            // Arrange
            _externalApiServiceMock
                .SetupSequence(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync("""{"value":[{"id":1}]}""")
                .ReturnsAsync("""{"value":[{"id":2}]}""");

            // Act
            var result = await _sut.IsContactImportTemplatesExist();

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task IsContactImportTemplatesExist_ReturnsFalse_WhenTemplateMissing()
        {
            // Arrange
            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync("""{"value":[]}""");

            // Act
            var result = await _sut.IsContactImportTemplatesExist();

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsContactImportTemplatesExist_Throws_WhenConfigMissing()
        {
            // Arrange
            var sut = new ImportService(
                _externalApiServiceMock.Object,
                _loggerMock.Object,
                _payloadMapperMock.Object,
                _chunkingServiceMock.Object,
                _csvServiceMock.Object,
                new List<DataMartSettings>(),
                _emailMarketingApi);

            // Act / Assert
            Assert.ThrowsAsync<InvalidOperationException>(
                async () => await sut.IsContactImportTemplatesExist());
        }

        [Test]
        public async Task ImportEmployeeRegistration_CompletesSuccessfully()
        {
            // Arrange
            var leads = new List<string> { "lead1", "lead2" };

            var chunks = new List<IList<string>>
            {
                new List<string> { "lead1", "lead2" }
            };

            _csvServiceMock
                .Setup(x => x.GetByteCount(It.IsAny<IList<string>>()))
                .Returns(100);

            _chunkingServiceMock
                .Setup(x => x.GetChunks(100L, It.IsAny<IList<string>>()))
                .Returns(chunks);

            _payloadMapperMock
                .Setup(x => x.MapToPayload(It.IsAny<IList<string>>(), "Lead"))
                .Returns(new List<ExpandoObject> { new System.Dynamic.ExpandoObject(), new System.Dynamic.ExpandoObject() });

            _csvServiceMock
                .Setup(x => x.ToCsv(It.IsAny<List<ExpandoObject>>()))
                .Returns("csv");

            _externalApiServiceMock
                .Setup(x => x.PostDataAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new BatchResultDetail
                {
                    Status = BatchStatus.Completed,
                    TokenFromEshot = "abc-token"
                });

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.Is<string>(s => s.Contains("ContactImports?$filter"))))
                .ReturnsAsync("""
                {
                    "value":[
                        {
                            "ImportStatus":"Completed",
                            "ContactsReceived":10,
                            "ContactsImported":10,
                            "IsPartiallyImport":false,
                            "AdditionalInfo":"ok"
                        }
                    ]
                }
                """);

            // Act
            var result = await _sut.ImportEmployeeRegistration(leads);

            // Assert
            Assert.That(result.Status, Is.EqualTo(BatchStatus.Completed));
            // 2 template IDs * 1 chunk each = 2 batches
            Assert.That(result.BatchResults.Count, Is.EqualTo(2));
            Assert.That(result.Messages.Any(m => m.Contains("Import completed.")));
            Assert.That(result.BatchResults.All(x => x.Status == BatchStatus.Completed));
        }

        [Test]
        public async Task ImportEmployeeRegistration_HandlesPartialImport()
        {
            // Arrange
            var leads = new List<string> { "lead1" };

            var chunks = new List<IList<string>>
            {
                new List<string> { "lead1" }
            };

            _csvServiceMock
                .Setup(x => x.GetByteCount(It.IsAny<IList<string>>()))
                .Returns(10);

            _chunkingServiceMock
                .Setup(x => x.GetChunks(10L, It.IsAny<IList<string>>()))
                .Returns(chunks);

            _payloadMapperMock
                .Setup(x => x.MapToPayload(It.IsAny<IList<string>>(), It.IsAny<string>()))
                .Returns(new List<ExpandoObject> { new System.Dynamic.ExpandoObject() });

            _csvServiceMock
                .Setup(x => x.ToCsv(It.IsAny<List<ExpandoObject>>()))
                .Returns("csv");

            _externalApiServiceMock
                .Setup(x => x.PostDataAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new BatchResultDetail
                {
                    Status = BatchStatus.Completed,
                    TokenFromEshot = "abc"
                });

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.Is<string>(x => x.Contains("ContactImports?$filter"))))
                .ReturnsAsync("""
                {
                    "value":[
                        {
                            "ImportStatus":"Error",
                            "ContactsReceived":10,
                            "ContactsImported":5,
                            "IsPartiallyImport":true,
                            "AdditionalInfo":"5 failed"
                        }
                    ]
                }
                """);

            // Act
            var result = await _sut.ImportEmployeeRegistration(leads);

            // Assert
            Assert.That(result.Status, Is.EqualTo(BatchStatus.Completed));
            // 2 templates, both should have partial imports
            Assert.That(result.BatchResults.Count, Is.EqualTo(2));
            Assert.That(result.BatchResults.All(x => x.Status == BatchStatus.Completed));
            Assert.That(result.BatchResults.All(x => x.IsPartiallyImported == true));
            Assert.That(result.BatchResults.All(x => x.RecordsFailed == 5));
        }

        [Test]
        public async Task ImportEmployeeRegistration_Fails_WhenNoTokenReturned()
        {
            // Arrange
            var leads = new List<string> { "lead1" };

            var chunks = new List<IList<string>>
            {
                new List<string> { "lead1" }
            };

            _csvServiceMock
                .Setup(x => x.GetByteCount(It.IsAny<IList<string>>()))
                .Returns(1);

            _chunkingServiceMock
                .Setup(x => x.GetChunks(1L, It.IsAny<IList<string>>()))
                .Returns(chunks);

            _payloadMapperMock
                .Setup(x => x.MapToPayload(It.IsAny<IList<string>>(), It.IsAny<string>()))
                .Returns(new List<ExpandoObject> { new System.Dynamic.ExpandoObject() });

            _csvServiceMock
                .Setup(x => x.ToCsv(It.IsAny<List<ExpandoObject>>()))
                .Returns("csv");

            _externalApiServiceMock
                .Setup(x => x.PostDataAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new BatchResultDetail
                {
                    Status = BatchStatus.Failed,
                    TokenFromEshot = null
                });

            // Act
            var result = await _sut.ImportEmployeeRegistration(leads);

            // Assert
            Assert.That(result.Status, Is.EqualTo(BatchStatus.Partial));
            // 2 templates, both should fail
            Assert.That(result.BatchResults.Count, Is.EqualTo(2));
            Assert.That(result.BatchResults.All(x => x.Status == BatchStatus.Failed));
            Assert.That(result.BatchResults.All(x => x.Error == "No token received from external API"));
        }

        [Test]
        public async Task ImportEmployeeRegistration_Fails_WhenVerificationReturnsNoData()
        {
            // Arrange
            var leads = new List<string> { "lead1" };

            var chunks = new List<IList<string>>
            {
                new List<string> { "lead1" }
            };

            _csvServiceMock
                .Setup(x => x.GetByteCount(It.IsAny<IList<string>>()))
                .Returns(1);

            _chunkingServiceMock
                .Setup(x => x.GetChunks(1L, It.IsAny<IList<string>>()))
                .Returns(chunks);

            _payloadMapperMock
                .Setup(x => x.MapToPayload(It.IsAny<IList<string>>(), It.IsAny<string>()))
                .Returns(new List<ExpandoObject> { new System.Dynamic.ExpandoObject() });

            _csvServiceMock
                .Setup(x => x.ToCsv(It.IsAny<List<ExpandoObject>>()))
                .Returns("csv");

            _externalApiServiceMock
                .Setup(x => x.PostDataAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new BatchResultDetail
                {
                    Status = BatchStatus.Completed,
                    TokenFromEshot = "abc"
                });

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.Is<string>(x => x.Contains("ContactImports?$filter"))))
                .ReturnsAsync("""{"value":[]}""");

            // Act
            var result = await _sut.ImportEmployeeRegistration(leads);

            // Assert
            Assert.That(result.BatchResults.Count, Is.EqualTo(2)); // 2 templates
            Assert.That(result.BatchResults.All(x => x.Status == BatchStatus.Failed));
            Assert.That(result.BatchResults.All(x => x.Error.Contains("No import status found")));
        }

        [Test]
        public async Task ImportEmployeeRegistration_Fails_WhenVerificationThrows()
        {
            // Arrange
            var leads = new List<string> { "lead1" };

            var chunks = new List<IList<string>>
            {
                new List<string> { "lead1" }
            };

            _csvServiceMock
                .Setup(x => x.GetByteCount(It.IsAny<IList<string>>()))
                .Returns(1);

            _chunkingServiceMock
                .Setup(x => x.GetChunks(1L, It.IsAny<IList<string>>()))
                .Returns(chunks);

            _payloadMapperMock
                .Setup(x => x.MapToPayload(It.IsAny<IList<string>>(), It.IsAny<string>()))
                .Returns(new List<ExpandoObject> { new System.Dynamic.ExpandoObject() });

            _csvServiceMock
                .Setup(x => x.ToCsv(It.IsAny<List<ExpandoObject>>()))
                .Returns("csv");

            _externalApiServiceMock
                .Setup(x => x.PostDataAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new BatchResultDetail
                {
                    Status = BatchStatus.Completed,
                    TokenFromEshot = "abc"
                });

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.Is<string>(x => x.Contains("ContactImports?$filter"))))
                .ThrowsAsync(new Exception("verification error"));

            // Act
            var result = await _sut.ImportEmployeeRegistration(leads);

            // Assert
            Assert.That(result.BatchResults.Count, Is.EqualTo(2)); // 2 templates
            Assert.That(result.BatchResults.All(x => x.Status == BatchStatus.Failed));
            Assert.That(result.BatchResults.All(x => x.Error.Contains("Failed to verify import status")));
        }

        [Test]
        public async Task ImportEmployeeRegistration_HandlesWaitingStatus()
        {
            // Arrange
            var leads = new List<string> { "lead1" };

            var chunks = new List<IList<string>>
            {
                new List<string> { "lead1" }
            };

            _csvServiceMock
                .Setup(x => x.GetByteCount(It.IsAny<IList<string>>()))
                .Returns(1);

            _chunkingServiceMock
                .Setup(x => x.GetChunks(1L, It.IsAny<IList<string>>()))
                .Returns(chunks);

            _payloadMapperMock
                .Setup(x => x.MapToPayload(It.IsAny<IList<string>>(), It.IsAny<string>()))
                .Returns(new List<ExpandoObject> { new System.Dynamic.ExpandoObject() });

            _csvServiceMock
                .Setup(x => x.ToCsv(It.IsAny<List<ExpandoObject>>()))
                .Returns("csv");

            _externalApiServiceMock
                .Setup(x => x.PostDataAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new BatchResultDetail
                {
                    Status = BatchStatus.Completed,
                    TokenFromEshot = "abc"
                });

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.Is<string>(x => x.Contains("ContactImports?$filter"))))
                .ReturnsAsync("""
                {
                    "value":[
                        {
                            "ImportStatus":"Waiting",
                            "ContactsReceived":5,
                            "ContactsImported":0,
                            "IsPartiallyImport":false
                        }
                    ]
                }
                """);

            // Act
            var result = await _sut.ImportEmployeeRegistration(leads);

            // Assert - After retry exhaustion with Waiting status, it should still complete
            Assert.That(result.BatchResults.Count, Is.EqualTo(2)); // 2 templates
            Assert.That(result.BatchResults.All(x => x.Status == BatchStatus.Completed));
        }

        [Test]
        public async Task ImportEmployeeRegistration_ExtractsTokenFromJson()
        {
            // Arrange
            var leads = new List<string> { "lead1" };

            var chunks = new List<IList<string>>
            {
                new List<string> { "lead1" }
            };

            _csvServiceMock
                .Setup(x => x.GetByteCount(It.IsAny<IList<string>>()))
                .Returns(1);

            _chunkingServiceMock
                .Setup(x => x.GetChunks(1L, It.IsAny<IList<string>>()))
                .Returns(chunks);

            _payloadMapperMock
                .Setup(x => x.MapToPayload(It.IsAny<IList<string>>(), It.IsAny<string>()))
                .Returns(new List<ExpandoObject> { new System.Dynamic.ExpandoObject() });

            _csvServiceMock
                .Setup(x => x.ToCsv(It.IsAny<List<ExpandoObject>>()))
                .Returns("csv");

            _externalApiServiceMock
                .Setup(x => x.PostDataAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new BatchResultDetail
                {
                    Status = BatchStatus.Completed,
                    TokenFromEshot = """{"Token":"real-token"}"""
                });

            _externalApiServiceMock
                .Setup(x => x.GetDataAsync(It.Is<string>(x => x.Contains("ContactImports?$filter"))))
                .ReturnsAsync("""
                {
                    "value":[
                        {
                            "ImportStatus":"Completed",
                            "ContactsReceived":1,
                            "ContactsImported":1,
                            "IsPartiallyImport":false
                        }
                    ]
                }
                """);

            // Act
            var result = await _sut.ImportEmployeeRegistration(leads);

            // Assert
            Assert.That(result.BatchResults.Count, Is.EqualTo(2)); // 2 templates
            Assert.That(result.BatchResults.All(x => x.TokenFromEshot == "real-token"));
        }

        [Test]
        public async Task ImportEmployeeRegistration_HandlesException()
        {
            // Arrange
            _csvServiceMock
                .Setup(x => x.GetByteCount(It.IsAny<IList<string>>()))
                .Throws(new Exception("boom"));

            // Act
            var result = await _sut.ImportEmployeeRegistration(new List<string>());

            // Assert
            Assert.That(result.Status, Is.EqualTo(BatchStatus.Failed));
            Assert.That(result.Messages.Any(x => x.Contains("boom")));
        }

        [Test]
        public async Task ImportEmployeeRegistration_HandlesNullLeads()
        {
            // Arrange
            _csvServiceMock
                .Setup(x => x.GetByteCount(It.IsAny<IList<string>>()))
                .Returns(0);

            _chunkingServiceMock
                .Setup(x => x.GetChunks(0L, It.IsAny<IList<string>>()))
                .Returns(new List<IList<string>>());

            // Act
            var result = await _sut.ImportEmployeeRegistration<string>(null);

            // Assert
            Assert.That(result.TotalRecordsFromDb, Is.EqualTo(0));
            Assert.That(result.Status, Is.EqualTo(BatchStatus.Completed));
        }
    }
}