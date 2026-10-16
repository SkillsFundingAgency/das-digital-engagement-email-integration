using System.Data.Common;
using Azure.Core;
using DAS.DigitalEngagement.CampaignInterest.Data.Repositories;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace DAS.DigitalEngagement.CampaignInterest.Data.UnitTests
{
    [TestFixture]
    public class DataMartRepositoryTests
    {
        [Test]
        public async Task RetrieveEmployeeRegistrationData_ReturnsRows_WhenDataExists()
        {
            var (repo, tokenCredentialMock, loggerMock, dbConnectionMock, dbCommandMock, dbDataReaderMock) = CreateRepository();

            dbConnectionMock.Setup(c => c.CreateCommand()).Returns(dbCommandMock.Object);
            dbCommandMock.SetupProperty(c => c.CommandText);
            dbCommandMock.SetupProperty(c => c.CommandTimeout);
            dbCommandMock.Setup(c => c.ExecuteReaderAsync(It.IsAny<CancellationToken>())).ReturnsAsync(dbDataReaderMock.Object);

            dbDataReaderMock.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            dbDataReaderMock.Setup(r => r.FieldCount).Returns(2);
            dbDataReaderMock.Setup(r => r.GetName(0)).Returns("Id");
            dbDataReaderMock.Setup(r => r.GetName(1)).Returns("Name");
            dbDataReaderMock.Setup(r => r.IsDBNullAsync(0)).ReturnsAsync(false);
            dbDataReaderMock.Setup(r => r.IsDBNullAsync(1)).ReturnsAsync(false);
            dbDataReaderMock.Setup(r => r.GetValue(0)).Returns(1);
            dbDataReaderMock.Setup(r => r.GetValue(1)).Returns("TestName");

            var result = await repo.RetrieveEmployeeRegistrationData();

            Assert.That(result, Has.Count.EqualTo(1));
            var row = (IDictionary<string, object?>)result[0];
            Assert.That(row["Id"], Is.EqualTo(1));
            Assert.That(row["Name"], Is.EqualTo("TestName"));

            Assert.Multiple(() =>
            {
                Assert.That(dbCommandMock.Object.CommandText, Is.EqualTo("SELECT * FROM [ASData_PL].[vw_DAS_EmailIntegration] ORDER BY EMAIL"));
                Assert.That(dbCommandMock.Object.CommandTimeout, Is.EqualTo(120));
            });

            VerifyLog(loggerMock, "Retrieved 1 employee registration records");
            tokenCredentialMock.Verify(tc => tc.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()), Times.Once);
            dbConnectionMock.Verify(c => c.OpenAsync(It.IsAny<CancellationToken>()), Times.Once);
            dbConnectionMock.Verify(c => c.CreateCommand(), Times.Once);
            dbCommandMock.Verify(c => c.ExecuteReaderAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task RetrieveEmployeeRegistrationData_ReturnsEmptyList_WhenNoData()
        {
            var (repo, tokenCredentialMock, loggerMock, dbConnectionMock, dbCommandMock, dbDataReaderMock) = CreateRepository();

            dbConnectionMock.Setup(c => c.CreateCommand()).Returns(dbCommandMock.Object);
            dbCommandMock.SetupProperty(c => c.CommandText);
            dbCommandMock.SetupProperty(c => c.CommandTimeout);
            dbCommandMock.Setup(c => c.ExecuteReaderAsync(It.IsAny<CancellationToken>())).ReturnsAsync(dbDataReaderMock.Object);
            dbDataReaderMock.SetupSequence(r => r.ReadAsync())
                .ReturnsAsync(false);

            var result = await repo.RetrieveEmployeeRegistrationData();

            Assert.That(result, Is.Empty);
            VerifyLog(loggerMock, "Retrieved 0 employee registration records");
            tokenCredentialMock.Verify(tc => tc.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()), Times.Once);
            dbConnectionMock.Verify(c => c.OpenAsync(It.IsAny<CancellationToken>()), Times.Once);
            dbConnectionMock.Verify(c => c.CreateCommand(), Times.Once);
            dbCommandMock.Verify(c => c.ExecuteReaderAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task RetrieveEmployeeRegistrationData_Throws_WhenTokenAcquisitionFails()
        {
            var tokenCredentialMock = new Mock<TokenCredential>();
            var loggerMock = new Mock<ILogger<DataMartRepository>>();
            var connectionString = new ConnectionString { CampaignsDatabase = "TestConnectionString", DataMart = "TestConnectionString" };
            var optionsMock = Mock.Of<IOptions<ConnectionString>>(o => o.Value == connectionString);
            var dbConnectionMock = new Mock<DbConnection>();
            var factoryInvoked = false;

            tokenCredentialMock
                .Setup(tc => tc.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Token failed"));

            var repo = new DataMartRepository(
                tokenCredentialMock.Object,
                optionsMock,
                loggerMock.Object,
                () =>
                {
                    factoryInvoked = true;
                    return dbConnectionMock.Object;
                });

            Assert.That(async () => await repo.RetrieveEmployeeRegistrationData(), Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("Token failed"));
            Assert.That(factoryInvoked, Is.False);
            dbConnectionMock.Verify(c => c.OpenAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task RetrieveEmployeeRegistrationData_Throws_WhenConnectionOpenFails()
        {
            var tokenCredentialMock = new Mock<TokenCredential>();
            var loggerMock = new Mock<ILogger<DataMartRepository>>();
            var connectionString = new ConnectionString { CampaignsDatabase = "TestConnectionString", DataMart = "TestConnectionString" };
            var optionsMock = Mock.Of<IOptions<ConnectionString>>(o => o.Value == connectionString);
            var accessToken = new AccessToken("mock-token", DateTimeOffset.UtcNow.AddMinutes(5));
            var sqlConnectionMock = new Mock<SqlConnection>();

            tokenCredentialMock
                .Setup(tc => tc.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(accessToken);
            sqlConnectionMock.SetupProperty(c => c.AccessToken);
            sqlConnectionMock.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Open failed"));

            var repo = new DataMartRepository(
                tokenCredentialMock.Object,
                optionsMock,
                loggerMock.Object,
                () => sqlConnectionMock.Object);

            Assert.That(async () => await repo.RetrieveEmployeeRegistrationData(), Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("Open failed"));
            Assert.That(sqlConnectionMock.Object.AccessToken, Is.EqualTo("mock-token"));
            tokenCredentialMock.Verify(tc => tc.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()), Times.Once);
            sqlConnectionMock.Verify(c => c.OpenAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task RetrieveEmployeeRegistrationData_Throws_WhenReaderExecutionFails()
        {
            var (repo, tokenCredentialMock, loggerMock, dbConnectionMock, dbCommandMock, _) = CreateRepository();
            var accessToken = new AccessToken("mock-token", DateTimeOffset.UtcNow.AddMinutes(5));

            tokenCredentialMock
                .Setup(tc => tc.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(accessToken);
            dbConnectionMock.Setup(c => c.CreateCommand()).Returns(dbCommandMock.Object);
            dbCommandMock.Setup(c => c.ExecuteReaderAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Execute failed"));

            Assert.That(async () => await repo.RetrieveEmployeeRegistrationData(), Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("Execute failed"));
            tokenCredentialMock.Verify(tc => tc.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()), Times.Once);
            dbConnectionMock.Verify(c => c.OpenAsync(It.IsAny<CancellationToken>()), Times.Once);
            dbConnectionMock.Verify(c => c.CreateCommand(), Times.Once);
            dbCommandMock.Verify(c => c.ExecuteReaderAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        private static (DataMartRepository Repo, Mock<TokenCredential> TokenCredentialMock, Mock<ILogger<DataMartRepository>> LoggerMock, Mock<DbConnection> DbConnectionMock, Mock<DbCommand> DbCommandMock, Mock<DbDataReader> DbDataReaderMock) CreateRepository()
        {
            var tokenCredentialMock = new Mock<TokenCredential>();
            var loggerMock = new Mock<ILogger<DataMartRepository>>();
            var connectionString = new ConnectionString { CampaignsDatabase = "TestConnectionString", DataMart = "TestConnectionString" };
            var optionsMock = Mock.Of<IOptions<ConnectionString>>(o => o.Value == connectionString);
            var accessToken = new AccessToken("mock-token", DateTimeOffset.UtcNow.AddMinutes(5));

            tokenCredentialMock
                .Setup(tc => tc.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(accessToken);

            var dbConnectionMock = new Mock<DbConnection>();
            dbConnectionMock.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var dbCommandMock = new Mock<DbCommand>();
            var dbDataReaderMock = new Mock<DbDataReader>();

            var repo = new DataMartRepository(
                tokenCredentialMock.Object,
                optionsMock,
                loggerMock.Object,
                () => dbConnectionMock.Object);

            return (repo, tokenCredentialMock, loggerMock, dbConnectionMock, dbCommandMock, dbDataReaderMock);
        }

        private static void VerifyLog(Mock<ILogger<DataMartRepository>> loggerMock, string message)
        {
            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString() == message),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}