using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DAS.DigitalEngagement.Application.Handlers.Import.Interfaces;
using DAS.DigitalEngagement.Models.Import;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace DAS.DigitalEngagement.EmailIntegration.UnitTests.Functions
{
    [TestFixture]
    public class EmailIntegrationTests
    {
        private Mock<ILogger<EmailIntegration>> _mockLogger;
        private Mock<IImportDataMartHandler> _mockImportDataMartHandler;
        private ApplicationConfiguration _mockConfiguration;
        private EmailIntegration _emailIntegration;

        [SetUp]
        public void SetUp()
        {
            _mockLogger = new Mock<ILogger<EmailIntegration>>();


            _mockImportDataMartHandler = new Mock<IImportDataMartHandler>();

            _mockConfiguration = new ApplicationConfiguration
            {
                DataMart = new[]
                {
                new DataMartSettings
                {
                    ViewName = "TestView"
                }
            },
                ConnectionString = new ConnectionString
                {
                    DataMart = "TestConnectionString"
                },
                EShotAPIM = new EShotAPIM
                {
                    ApiBaseUrl = "https://api.test.com"
                }
            };

            _emailIntegration = new EmailIntegration(_mockLogger.Object, _mockImportDataMartHandler.Object, _mockConfiguration);

        }

        [Test]
        public async Task RunAsync_ShouldLogError_WhenDataMartSettingsIsNull()
        {
            // Arrange
            _mockConfiguration.DataMart = Array.Empty<DataMartSettings>();

            // Act
            var timerInfo = Activator.CreateInstance(typeof(Microsoft.Azure.Functions.Worker.TimerInfo), true) as Microsoft.Azure.Functions.Worker.TimerInfo
                ?? throw new InvalidOperationException("Failed to create TimerInfo instance.");

            await _emailIntegration.RunAsync(timerInfo);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("First DataMartSettings entry is null.")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once
            );
        }


        [TestCase(new object[0], "First DataMartSettings entry is null.")]
        [TestCase(new object[] { "ValidView" }, "Starting Email Integration Job")]
        public async Task RunAsync_ShouldHandleVariousDataMartSettings(object[] dataMartSettings, string expectedLogMessage)
        {
            // Arrange
            _mockConfiguration.DataMart = dataMartSettings?.Select(viewName => new DataMartSettings { ViewName = viewName?.ToString() }).ToArray();


            var timerInfo = Activator.CreateInstance(typeof(Microsoft.Azure.Functions.Worker.TimerInfo), true) as Microsoft.Azure.Functions.Worker.TimerInfo
                ?? throw new InvalidOperationException("Failed to create TimerInfo instance.");

            // Act
            await _emailIntegration.RunAsync(timerInfo);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(expectedLogMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once
            );
        }

        [Test]
        public async Task RunAsync_ShouldLogInformation_WhenJobCompletesSuccessfully()
        {
            // Arrange
            var validDataMartSettings = new DataMartSettings
            {
                ViewName = "ValidView"
            };

            _mockConfiguration.DataMart = new[] { validDataMartSettings };

            _mockImportDataMartHandler
                .Setup(h => h.Handle(It.IsAny<DataMartSettings>()))
                .ReturnsAsync(new BulkImportStatus
                {
                    Container = "TestContainer",
                    Name = "TestName",
                    Id = "TestId",
                    BulkImportJobStatus = new List<BulkImportJobStatus>(),
                    ValidationError = string.Empty
                }); // Simulate successful handling

            var timerInfo = Activator.CreateInstance(typeof(Microsoft.Azure.Functions.Worker.TimerInfo), true) as Microsoft.Azure.Functions.Worker.TimerInfo
                ?? throw new InvalidOperationException("Failed to create TimerInfo instance.");

            // Act
            await _emailIntegration.RunAsync(timerInfo);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Email Integration Job completed successfully")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once
            );
        }

        [Test]
        public async Task RunAsync_ShouldLogError_WhenJobFailsWithException()
        {
            // Arrange
            var validDataMartSettings = new DataMartSettings
            {
                ViewName = "ValidView"
            };

            _mockConfiguration.DataMart = new[] { validDataMartSettings };

            var testException = new Exception("Test exception");

            _mockImportDataMartHandler
                .Setup(h => h.Handle(It.IsAny<DataMartSettings>()))
                .ThrowsAsync(testException); // Simulate an exception during handling

            var timerInfo = Activator.CreateInstance(typeof(Microsoft.Azure.Functions.Worker.TimerInfo), true) as Microsoft.Azure.Functions.Worker.TimerInfo
                ?? throw new InvalidOperationException("Failed to create TimerInfo instance.");

            // Act
            await _emailIntegration.RunAsync(timerInfo);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Email Integration Job failed with an exception")),
                    testException,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once
            );
        }

    }
}
