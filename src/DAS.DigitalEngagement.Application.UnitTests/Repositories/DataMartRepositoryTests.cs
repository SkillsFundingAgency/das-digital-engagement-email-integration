using System.Collections.Generic;
using System.Data.Common;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using DAS.DigitalEngagement.Application.Repositories;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace DAS.DigitalEngagement.Application.UnitTests.Repositories
{
    [TestFixture]
    public class DataMartRepositoryTests
    {
        [Test]
        public async Task RetrieveEmployeeRegistrationData_ReturnsRows_WhenDataExists()
        {
            // Arrange
            var tokenCredentialMock = new Mock<TokenCredential>();
            var loggerMock = new Mock<ILogger<DataMartRepository>>();
            var connectionString = new ConnectionString { DataMart = "TestConnectionString" };
            var optionsMock = Mock.Of<IOptions<ConnectionString>>(o => o.Value == connectionString);

            var accessToken = new AccessToken("mock-token", System.DateTimeOffset.Now.AddMinutes(5));
            tokenCredentialMock
                .Setup(tc => tc.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(accessToken);

            var dbConnectionMock = new Mock<DbConnection>();
            var dbCommandMock = new Mock<DbCommand>();
            var dbDataReaderMock = new Mock<DbDataReader>();

            dbConnectionMock.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            dbConnectionMock.Setup(c => c.CreateCommand()).Returns(dbCommandMock.Object);

            dbCommandMock.SetupProperty(c => c.CommandText);
            dbCommandMock.Setup(c => c.ExecuteReaderAsync(It.IsAny<CancellationToken>())).ReturnsAsync(dbDataReaderMock.Object);

            dbDataReaderMock.SetupSequence(r => r.ReadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            dbDataReaderMock.Setup(r => r.FieldCount).Returns(2);
            dbDataReaderMock.Setup(r => r.GetName(0)).Returns("Id");
            dbDataReaderMock.Setup(r => r.GetName(1)).Returns("Name");
            dbDataReaderMock.Setup(r => r.IsDBNullAsync(0, It.IsAny<CancellationToken>())).ReturnsAsync(false);
            dbDataReaderMock.Setup(r => r.IsDBNullAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(false);
            dbDataReaderMock.Setup(r => r.GetValue(0)).Returns(1);
            dbDataReaderMock.Setup(r => r.GetValue(1)).Returns("TestName");

            var repo = new DataMartRepository(
                tokenCredentialMock.Object,
                optionsMock,
                loggerMock.Object,
                () => dbConnectionMock.Object);

            // Act
            var result = await repo.RetrieveEmployeeRegistrationData();

            // Assert
            Assert.That(result, Has.Count.EqualTo(1));
            var row = (IDictionary<string, object?>)result[0];
            Assert.That(row["Id"], Is.EqualTo(1));
            Assert.That(row["Name"], Is.EqualTo("TestName"));
        }

        [Test]
        public async Task RetrieveEmployeeRegistrationData_ReturnsEmptyList_WhenNoData()
        {
            // Arrange
            var tokenCredentialMock = new Mock<TokenCredential>();
            var loggerMock = new Mock<ILogger<DataMartRepository>>();
            var connectionString = new ConnectionString { DataMart = "TestConnectionString" };
            var optionsMock = Mock.Of<IOptions<ConnectionString>>(o => o.Value == connectionString);

            var accessToken = new AccessToken("mock-token", System.DateTimeOffset.Now.AddMinutes(5));
            tokenCredentialMock
                .Setup(tc => tc.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(accessToken);

            var dbConnectionMock = new Mock<DbConnection>();
            var dbCommandMock = new Mock<DbCommand>();
            var dbDataReaderMock = new Mock<DbDataReader>();

            dbConnectionMock.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            dbConnectionMock.Setup(c => c.CreateCommand()).Returns(dbCommandMock.Object);

            dbCommandMock.SetupProperty(c => c.CommandText);
            dbCommandMock.Setup(c => c.ExecuteReaderAsync(It.IsAny<CancellationToken>())).ReturnsAsync(dbDataReaderMock.Object);

            dbDataReaderMock.SetupSequence(r => r.ReadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var repo = new DataMartRepository(
                tokenCredentialMock.Object,
                optionsMock,
                loggerMock.Object,
                () => dbConnectionMock.Object);

            // Act
            var result = await repo.RetrieveEmployeeRegistrationData();

            // Assert
            Assert.That(result, Is.Empty);
        }
    }
}