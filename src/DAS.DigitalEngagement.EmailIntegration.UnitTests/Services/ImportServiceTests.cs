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

        [TestCase("Error", false, 1, true)]
        [TestCase("Completed", true, 1, true)]
        [TestCase("Completed", false, 1, false)]
        public async Task ImportEmployeeRegistration_RetrievesRejectedRowsOnlyWhenRequired(string status, bool partial, int imported, bool shouldRetrieve)
        {
            ConfigureRejectedRowsImport(status, partial, imported);
            const string csvText = "Email,Custom\r\nrejected@example.com,\"quoted, value\"\r\n";
            _externalApiServiceMock.Setup(api => api.GetCsvAsync("ContactImports/InvalidRows?token=token%2B%2F%3D%20value"))
                .ReturnsAsync(csvText);

            var summary = await _sut.ImportEmployeeRegistration(new List<string> { "lead" });
            var batch = summary.BatchResults.Single();

            _externalApiServiceMock.Verify(api => api.GetCsvAsync(It.IsAny<string>()), shouldRetrieve ? Times.Once() : Times.Never());
            Assert.That(batch.RejectedRowsRetrievalError, Is.Null);
            Assert.That(batch.RejectedRows, Has.Count.EqualTo(shouldRetrieve ? 1 : 0));
            if (shouldRetrieve)
            {
                Assert.That(batch.RejectedRows[0]["Email"], Is.EqualTo("rejected@example.com"));
                Assert.That(batch.RejectedRows[0]["Custom"], Is.EqualTo("quoted, value"));
            }
        }

        [TestCase("")]
        [TestCase(" \r\n")]
        [TestCase("Email,Reason\r\n")]
        public async Task ImportEmployeeRegistration_EmptyRejectedRows_PreservesBatch(string csvText)
        {
            ConfigureRejectedRowsImport("Error", true, 1);
            _externalApiServiceMock.Setup(api => api.GetCsvAsync(It.IsAny<string>())).ReturnsAsync(csvText);

            var summary = await _sut.ImportEmployeeRegistration(new List<string> { "lead" });
            var batch = summary.BatchResults.Single();

            Assert.That(batch.Status, Is.EqualTo(BatchStatus.Completed));
            Assert.That(batch.RejectedRows, Is.Empty);
            Assert.That(batch.RejectedRowsRetrievalError, Is.Null);
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task ImportEmployeeRegistration_RejectedRowsFailure_DoesNotChangeBatchOrRetryVerification(bool parsingFailure)
        {
            ConfigureRejectedRowsImport("Error", true, 1);
            const string malformedCsv = "Email,Reason\r\nrejected@example.com,\"unclosed";
            var download = _externalApiServiceMock.Setup(api => api.GetCsvAsync(It.IsAny<string>()));
            if (parsingFailure) download.ReturnsAsync(malformedCsv);
            else download.ThrowsAsync(new InvalidOperationException("private contact data"));

            var summary = await _sut.ImportEmployeeRegistration(new List<string> { "lead" });
            var batch = summary.BatchResults.Single();

            Assert.That(batch.Status, Is.EqualTo(BatchStatus.Completed));
            Assert.That(batch.Error, Is.Null);
            Assert.That(batch.RecordsProcessed, Is.EqualTo(1));
            Assert.That(batch.RejectedRowsRetrievalError, Does.StartWith("Failed to retrieve or parse rejected rows"));
            Assert.That(batch.RejectedRowsRetrievalError, Does.Not.Contain("private contact data"));
            Assert.That(batch.RejectedRows, Is.Empty);
            _externalApiServiceMock.Verify(api => api.GetDataAsync(It.IsAny<string>()), Times.Once);
            _loggerMock.Verify(logger => logger.Log(
                It.IsAny<LogLevel>(), It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("rejected@example.com") || state.ToString().Contains("private contact data")),
                It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Never);
        }

        private void ConfigureRejectedRowsImport(string status, bool partial, int imported)
        {
            _settings[0].TemplatedUploadId = new[] { 100 };
            _chunkingServiceMock.Setup(service => service.GetChunks(It.IsAny<long>(), It.IsAny<IList<string>>()))
                .Returns(new List<IList<string>> { new List<string> { "lead" } });
            _payloadMapperMock.Setup(mapper => mapper.MapToPayload(It.IsAny<IList<string>>(), "Lead"))
                .Returns(new List<ExpandoObject>());
            _csvServiceMock.Setup(service => service.ParseRejectedRows(It.IsAny<string>()))
                .Returns((string csvText) => new CsvService().ParseRejectedRows(csvText));
            _externalApiServiceMock.Setup(api => api.PostDataAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new BatchResultDetail { Status = BatchStatus.InProgress, TokenFromEshot = "{\"Token\":\"token+/= value\"}" });
            var response = System.Text.Json.JsonSerializer.Serialize(new
            {
                value = new[] { new { ImportStatus = status, ContactsReceived = 1, ContactsImported = imported, IsPartiallyImport = partial } }
            });
            _externalApiServiceMock.Setup(api => api.GetDataAsync(It.IsAny<string>())).ReturnsAsync(response);
        }

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