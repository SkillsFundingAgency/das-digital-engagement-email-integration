using Azure.Core;
using DAS.DigitalEngagement.Application.Repositories;
using DAS.DigitalEngagement.EmailIntegration.UnitTests.Repositories.Helpers;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;

namespace DAS.DigitalEngagement.EmailIntegration.UnitTests.Repositories
{
    [TestFixture]
    public class DataMartRepositoryTests
    {
        private Mock<TokenCredential> _tokenCredentialMock;
        private Mock<ILogger<DataMartRepository>> _loggerMock;
        private DataMartRepository dataMartRepo;

        [SetUp]
        public void SetUp()
        {
            _tokenCredentialMock = new Mock<TokenCredential>();
            _loggerMock = new Mock<ILogger<DataMartRepository>>();

            _tokenCredentialMock
                .Setup(t => t.GetTokenAsync(
                    It.IsAny<TokenRequestContext>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessToken("fake-token", DateTimeOffset.UtcNow.AddHours(1)));

            // Fake ADO.NET pipeline
            var reader = new FakeDbDataReader();
            var command = new FakeDbCommand(reader);
            var connection = new FakeDbConnection(command);

            var connectionStringMock =
                new Mock<IOptions<ConnectionString>>();

            connectionStringMock
                .Setup(cs => cs.Value)
                .Returns(new ConnectionString { DataMart = "FakeConnectionString" });

            // Create a single reusable repository instance for use by tests
            dataMartRepo = new DataMartRepository(
                _tokenCredentialMock.Object,
                connectionStringMock.Object,
                _loggerMock.Object,
                () => connection);
        }

        [Test]
        public async Task RetrieveEmployeeRegistrationData_ReturnsExpectedRow()
        {
            // Arrange: Use the shared dataMartRepo created in SetUp.

            // Act
            var result = await dataMartRepo.RetrieveEmployeeRegistrationData("mailIntegration");

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            var row = (IDictionary<string, object>)result[0];
            Assert.That(row["EmployeeId"], Is.EqualTo(123));
            Assert.That(row["Name"], Is.EqualTo("John Doe"));
        }

        [Test]
        public void RetrieveEmployeeRegistrationData_ShouldThrowArgumentException_WhenViewNameIsNullOrEmpty()
        {
            // Arrange
            string viewName = null;

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(() => dataMartRepo.RetrieveEmployeeRegistrationData(viewName));
            Assert.That(ex.Message, Is.EqualTo("View name cannot be empty. (Parameter 'viewName')"));
        }

        [Test]
        public void RetrieveEmployeeRegistrationData_Throws_WhenTokenAcquisitionFails()
        {
            // Arrange
            var failingToken = new Mock<TokenCredential>();
            failingToken
                .Setup(t => t.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Token acquisition failed"));

            var connectionStringMock = new Mock<IOptions<ConnectionString>>();
            connectionStringMock.Setup(cs => cs.Value).Returns(new ConnectionString { DataMart = "FakeConnectionString" });

            var reader = new FakeDbDataReader();
            var command = new FakeDbCommand(reader);
            var connection = new FakeDbConnection(command);

            var repo = new DataMartRepository(
                failingToken.Object,
                connectionStringMock.Object,
                _loggerMock.Object,
                () => connection);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(() => repo.RetrieveEmployeeRegistrationData("mailIntegration"));
            Assert.That(ex.Message, Is.EqualTo("Token acquisition failed"));

            failingToken.Verify(t => t.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void RetrieveEmployeeRegistrationData_Throws_WhenConnectionOpenFails()
        {
            // Arrange
            var token = new Mock<TokenCredential>();
            token
                .Setup(t => t.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessToken("fake-token", DateTimeOffset.UtcNow.AddHours(1)));

            var connectionStringMock = new Mock<IOptions<ConnectionString>>();
            connectionStringMock.Setup(cs => cs.Value).Returns(new ConnectionString { DataMart = "FakeConnectionString" });

            var mockConnection = new Mock<DbConnection>();
            mockConnection
                .Setup(c => c.OpenAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Open failed"));

            var repo = new DataMartRepository(
                token.Object,
                connectionStringMock.Object,
                _loggerMock.Object,
                () => mockConnection.Object);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(() => repo.RetrieveEmployeeRegistrationData("mailIntegration"));
            Assert.That(ex.Message, Is.EqualTo("Open failed"));

            mockConnection.Verify(c => c.OpenAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void RetrieveEmployeeRegistrationData_Throws_WhenExecuteReaderFails()
        {
            // Arrange
            var token = new Mock<TokenCredential>();
            token
                .Setup(t => t.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessToken("fake-token", DateTimeOffset.UtcNow.AddHours(1)));

            var connectionStringMock = new Mock<IOptions<ConnectionString>>();
            connectionStringMock
                .Setup(cs => cs.Value)
                .Returns(new ConnectionString { DataMart = "FakeConnectionString" });

            var failingCommand = new FailingDbCommand();
            var connection = new FakeDbConnection(failingCommand);

            var loggerMock = new Mock<ILogger<DataMartRepository>>();

            var repo = new DataMartRepository(
                token.Object,
                connectionStringMock.Object,
                loggerMock.Object,
                () => connection);

            // Act + Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(
                () => repo.RetrieveEmployeeRegistrationData("mailIntegration"));

            Assert.That(ex!.Message, Is.EqualTo("ExecuteReader failed"));
        }
    }
}