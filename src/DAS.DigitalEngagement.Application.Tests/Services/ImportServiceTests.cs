using DAS.DigitalEngagement.Application.Services;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.Models.Import;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Dynamic;

namespace DAS.DigitalEngagement.Application.Tests.Services
{
    [TestFixture]
    public class ImportServiceTests
    {
        private Mock<ILogger<ImportService>> _mockLogger;
        private Mock<IExternalApiService> _mockExternalApiService;
        private Mock<IPayLoadMapper> _mockPayLoadMapper;
        private Mock<IChunkingService> _mockChunkingService;
        private Mock<ICsvService> _mockCsvService;
        private IList<DataMartSettings> _dataMartSettings;
        private EmailMarketingApi _emailMarketingApi;

        [SetUp]
        public void SetUp()
        {
            _mockLogger = new Mock<ILogger<ImportService>>();
            _mockExternalApiService = new Mock<IExternalApiService>();
            _mockPayLoadMapper = new Mock<IPayLoadMapper>();
            _mockChunkingService = new Mock<IChunkingService>();
            _mockCsvService = new Mock<ICsvService>();
            
            _dataMartSettings = new List<DataMartSettings>
            {
                new DataMartSettings
                {
                    ViewName = "LeadView",
                    ObjectName = "Lead",
                    FieldMapping = "{\"Source\":\"Email\",\"Target\":\"EmailAddress\"}",
                    TemplatedUploadId = new[] { 123, 456 }
                }
            };
            
            _emailMarketingApi = new EmailMarketingApi
            {
                ApiBaseUrl = "https://api.test.com",
                ApiKey = "test-key",
                ApiRetryCount = 3,
                ChunkSizeKB = 10240,
                PageSize = 5000,
                ImportWindowDays = 7
            };
        }

        private ImportService CreateService()
        {
            return new ImportService(
                _mockExternalApiService.Object,
                _mockLogger.Object,
                _mockPayLoadMapper.Object,
                _mockChunkingService.Object,
                _mockCsvService.Object,
                _dataMartSettings,
                _emailMarketingApi);
        }

        #region IsContactImportTemplatesExist Tests

        [Test]
        public async Task IsContactImportTemplatesExist_AllTemplatesExist_ReturnsTrue()
        {
            // Arrange
            var service = CreateService();
            var responseJson = "{\"value\":[{\"ID\":123}]}";
            
            _mockExternalApiService
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(responseJson);

            // Act
            var result = await service.IsContactImportTemplatesExist();

            // Assert
            Assert.That(result, Is.True);
            _mockExternalApiService.Verify(x => x.GetDataAsync(It.IsAny<string>()), Times.Exactly(2));
        }

        [Test]
        public async Task IsContactImportTemplatesExist_TemplateNotFound_ReturnsFalse()
        {
            // Arrange
            var service = CreateService();
            var emptyResponseJson = "{\"value\":[]}";
            
            _mockExternalApiService
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(emptyResponseJson);

            // Act
            var result = await service.IsContactImportTemplatesExist();

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task IsContactImportTemplatesExist_ResponseWithMultipleTemplates_ReturnsTrue()
        {
            // Arrange
            var service = CreateService();
            var responseJson = "{\"value\":[{\"ID\":123},{\"ID\":124}]}";
            
            _mockExternalApiService
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(responseJson);

            // Act
            var result = await service.IsContactImportTemplatesExist();

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task IsContactImportTemplatesExist_ResponseWithNullValue_ReturnsFalse()
        {
            // Arrange
            var service = CreateService();
            var responseJson = "{\"value\":null}";
            
            _mockExternalApiService
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(responseJson);

            // Act
            var result = await service.IsContactImportTemplatesExist();

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task IsContactImportTemplatesExist_ResponseWithoutValueProperty_ReturnsFalse()
        {
            // Arrange
            var service = CreateService();
            var responseJson = "{\"data\":[{\"ID\":123}]}";
            
            _mockExternalApiService
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(responseJson);

            // Act
            var result = await service.IsContactImportTemplatesExist();

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task IsContactImportTemplatesExist_EmptyJsonObject_ReturnsFalse()
        {
            // Arrange
            var service = CreateService();
            var responseJson = "{}";
            
            _mockExternalApiService
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(responseJson);

            // Act
            var result = await service.IsContactImportTemplatesExist();

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task IsContactImportTemplatesExist_ValueIsNotArray_ReturnsFalse()
        {
            // Arrange
            var service = CreateService();
            var responseJson = "{\"value\":\"not an array\"}";
            
            _mockExternalApiService
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(responseJson);

            // Act
            var result = await service.IsContactImportTemplatesExist();

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task IsContactImportTemplatesExist_ValueIsEmptyObject_ReturnsFalse()
        {
            // Arrange
            var service = CreateService();
            var responseJson = "{\"value\":{}}";
            
            _mockExternalApiService
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(responseJson);

            // Act
            var result = await service.IsContactImportTemplatesExist();

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsContactImportTemplatesExist_InvalidJsonFormat_ThrowsJsonException()
        {
            // Arrange
            var service = CreateService();
            
            _mockExternalApiService
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync("{invalid json format");

            // Act & Assert
            Assert.ThrowsAsync<System.Text.Json.JsonException>(() => 
                service.IsContactImportTemplatesExist());
        }

        [Test]
        public async Task IsContactImportTemplatesExist_FirstTemplateHasCountGreaterThanOne_ReturnsTrue()
        {
            // Arrange
            var service = CreateService();
            var responseJson = "{\"value\":[{\"ID\":123},{\"ID\":123}]}"; // Duplicate IDs
            
            _mockExternalApiService
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(responseJson);

            // Act
            var result = await service.IsContactImportTemplatesExist();

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task IsContactImportTemplatesExist_SecondTemplateCheckFails_LogsWarningAndReturnsFalse()
        {
            // Arrange
            var service = CreateService();
            
            _mockExternalApiService
                .SetupSequence(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync("{\"value\":[{\"ID\":123}]}")
                .ReturnsAsync("{\"value\":[]}");

            // Act
            var result = await service.IsContactImportTemplatesExist();

            // Assert
            Assert.That(result, Is.False);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Template ID 456 not found")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Test]
        public async Task IsContactImportTemplatesExist_BothTemplatesExistWithDifferentResponses_ReturnsTrue()
        {
            // Arrange
            var service = CreateService();
            
            _mockExternalApiService
                .SetupSequence(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync("{\"value\":[{\"ID\":123,\"Name\":\"Template1\"}]}")
                .ReturnsAsync("{\"value\":[{\"ID\":456,\"Name\":\"Template2\",\"Active\":true}]}");

            // Act
            var result = await service.IsContactImportTemplatesExist();

            // Assert
            Assert.That(result, Is.True);
            _mockExternalApiService.Verify(x => x.GetDataAsync(It.IsAny<string>()), Times.Exactly(2));
        }

        [Test]
        public async Task IsContactImportTemplatesExist_NullResponse_ReturnsFalse()
        {
            // Arrange
            var service = CreateService();
            
            _mockExternalApiService
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync("null");

            // Act
            var result = await service.IsContactImportTemplatesExist();

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsContactImportTemplatesExist_ThrowsException_PropagatesException()
        {
            // Arrange
            var service = CreateService();
            
            _mockExternalApiService
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException("API error"));

            // Act & Assert
            Assert.ThrowsAsync<HttpRequestException>(() => service.IsContactImportTemplatesExist());
        }

        #endregion

        #region ImportEmployeeRegistration Tests - Success Scenarios

        [Test]
        public async Task ImportEmployeeRegistration_WithValidData_ReturnsCompletedSummary()
        {
            // Arrange
            var service = CreateService();
            var leads = new List<TestLead> { new TestLead { Email = "test@example.com", Name = "Test User" } };
            var chunks = new List<IList<TestLead>> { leads };
            
            SetupSuccessfulImport(leads, chunks);

            // Act
            var result = await service.ImportEmployeeRegistration(leads);

            // Assert
            Assert.That(result.Status, Is.EqualTo("Completed"));
            Assert.That(result.TotalRecordsFromDb, Is.EqualTo(1));
            Assert.That(result.BatchResults.Count, Is.EqualTo(2)); // 2 templates
            Assert.That(result.Messages, Contains.Item("Import completed."));
            Assert.That(result.EndTime, Is.GreaterThan(result.StartTime));
        }

        [Test]
        public async Task ImportEmployeeRegistration_WithMultipleChunks_ProcessesAllChunks()
        {
            // Arrange
            var service = CreateService();
            var leads = new List<TestLead> 
            { 
                new TestLead { Email = "test1@example.com", Name = "User 1" },
                new TestLead { Email = "test2@example.com", Name = "User 2" }
            };
            var chunk1 = new List<TestLead> { leads[0] };
            var chunk2 = new List<TestLead> { leads[1] };
            var chunks = new List<IList<TestLead>> { chunk1, chunk2 };
            
            SetupSuccessfulImport(leads, chunks);

            // Act
            var result = await service.ImportEmployeeRegistration(leads);

            // Assert
            Assert.That(result.Status, Is.EqualTo("Completed"));
            Assert.That(result.BatchResults.Count, Is.EqualTo(4)); // 2 templates × 2 chunks
            _mockExternalApiService.Verify(x => x.PostDataAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(4));
        }

        [Test]
        public async Task ImportEmployeeRegistration_WithNullLeads_ReturnsCompletedWithZeroRecords()
        {
            // Arrange
            var service = CreateService();
            var emptyChunks = new List<IList<TestLead>>();
            
            _mockCsvService.Setup(x => x.GetByteCount(It.IsAny<List<TestLead>>())).Returns(0);
            _mockChunkingService.Setup(x => x.GetChunks(0, It.IsAny<List<TestLead>>())).Returns(emptyChunks);

            // Act
            var result = await service.ImportEmployeeRegistration<TestLead>(null);

            // Assert
            Assert.That(result.Status, Is.EqualTo("Completed"));
            Assert.That(result.TotalRecordsFromDb, Is.EqualTo(0));
            Assert.That(result.BatchResults.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task ImportEmployeeRegistration_WithEmptyLeads_ReturnsCompletedWithZeroRecords()
        {
            // Arrange
            var service = CreateService();
            var leads = new List<TestLead>();
            var emptyChunks = new List<IList<TestLead>>();
            
            _mockCsvService.Setup(x => x.GetByteCount(leads)).Returns(0);
            _mockChunkingService.Setup(x => x.GetChunks(0, leads)).Returns(emptyChunks);

            // Act
            var result = await service.ImportEmployeeRegistration(leads);

            // Assert
            Assert.That(result.Status, Is.EqualTo("Completed"));
            Assert.That(result.TotalRecordsFromDb, Is.EqualTo(0));
            Assert.That(result.BatchResults.Count, Is.EqualTo(0));
        }

        #endregion

        #region ImportEmployeeRegistration Tests - Partial Import Scenarios

        [Test]
        public async Task ImportEmployeeRegistration_WithPartialImport_ReturnsPartialStatus()
        {
            // Arrange
            var service = CreateService();
            var leads = new List<TestLead> { new TestLead { Email = "test@example.com", Name = "Test User" } };
            var chunks = new List<IList<TestLead>> { leads };
            
            SetupPartialImport(leads, chunks);

            // Act
            var result = await service.ImportEmployeeRegistration(leads);

            // Assert
            Assert.That(result.Status, Is.EqualTo("Partial"));
            Assert.That(result.BatchResults.Any(b => b.Status == "Completed"), Is.True);
            Assert.That(result.BatchResults.Any(b => b.IsPartiallyImported), Is.True);
        }

        [Test]
        public async Task ImportEmployeeRegistration_SomeBatchesFail_ReturnsPartialStatus()
        {
            // Arrange
            var service = CreateService();
            var leads = new List<TestLead> { new TestLead { Email = "test@example.com", Name = "Test User" } };
            var chunks = new List<IList<TestLead>> { leads };
            
            _mockCsvService.Setup(x => x.GetByteCount(leads)).Returns(1000);
            _mockChunkingService.Setup(x => x.GetChunks(1000, leads)).Returns(chunks);
            _mockPayLoadMapper.Setup(x => x.MapToPayload(It.IsAny<IList<TestLead>>(), "Lead"))
                .Returns(new List<ExpandoObject> { new ExpandoObject() });
            _mockCsvService.Setup(x => x.ToCsv(It.IsAny<IList<ExpandoObject>>())).Returns("csv,data");
            
            // First template succeeds, second fails
            _mockExternalApiService
                .SetupSequence(x => x.PostDataAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new BatchResultDetail
                {
                    Status = "Completed",
                    TokenFromEshot = "token1",
                    RecordsReceived = 1,
                    RecordsProcessed = 1
                })
                .ReturnsAsync(new BatchResultDetail
                {
                    Status = "Failed",
                    TokenFromEshot = "",
                    RecordsReceived = 1,
                    RecordsProcessed = 0
                });
            
            _mockExternalApiService
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync("{\"value\":[{\"ImportStatus\":\"Completed\",\"ContactsReceived\":1,\"ContactsImported\":1,\"IsPartiallyImport\":false}]}");

            // Act
            var result = await service.ImportEmployeeRegistration(leads);

            // Assert
            Assert.That(result.Status, Is.EqualTo("Partial"));
        }

        #endregion

        #region ImportEmployeeRegistration Tests - Error Scenarios

        [Test]
        public async Task ImportEmployeeRegistration_PostDataThrowsException_ReturnsFailedStatus()
        {
            // Arrange
            var service = CreateService();
            var leads = new List<TestLead> { new TestLead { Email = "test@example.com", Name = "Test User" } };
            var chunks = new List<IList<TestLead>> { leads };
            
            _mockCsvService.Setup(x => x.GetByteCount(leads)).Returns(1000);
            _mockChunkingService.Setup(x => x.GetChunks(1000, leads)).Returns(chunks);
            _mockPayLoadMapper.Setup(x => x.MapToPayload(It.IsAny<IList<TestLead>>(), "Lead"))
                .Returns(new List<ExpandoObject> { new ExpandoObject() });
            _mockCsvService.Setup(x => x.ToCsv(It.IsAny<IList<ExpandoObject>>())).Returns("csv,data");
            
            _mockExternalApiService
                .Setup(x => x.PostDataAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException("API is down"));

            // Act
            var result = await service.ImportEmployeeRegistration(leads);

            // Assert
            Assert.That(result.Status, Is.EqualTo("Failed"));
            Assert.That(result.Messages.Any(m => m.Contains("Import failed")), Is.True);
            Assert.That(result.Messages.Any(m => m.Contains("API is down")), Is.True);
        }

        [Test]
        public async Task ImportEmployeeRegistration_ConfigNotFound_ThrowsInvalidOperationException()
        {
            // Arrange
            var emptySettings = new List<DataMartSettings>();
            var service = new ImportService(
                _mockExternalApiService.Object,
                _mockLogger.Object,
                _mockPayLoadMapper.Object,
                _mockChunkingService.Object,
                _mockCsvService.Object,
                emptySettings,
                _emailMarketingApi);
            
            var leads = new List<TestLead> { new TestLead { Email = "test@example.com", Name = "Test User" } };

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(() => 
                service.ImportEmployeeRegistration(leads));
            Assert.That(ex.Message, Does.Contain("Employee registration config is missing"));
        }

        #endregion

        #region VerifyContactImport Tests - Token Extraction

        [Test]
        public async Task ImportEmployeeRegistration_TokenAsJsonString_ExtractsToken()
        {
            // Arrange
            var service = CreateService();
            var leads = new List<TestLead> { new TestLead { Email = "test@example.com", Name = "Test User" } };
            var chunks = new List<IList<TestLead>> { leads };
            
            _mockCsvService.Setup(x => x.GetByteCount(leads)).Returns(1000);
            _mockChunkingService.Setup(x => x.GetChunks(1000, leads)).Returns(chunks);
            _mockPayLoadMapper.Setup(x => x.MapToPayload(It.IsAny<IList<TestLead>>(), "Lead"))
                .Returns(new List<ExpandoObject> { new ExpandoObject() });
            _mockCsvService.Setup(x => x.ToCsv(It.IsAny<IList<ExpandoObject>>())).Returns("csv,data");
            
            var batchResult = new BatchResultDetail
            {
                Status = "Completed",
                TokenFromEshot = "{\"Token\":\"extracted-token-123\"}",
                RecordsReceived = 1,
                RecordsProcessed = 1
            };
            
            _mockExternalApiService
                .Setup(x => x.PostDataAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(batchResult);
            
            _mockExternalApiService
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync("{\"value\":[{\"ImportStatus\":\"Completed\",\"ContactsReceived\":1,\"ContactsImported\":1,\"IsPartiallyImport\":false}]}");

            // Act
            var result = await service.ImportEmployeeRegistration(leads);

            // Assert
            Assert.That(result.BatchResults[0].TokenFromEshot, Is.EqualTo("extracted-token-123"));
            _mockExternalApiService.Verify(x => x.GetDataAsync(It.Is<string>(s => s.Contains("extracted-token-123"))), Times.AtLeastOnce);
        }

        [Test]
        public async Task ImportEmployeeRegistration_TokenExtractionFails_UsesOriginalToken()
        {
            // Arrange
            var service = CreateService();
            var leads = new List<TestLead> { new TestLead { Email = "test@example.com", Name = "Test User" } };
            var chunks = new List<IList<TestLead>> { leads };
            
            _mockCsvService.Setup(x => x.GetByteCount(leads)).Returns(1000);
            _mockChunkingService.Setup(x => x.GetChunks(1000, leads)).Returns(chunks);
            _mockPayLoadMapper.Setup(x => x.MapToPayload(It.IsAny<IList<TestLead>>(), "Lead"))
                .Returns(new List<ExpandoObject> { new ExpandoObject() });
            _mockCsvService.Setup(x => x.ToCsv(It.IsAny<IList<ExpandoObject>>())).Returns("csv,data");
            
            var batchResult = new BatchResultDetail
            {
                Status = "Completed",
                TokenFromEshot = "{invalid json}",
                RecordsReceived = 1,
                RecordsProcessed = 1
            };
            
            _mockExternalApiService
                .Setup(x => x.PostDataAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(batchResult);
            
            _mockExternalApiService
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync("{\"value\":[{\"ImportStatus\":\"Completed\",\"ContactsReceived\":1,\"ContactsImported\":1,\"IsPartiallyImport\":false}]}");

            // Act
            var result = await service.ImportEmployeeRegistration(leads);

            // Assert
            Assert.That(result.BatchResults[0].TokenFromEshot, Is.EqualTo("{invalid json}"));
        }

        [Test]
        public async Task ImportEmployeeRegistration_EmptyToken_MarksAsFailed()
        {
            // Arrange
            var service = CreateService();
            var leads = new List<TestLead> { new TestLead { Email = "test@example.com", Name = "Test User" } };
            var chunks = new List<IList<TestLead>> { leads };
            
            _mockCsvService.Setup(x => x.GetByteCount(leads)).Returns(1000);
            _mockChunkingService.Setup(x => x.GetChunks(1000, leads)).Returns(chunks);
            _mockPayLoadMapper.Setup(x => x.MapToPayload(It.IsAny<IList<TestLead>>(), "Lead"))
                .Returns(new List<ExpandoObject> { new ExpandoObject() });
            _mockCsvService.Setup(x => x.ToCsv(It.IsAny<IList<ExpandoObject>>())).Returns("csv,data");
            
            var batchResult = new BatchResultDetail
            {
                Status = "Completed",
                TokenFromEshot = "",
                RecordsReceived = 1,
                RecordsProcessed = 0
            };
            
            _mockExternalApiService
                .Setup(x => x.PostDataAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(batchResult);

            // Act
            var result = await service.ImportEmployeeRegistration(leads);

            // Assert
            Assert.That(result.BatchResults.Any(b => b.Status == "Failed"), Is.True);
            Assert.That(result.BatchResults.Any(b => b.Error?.Contains("No token received")), Is.True);
        }

        #endregion

        #region VerifyContactImport Tests - Retry Logic

        [Test]
        public async Task ImportEmployeeRegistration_VerificationSucceedsOnSecondAttempt_CompletesSuccessfully()
        {
            // Arrange
            var service = CreateService();
            var leads = new List<TestLead> { new TestLead { Email = "test@example.com", Name = "Test User" } };
            var chunks = new List<IList<TestLead>> { leads };
            
            _mockCsvService.Setup(x => x.GetByteCount(leads)).Returns(1000);
            _mockChunkingService.Setup(x => x.GetChunks(1000, leads)).Returns(chunks);
            _mockPayLoadMapper.Setup(x => x.MapToPayload(It.IsAny<IList<TestLead>>(), "Lead"))
                .Returns(new List<ExpandoObject> { new ExpandoObject() });
            _mockCsvService.Setup(x => x.ToCsv(It.IsAny<IList<ExpandoObject>>())).Returns("csv,data");
            
            var batchResult = new BatchResultDetail
            {
                Status = "Completed",
                TokenFromEshot = "test-token",
                RecordsReceived = 1,
                RecordsProcessed = 1
            };
            
            _mockExternalApiService
                .Setup(x => x.PostDataAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(batchResult);
            
            _mockExternalApiService
                .SetupSequence(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync("{\"value\":[]}")
                .ReturnsAsync("{\"value\":[{\"ImportStatus\":\"Completed\",\"ContactsReceived\":1,\"ContactsImported\":1,\"IsPartiallyImport\":false}]}");

            // Act
            var result = await service.ImportEmployeeRegistration(leads);

            // Assert
            Assert.That(result.Status, Is.EqualTo("Completed"));
            _mockExternalApiService.Verify(x => x.GetDataAsync(It.IsAny<string>()), Times.AtLeast(2));
        }

        [Test]
        public async Task ImportEmployeeRegistration_ProcessingStatusRetriesUntilComplete_CompletesSuccessfully()
        {
            // Arrange
            var service = CreateService();
            var leads = new List<TestLead> { new TestLead { Email = "test@example.com", Name = "Test User" } };
            var chunks = new List<IList<TestLead>> { leads };
            
            _mockCsvService.Setup(x => x.GetByteCount(leads)).Returns(1000);
            _mockChunkingService.Setup(x => x.GetChunks(1000, leads)).Returns(chunks);
            _mockPayLoadMapper.Setup(x => x.MapToPayload(It.IsAny<IList<TestLead>>(), "Lead"))
                .Returns(new List<ExpandoObject> { new ExpandoObject() });
            _mockCsvService.Setup(x => x.ToCsv(It.IsAny<IList<ExpandoObject>>())).Returns("csv,data");
            
            var batchResult = new BatchResultDetail
            {
                Status = "Completed",
                TokenFromEshot = "test-token",
                RecordsReceived = 1,
                RecordsProcessed = 1
            };
            
            _mockExternalApiService
                .Setup(x => x.PostDataAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(batchResult);
            
            _mockExternalApiService
                .SetupSequence(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync("{\"value\":[{\"ImportStatus\":\"Waiting\"}]}")
                .ReturnsAsync("{\"value\":[{\"ImportStatus\":\"Processing\"}]}")
                .ReturnsAsync("{\"value\":[{\"ImportStatus\":\"Completed\",\"ContactsReceived\":1,\"ContactsImported\":1,\"IsPartiallyImport\":false}]}");

            // Act
            var result = await service.ImportEmployeeRegistration(leads);

            // Assert
            Assert.That(result.Status, Is.EqualTo("Completed"));
            _mockExternalApiService.Verify(x => x.GetDataAsync(It.IsAny<string>()), Times.AtLeast(3));
        }

        [Test]
        public async Task ImportEmployeeRegistration_MaxRetriesExceeded_MarkesAsFailed()
        {
            // Arrange
            var service = CreateService();
            var leads = new List<TestLead> { new TestLead { Email = "test@example.com", Name = "Test User" } };
            var chunks = new List<IList<TestLead>> { leads };
            
            _mockCsvService.Setup(x => x.GetByteCount(leads)).Returns(1000);
            _mockChunkingService.Setup(x => x.GetChunks(1000, leads)).Returns(chunks);
            _mockPayLoadMapper.Setup(x => x.MapToPayload(It.IsAny<IList<TestLead>>(), "Lead"))
                .Returns(new List<ExpandoObject> { new ExpandoObject() });
            _mockCsvService.Setup(x => x.ToCsv(It.IsAny<IList<ExpandoObject>>())).Returns("csv,data");
            
            var batchResult = new BatchResultDetail
            {
                Status = "Completed",
                TokenFromEshot = "test-token",
                RecordsReceived = 1,
                RecordsProcessed = 0
            };
            
            _mockExternalApiService
                .Setup(x => x.PostDataAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(batchResult);
            
            _mockExternalApiService
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync("{\"value\":[]}");

            // Act
            var result = await service.ImportEmployeeRegistration(leads);

            // Assert
            Assert.That(result.BatchResults.Any(b => b.Status == "Failed"), Is.True);
            Assert.That(result.BatchResults.Any(b => b.Error?.Contains("No import status found")), Is.True);
            _mockExternalApiService.Verify(x => x.GetDataAsync(It.IsAny<string>()), Times.AtLeast(3));
        }

        [Test]
        public async Task ImportEmployeeRegistration_VerificationThrowsException_RetriesAndFails()
        {
            // Arrange
            var service = CreateService();
            var leads = new List<TestLead> { new TestLead { Email = "test@example.com", Name = "Test User" } };
            var chunks = new List<IList<TestLead>> { leads };
            
            _mockCsvService.Setup(x => x.GetByteCount(leads)).Returns(1000);
            _mockChunkingService.Setup(x => x.GetChunks(1000, leads)).Returns(chunks);
            _mockPayLoadMapper.Setup(x => x.MapToPayload(It.IsAny<IList<TestLead>>(), "Lead"))
                .Returns(new List<ExpandoObject> { new ExpandoObject() });
            _mockCsvService.Setup(x => x.ToCsv(It.IsAny<IList<ExpandoObject>>())).Returns("csv,data");
            
            var batchResult = new BatchResultDetail
            {
                Status = "Completed",
                TokenFromEshot = "test-token",
                RecordsReceived = 1,
                RecordsProcessed = 0
            };
            
            _mockExternalApiService
                .Setup(x => x.PostDataAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(batchResult);
            
            _mockExternalApiService
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException("Verification API error"));

            // Act
            var result = await service.ImportEmployeeRegistration(leads);

            // Assert
            Assert.That(result.BatchResults.Any(b => b.Status == "Failed"), Is.True);
            Assert.That(result.BatchResults.Any(b => b.Error?.Contains("Failed to verify import")), Is.True);
        }

        #endregion

        #region VerifyContactImport Tests - Import Status Handling

        [Test]
        public async Task ImportEmployeeRegistration_ErrorStatusWithPartialImport_MarksAsCompleted()
        {
            // Arrange
            var service = CreateService();
            var leads = new List<TestLead> { new TestLead { Email = "test@example.com", Name = "Test User" } };
            var chunks = new List<IList<TestLead>> { leads };
            
            _mockCsvService.Setup(x => x.GetByteCount(leads)).Returns(1000);
            _mockChunkingService.Setup(x => x.GetChunks(1000, leads)).Returns(chunks);
            _mockPayLoadMapper.Setup(x => x.MapToPayload(It.IsAny<IList<TestLead>>(), "Lead"))
                .Returns(new List<ExpandoObject> { new ExpandoObject() });
            _mockCsvService.Setup(x => x.ToCsv(It.IsAny<IList<ExpandoObject>>())).Returns("csv,data");
            
            var batchResult = new BatchResultDetail
            {
                Status = "Completed",
                TokenFromEshot = "test-token",
                RecordsReceived = 10,
                RecordsProcessed = 5
            };
            
            _mockExternalApiService
                .Setup(x => x.PostDataAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(batchResult);
            
            _mockExternalApiService
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync("{\"value\":[{\"ImportStatus\":\"Error\",\"ContactsReceived\":10,\"ContactsImported\":5,\"IsPartiallyImport\":true,\"AdditionalInfo\":\"Some records failed validation\"}]}");

            // Act
            var result = await service.ImportEmployeeRegistration(leads);

            // Assert
            Assert.That(result.BatchResults.Any(b => b.Status == "Completed"), Is.True);
            Assert.That(result.BatchResults.Any(b => b.IsPartiallyImported), Is.True);
            Assert.That(result.BatchResults.Any(b => b.RecordsFailed == 5), Is.True);
        }

        [Test]
        public async Task ImportEmployeeRegistration_ErrorStatusWithNoImportedRecords_MarksAsFailed()
        {
            // Arrange
            var service = CreateService();
            var leads = new List<TestLead> { new TestLead { Email = "test@example.com", Name = "Test User" } };
            var chunks = new List<IList<TestLead>> { leads };
            
            _mockCsvService.Setup(x => x.GetByteCount(leads)).Returns(1000);
            _mockChunkingService.Setup(x => x.GetChunks(1000, leads)).Returns(chunks);
            _mockPayLoadMapper.Setup(x => x.MapToPayload(It.IsAny<IList<TestLead>>(), "Lead"))
                .Returns(new List<ExpandoObject> { new ExpandoObject() });
            _mockCsvService.Setup(x => x.ToCsv(It.IsAny<IList<ExpandoObject>>())).Returns("csv,data");
            
            var batchResult = new BatchResultDetail
            {
                Status = "Completed",
                TokenFromEshot = "test-token",
                RecordsReceived = 10,
                RecordsProcessed = 0
            };
            
            _mockExternalApiService
                .Setup(x => x.PostDataAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(batchResult);
            
            _mockExternalApiService
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync("{\"value\":[{\"ImportStatus\":\"Error\",\"ContactsReceived\":10,\"ContactsImported\":0,\"IsPartiallyImport\":false,\"AdditionalInfo\":\"Invalid data format\"}]}");

            // Act
            var result = await service.ImportEmployeeRegistration(leads);

            // Assert
            Assert.That(result.BatchResults.Any(b => b.Status == "Failed"), Is.True);
            Assert.That(result.BatchResults.Any(b => b.Error?.Contains("Invalid data format")), Is.True);
        }

        [Test]
        public async Task ImportEmployeeRegistration_UsesIsPartiallyImportedFieldName_ParsesCorrectly()
        {
            // Arrange
            var service = CreateService();
            var leads = new List<TestLead> { new TestLead { Email = "test@example.com", Name = "Test User" } };
            var chunks = new List<IList<TestLead>> { leads };
            
            _mockCsvService.Setup(x => x.GetByteCount(leads)).Returns(1000);
            _mockChunkingService.Setup(x => x.GetChunks(1000, leads)).Returns(chunks);
            _mockPayLoadMapper.Setup(x => x.MapToPayload(It.IsAny<IList<TestLead>>(), "Lead"))
                .Returns(new List<ExpandoObject> { new ExpandoObject() });
            _mockCsvService.Setup(x => x.ToCsv(It.IsAny<IList<ExpandoObject>>())).Returns("csv,data");
            
            var batchResult = new BatchResultDetail
            {
                Status = "Completed",
                TokenFromEshot = "test-token",
                RecordsReceived = 10,
                RecordsProcessed = 8
            };
            
            _mockExternalApiService
                .Setup(x => x.PostDataAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(batchResult);
            
            // Test with alternative field name
            _mockExternalApiService
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync("{\"value\":[{\"ImportStatus\":\"Completed\",\"ContactsReceived\":10,\"ContactsImported\":8,\"IsPartiallyImported\":true}]}");

            // Act
            var result = await service.ImportEmployeeRegistration(leads);

            // Assert
            Assert.That(result.BatchResults.Any(b => b.IsPartiallyImported), Is.True);
        }

        #endregion

        #region Integration Tests with Multiple Templates

        [Test]
        public async Task ImportEmployeeRegistration_MultipleTemplates_ProcessesAllTemplates()
        {
            // Arrange
            var service = CreateService();
            var leads = new List<TestLead> { new TestLead { Email = "test@example.com", Name = "Test User" } };
            var chunks = new List<IList<TestLead>> { leads };
            
            SetupSuccessfulImport(leads, chunks);

            // Act
            var result = await service.ImportEmployeeRegistration(leads);

            // Assert
            Assert.That(result.BatchResults.Count, Is.EqualTo(2)); // 2 templates
            Assert.That(result.BatchResults.Any(b => b.BatchId?.Contains("Template 123")), Is.True);
            Assert.That(result.BatchResults.Any(b => b.BatchId?.Contains("Template 456")), Is.True);
        }

        [Test]
        public async Task ImportEmployeeRegistration_CustomRetryCount_UsesConfiguredValue()
        {
            // Arrange
            _emailMarketingApi.ApiRetryCount = 2;
            var service = CreateService();
            var leads = new List<TestLead> { new TestLead { Email = "test@example.com", Name = "Test User" } };
            var chunks = new List<IList<TestLead>> { leads };
            
            _mockCsvService.Setup(x => x.GetByteCount(leads)).Returns(1000);
            _mockChunkingService.Setup(x => x.GetChunks(1000, leads)).Returns(chunks);
            _mockPayLoadMapper.Setup(x => x.MapToPayload(It.IsAny<IList<TestLead>>(), "Lead"))
                .Returns(new List<ExpandoObject> { new ExpandoObject() });
            _mockCsvService.Setup(x => x.ToCsv(It.IsAny<IList<ExpandoObject>>())).Returns("csv,data");
            
            var batchResult = new BatchResultDetail
            {
                Status = "Completed",
                TokenFromEshot = "test-token",
                RecordsReceived = 1,
                RecordsProcessed = 0
            };
            
            _mockExternalApiService
                .Setup(x => x.PostDataAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(batchResult);
            
            _mockExternalApiService
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync("{\"value\":[]}");

            // Act
            var result = await service.ImportEmployeeRegistration(leads);

            // Assert
            _mockExternalApiService.Verify(x => x.GetDataAsync(It.IsAny<string>()), Times.Exactly(4)); // 2 retries × 2 templates
        }

        [Test]
        public async Task ImportEmployeeRegistration_ZeroRetryCount_UsesDefaultFive()
        {
            // Arrange
            _emailMarketingApi.ApiRetryCount = 0;
            var service = CreateService();
            var leads = new List<TestLead> { new TestLead { Email = "test@example.com", Name = "Test User" } };
            var chunks = new List<IList<TestLead>> { leads };
            
            _mockCsvService.Setup(x => x.GetByteCount(leads)).Returns(1000);
            _mockChunkingService.Setup(x => x.GetChunks(1000, leads)).Returns(chunks);
            _mockPayLoadMapper.Setup(x => x.MapToPayload(It.IsAny<IList<TestLead>>(), "Lead"))
                .Returns(new List<ExpandoObject> { new ExpandoObject() });
            _mockCsvService.Setup(x => x.ToCsv(It.IsAny<IList<ExpandoObject>>())).Returns("csv,data");
            
            var batchResult = new BatchResultDetail
            {
                Status = "Completed",
                TokenFromEshot = "test-token",
                RecordsReceived = 1,
                RecordsProcessed = 0
            };
            
            _mockExternalApiService
                .Setup(x => x.PostDataAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(batchResult);
            
            _mockExternalApiService
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync("{\"value\":[]}");

            // Act
            var result = await service.ImportEmployeeRegistration(leads);

            // Assert
            _mockExternalApiService.Verify(x => x.GetDataAsync(It.IsAny<string>()), Times.Exactly(10)); // 5 retries × 2 templates
        }

        #endregion

        #region Helper Methods

        private void SetupSuccessfulImport(List<TestLead> leads, List<IList<TestLead>> chunks)
        {
            _mockCsvService.Setup(x => x.GetByteCount(leads)).Returns(1000);
            _mockChunkingService.Setup(x => x.GetChunks(1000, leads)).Returns(chunks);
            _mockPayLoadMapper.Setup(x => x.MapToPayload(It.IsAny<IList<TestLead>>(), "Lead"))
                .Returns(new List<ExpandoObject> { new ExpandoObject() });
            _mockCsvService.Setup(x => x.ToCsv(It.IsAny<IList<ExpandoObject>>())).Returns("csv,data");
            
            var batchResult = new BatchResultDetail
            {
                Status = "Completed",
                TokenFromEshot = "test-token",
                RecordsReceived = 1,
                RecordsProcessed = 1
            };
            
            _mockExternalApiService
                .Setup(x => x.PostDataAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(batchResult);
            
            _mockExternalApiService
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync("{\"value\":[{\"ImportStatus\":\"Completed\",\"ContactsReceived\":1,\"ContactsImported\":1,\"IsPartiallyImport\":false}]}");
        }

        private void SetupPartialImport(List<TestLead> leads, List<IList<TestLead>> chunks)
        {
            _mockCsvService.Setup(x => x.GetByteCount(leads)).Returns(1000);
            _mockChunkingService.Setup(x => x.GetChunks(1000, leads)).Returns(chunks);
            _mockPayLoadMapper.Setup(x => x.MapToPayload(It.IsAny<IList<TestLead>>(), "Lead"))
                .Returns(new List<ExpandoObject> { new ExpandoObject() });
            _mockCsvService.Setup(x => x.ToCsv(It.IsAny<IList<ExpandoObject>>())).Returns("csv,data");
            
            var batchResult = new BatchResultDetail
            {
                Status = "Completed",
                TokenFromEshot = "test-token",
                RecordsReceived = 10,
                RecordsProcessed = 7
            };
            
            _mockExternalApiService
                .Setup(x => x.PostDataAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(batchResult);
            
            _mockExternalApiService
                .Setup(x => x.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync("{\"value\":[{\"ImportStatus\":\"Error\",\"ContactsReceived\":10,\"ContactsImported\":7,\"IsPartiallyImport\":true,\"AdditionalInfo\":\"3 records failed validation\"}]}");
        }

        #endregion

        #region Test Helper Class

        public class TestLead
        {
            public string Email { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
        }

        #endregion
    }
}