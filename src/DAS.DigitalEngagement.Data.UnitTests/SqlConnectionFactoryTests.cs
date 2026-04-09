using Azure.Core;
using DAS.DigitalEngagement.CampaignInterest.Data.Helpers;
using Microsoft.Data.SqlClient;
using Moq;
using System.Data;

namespace DAS.DigitalEngagement.CampaignInterest.Data.UnitTests;

[TestFixture]
public class SqlConnectionFactoryTests
{
    private const string ValidConnectionString = "Server=localhost;Database=TestDb;Integrated Security=true;";

    #region Constructor Tests

    [Test]
    public void Constructor_Should_Accept_Valid_Connection_String()
    {
        // Act
        var factory = new SqlConnectionFactory(ValidConnectionString);

        // Assert
        Assert.That(factory, Is.Not.Null);
    }

    // Additional constructor tests with token credentials
    [Test]
    public void Constructor_Should_Accept_Valid_Connection_String_And_Token_Credential()
    {
        // Arrange
        var tokenCredential = new Mock<TokenCredential>();

        // Act
        var factory = new SqlConnectionFactory(ValidConnectionString, tokenCredential.Object);

        // Assert
        Assert.That(factory, Is.Not.Null);
    }

    #endregion

    #region CreateConnection Tests

    [Test]
    public async Task CreateConnection_Should_Return_SqlConnection_Instance()
    {
        // Arrange
        var factory = new SqlConnectionFactory(ValidConnectionString);

        // Act
        var connection = await factory.CreateConnectionAsync();

        // Assert
        Assert.That(connection, Is.Not.Null);
        Assert.That(connection, Is.InstanceOf<SqlConnection>());
    }

    [Test]
    public async Task CreateConnection_Should_Return_IDbConnection_Instance()
    {
        // Arrange
        var factory = new SqlConnectionFactory(ValidConnectionString);

        // Act
        var connection = await factory.CreateConnectionAsync();

        // Assert
        Assert.That(connection, Is.Not.Null);
        Assert.That(connection, Is.InstanceOf<IDbConnection>());
    }

    [Test]
    public async Task CreateConnection_Should_Return_Connection_With_Correct_Connection_String()
    {
        // Arrange
        var factory = new SqlConnectionFactory(ValidConnectionString);

        // Act
        var connection = await factory.CreateConnectionAsync() as SqlConnection;

        // Assert
        Assert.That(connection, Is.Not.Null);
        Assert.That(connection!.ConnectionString, Is.EqualTo(ValidConnectionString));
    }

    [Test]
    public async Task CreateConnection_Should_Return_Closed_Connection()
    {
        // Arrange
        var factory = new SqlConnectionFactory(ValidConnectionString);

        // Act
        var connection = await factory.CreateConnectionAsync();

        // Assert
        Assert.That(connection.State, Is.EqualTo(ConnectionState.Closed));
    }

    [Test]
    public async Task CreateConnection_Should_Return_New_Instance_On_Each_Call()
    {
        // Arrange
        var factory = new SqlConnectionFactory(ValidConnectionString);

        // Act
        var connection1 = await factory.CreateConnectionAsync();
        var connection2 = await factory.CreateConnectionAsync();
        var connection3 = await factory.CreateConnectionAsync();

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(connection1, Is.Not.Null);
            Assert.That(connection2, Is.Not.Null);
            Assert.That(connection3, Is.Not.Null);
        });
        Assert.Multiple(() =>
        {
            Assert.That(connection1, Is.Not.SameAs(connection2));
            Assert.That(connection2, Is.Not.SameAs(connection3));
        });
        Assert.That(connection1, Is.Not.SameAs(connection3));
    }

    [Test]
    public async Task CreateConnection_Should_Handle_Multiple_Calls_With_Same_Connection_String()
    {
        // Arrange
        var factory = new SqlConnectionFactory(ValidConnectionString);

        // Act
        var connections = new List<IDbConnection>();
        for (int i = 0; i < 5; i++)
        {
            connections.Add(await factory.CreateConnectionAsync());
        }

        // Assert
        Assert.That(connections, Has.Count.EqualTo(5));
        foreach (var connection in connections)
        {
            Assert.Multiple(() =>
            {
                Assert.That(connection, Is.InstanceOf<SqlConnection>());
                Assert.That(((SqlConnection)connection).ConnectionString, Is.EqualTo(ValidConnectionString));
            });
        }

        // Verify all are different instances
        for (int i = 0; i < connections.Count; i++)
        {
            for (int j = i + 1; j < connections.Count; j++)
            {
                Assert.That(connections[i], Is.Not.SameAs(connections[j]));
            }
        }
    }

    [Test]
    public void CreateConnection_Should_Throw_Exception_For_Null_Or_Empty_Connection_String()
    {
        // Arrange
        var factoryWithNull = new SqlConnectionFactory(null!);
        var factoryWithEmpty = new SqlConnectionFactory(string.Empty);

        // Act & Assert
        Assert.That(async () => await factoryWithNull.CreateConnectionAsync(), Throws.InvalidOperationException);
        Assert.That(async () => await factoryWithEmpty.CreateConnectionAsync(), Throws.InvalidOperationException);
    }

    #endregion

    #region Connection String Variations Tests

    [Test]
    public async Task CreateConnection_Should_Handle_Connection_String_With_Trusted_Connection()
    {
        // Arrange
        var connectionString = "Server=myServer;Database=myDB;Trusted_Connection=True;";
        var factory = new SqlConnectionFactory(connectionString);

        // Act
        var connection = await factory.CreateConnectionAsync() as SqlConnection;

        // Assert
        Assert.That(connection, Is.Not.Null);
        Assert.That(connection!.ConnectionString, Is.EqualTo(connectionString));
    }

    [Test]
    public async Task CreateConnection_Should_Handle_Connection_String_With_Username_Password()
    {
        // Arrange
        var connectionString = "Server=myServer;Database=myDB;User Id=myUser;Password=myPassword;";
        var factory = new SqlConnectionFactory(connectionString);

        // Act
        var connection = await factory.CreateConnectionAsync() as SqlConnection;

        // Assert
        Assert.That(connection, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(connection!.ConnectionString, Contains.Substring("myServer"));
            Assert.That(connection.ConnectionString, Contains.Substring("myDB"));
        });
    }

    [Test]
    public async Task CreateConnection_Should_Handle_Connection_String_With_Additional_Parameters()
    {
        // Arrange
        var connectionString = "Server=myServer;Database=myDB;Integrated Security=true;Timeout=30;Encrypt=True;";
        var factory = new SqlConnectionFactory(connectionString);

        // Act
        var connection = await factory.CreateConnectionAsync() as SqlConnection;

        // Assert
        Assert.That(connection, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(connection!.ConnectionString, Contains.Substring("Timeout"));
            Assert.That(connection.ConnectionString, Contains.Substring("Encrypt"));
        });
    }

    #endregion

    #region Interface Implementation Tests

    [Test]
    public void SqlConnectionFactory_Should_Implement_IDbConnectionFactory()
    {
        // Arrange
        var factory = new SqlConnectionFactory(ValidConnectionString);

        // Act & Assert
        Assert.That(factory, Is.InstanceOf<IDbConnectionFactory>());
    }

    [Test]
    public async Task CreateConnection_Through_Interface_Should_Return_IDbConnection()
    {
        // Arrange
#pragma warning disable CA1859
        IDbConnectionFactory factory = new SqlConnectionFactory(ValidConnectionString);
#pragma warning restore CA1859

        // Act
        var connection = await factory.CreateConnectionAsync();

        // Assert
        Assert.That(connection, Is.Not.Null);
        Assert.That(connection, Is.InstanceOf<IDbConnection>());
        Assert.That(connection, Is.InstanceOf<SqlConnection>());
    }

    #endregion

    #region Multiple Factory Instances Tests

    [Test]
    public async Task Multiple_Factories_With_Different_Connection_Strings_Should_Create_Distinct_Connections()
    {
        // Arrange
        var connectionString1 = "Server=server1;Database=db1;Integrated Security=true;";
        var connectionString2 = "Server=server2;Database=db2;Integrated Security=true;";

        var factory1 = new SqlConnectionFactory(connectionString1);
        var factory2 = new SqlConnectionFactory(connectionString2);

        // Act
        var connection1 = await factory1.CreateConnectionAsync() as SqlConnection;
        var connection2 = await factory2.CreateConnectionAsync() as SqlConnection;

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(connection1, Is.Not.Null);
            Assert.That(connection2, Is.Not.Null);
        });
        Assert.Multiple(() =>
        {
            Assert.That(connection1!.ConnectionString, Is.EqualTo(connectionString1));
            Assert.That(connection2!.ConnectionString, Is.EqualTo(connectionString2));
            Assert.That(connection1, Is.Not.SameAs(connection2));
        });
    }

    [Test]
    public async Task Multiple_Factories_With_Same_Connection_String_Should_Create_Independent_Connections()
    {
        // Arrange
        var factory1 = new SqlConnectionFactory(ValidConnectionString);
        var factory2 = new SqlConnectionFactory(ValidConnectionString);

        // Act
        var connection1 = await factory1.CreateConnectionAsync();
        var connection2 = await factory2.CreateConnectionAsync();

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(connection1, Is.Not.SameAs(connection2));
            Assert.That(((SqlConnection)connection1).ConnectionString,
                Is.EqualTo(((SqlConnection)connection2).ConnectionString));
        });
    }

    #endregion

    #region Performance Tests

    [Test]
    public async Task CreateConnection_Should_Execute_Quickly()
    {
        // Arrange
        var factory = new SqlConnectionFactory(ValidConnectionString);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var connection = await factory.CreateConnectionAsync();
        stopwatch.Stop();

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(connection, Is.Not.Null);
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(100),
                "Connection creation should complete in less than 100ms");
        });
    }

    [Test]
    public async Task CreateConnection_Should_Handle_Rapid_Successive_Calls()
    {
        // Arrange
        var factory = new SqlConnectionFactory(ValidConnectionString);
        var connections = new List<IDbConnection>();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        for (int i = 0; i < 100; i++)
        {
            connections.Add(await factory.CreateConnectionAsync());
        }
        stopwatch.Stop();

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(connections, Has.Count.EqualTo(100));
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(1000),
                "Creating 100 connections should complete in less than 1 second");
        });
    }

    #endregion

    #region Connection Disposal Tests

    [Test]
    public async Task Created_Connection_Should_Be_Disposable()
    {
        // Arrange
        var factory = new SqlConnectionFactory(ValidConnectionString);
        var connection = await factory.CreateConnectionAsync();

        // Act & Assert
        Assert.DoesNotThrow(() => connection.Dispose());
    }

    [Test]
    public async Task Created_Connection_Should_Support_Using_Statement()
    {
        // Arrange
        var factory = new SqlConnectionFactory(ValidConnectionString);

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            using var connection = await factory.CreateConnectionAsync();
            Assert.That(connection, Is.Not.Null);
        });
    }

    [Test]
    public async Task Disposing_Connection_Should_Not_Affect_Factory()
    {
        // Arrange
        var factory = new SqlConnectionFactory(ValidConnectionString);

        // Act
        using (var connection1 = await factory.CreateConnectionAsync())
        {
            Assert.That(connection1, Is.Not.Null);
        }

        var connection2 = await factory.CreateConnectionAsync();

        // Assert
        Assert.That(connection2, Is.Not.Null);
        Assert.That(connection2.State, Is.EqualTo(ConnectionState.Closed));
    }

    #endregion
}
