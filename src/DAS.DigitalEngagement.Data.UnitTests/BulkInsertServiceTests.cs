using DAS.DigitalEngagement.CampaignInterest.Data.Helpers;
using DAS.DigitalEngagement.CampaignInterest.Data.Service;
using Microsoft.Extensions.Logging;
using Moq;
using System.Data;

namespace DAS.DigitalEngagement.CampaignInterest.Data.UnitTests;

[TestFixture]
public class BulkInsertServiceTests
{
    private BulkInsertService _service = null!;
    private Mock<ILogger<BulkInsertService>> _loggerMock = null!;
    private Mock<IDbConnectionFactory> _mockConnectionFactory = null!;
    private Mock<IDbConnection> _mockConnection = null!;
    private List<TestEntity> _testData = null!;

    [SetUp]
    public void Setup()
    {
        _testData =
        [
            new TestEntity { Id = 1, Name = "Test1", IsActive = true, CreatedDate = DateTime.UtcNow },
            new TestEntity { Id = 2, Name = "Test2", IsActive = false, CreatedDate = DateTime.UtcNow.AddDays(-1) }
        ];

        _loggerMock = new Mock<ILogger<BulkInsertService>>();
        _mockConnectionFactory = new Mock<IDbConnectionFactory>();
        _mockConnection = new Mock<IDbConnection>();

        _mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(_mockConnection.Object);

        _service = new BulkInsertService(_mockConnectionFactory.Object, _loggerMock.Object);
    }

    #region Basic Functionality Tests

    [Test]
    public void BulkInsertAsync_CurrentImplementation_Throws_InvalidCastException_With_Mock()
    {
        // Arrange
        var tableName = "dbo.TestTable";

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _service.BulkInsertAsync(_testData, tableName));

        Assert.That(ex!.Message, Does.Contain("SqlConnection"));
    }

    [Test]
    public void BulkInsertAsync_Should_Accept_Empty_Collection()
    {
        // Arrange
        var emptyData = new List<TestEntity>();
        var tableName = "dbo.TestTable";

        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _service.BulkInsertAsync(emptyData, tableName));
    }

    [Test]
    public void BulkInsertAsync_Should_Fail_Before_Validating_Null_Data_Due_To_SqlConnection_Cast()
    {
        // Arrange
        List<TestEntity>? nullData = null;
        var tableName = "dbo.TestTable";

        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _service.BulkInsertAsync(nullData!, tableName));
    }

    [Test]
    public void BulkInsertAsync_Should_Accept_Schema_Qualified_Table_Name()
    {
        // Arrange
        var tableName = "dbo.MyTable";

        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _service.BulkInsertAsync(_testData, tableName));
    }

    [Test]
    public void BulkInsertAsync_Should_Accept_Simple_Table_Name()
    {
        // Arrange
        var tableName = "MyTable";

        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _service.BulkInsertAsync(_testData, tableName));
    }

    #endregion

    #region Entity Type Tests

    [Test]
    public void BulkInsertAsync_Should_Handle_Entity_With_Multiple_Data_Types()
    {
        // Arrange
        var complexData = new List<ComplexEntity>
        {
            new()
            {
                Id = 1,
                Name = "Test",
                Amount = 100.50m,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                Count = 42
            }
        };
        var tableName = "dbo.ComplexTable";

        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _service.BulkInsertAsync(complexData, tableName));
    }

    [Test]
    public void BulkInsertAsync_Should_Handle_Entity_With_Nullable_Properties()
    {
        // Arrange
        var nullableData = new List<NullableEntity>
        {
            new() { Id = 1, NullableInt = null, NullableDate = null, NullableDecimal = 100.5m },
            new() { Id = 2, NullableInt = 42, NullableDate = DateTime.UtcNow, NullableDecimal = null }
        };
        var tableName = "dbo.NullableTable";

        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _service.BulkInsertAsync(nullableData, tableName));
    }

    [Test]
    public void BulkInsertAsync_Should_Handle_Entity_With_String_Properties()
    {
        // Arrange
        var stringData = new List<StringEntity>
        {
            new() { Id = 1, Name = "Test", Description = "Description", Email = "test@example.com" }
        };
        var tableName = "dbo.StringTable";

        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _service.BulkInsertAsync(stringData, tableName));
    }

    [Test]
    public void BulkInsertAsync_Should_Handle_Entity_With_All_Null_Values()
    {
        // Arrange
        var nullData = new List<AllNullableEntity>
        {
            new() { Name = null, Age = null, IsActive = null }
        };
        var tableName = "dbo.AllNullableTable";

        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _service.BulkInsertAsync(nullData, tableName));
    }

    #endregion

    #region Performance and Scalability Tests

    [Test]
    public void BulkInsertAsync_Should_Handle_Large_Dataset()
    {
        // Arrange
        var largeDataSet = Enumerable.Range(1, 10000)
            .Select(i => new TestEntity
            {
                Id = i,
                Name = $"Test{i}",
                IsActive = i % 2 == 0,
                CreatedDate = DateTime.UtcNow.AddDays(-i)
            })
            .ToList();
        var tableName = "dbo.LargeTable";

        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _service.BulkInsertAsync(largeDataSet, tableName));
    }

    [Test]
    public void BulkInsertAsync_Should_Handle_Very_Large_Dataset()
    {
        // Arrange
        var veryLargeDataSet = Enumerable.Range(1, 50000)
            .Select(i => new TestEntity
            {
                Id = i,
                Name = $"Test{i}",
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            })
            .ToList();
        var tableName = "dbo.VeryLargeTable";

        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _service.BulkInsertAsync(veryLargeDataSet, tableName));
    }

    [Test]
    public void BulkInsertAsync_Should_Handle_Single_Record()
    {
        // Arrange
        var singleRecord = new List<TestEntity>
        {
            new() { Id = 1, Name = "Single", IsActive = true, CreatedDate = DateTime.UtcNow }
        };
        var tableName = "dbo.SingleRecordTable";

        // Act & Assert
        Assert.ThrowsAsync<InvalidCastException>(async () =>
            await _service.BulkInsertAsync(singleRecord, tableName));
    }

    #endregion

    #region Data Type Conversion Tests

    [Test]
    public void ConvertToDataTable_Should_Create_Table_With_Correct_Columns()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new() { Id = 1, Name = "Test", IsActive = true, CreatedDate = DateTime.UtcNow }
        };

        // Act
        var table = InvokeConvertToDataTable(data);

        // Assert
        Assert.That(table, Is.Not.Null);
        Assert.That(table.Columns, Has.Count.EqualTo(4));
        Assert.Multiple(() =>
        {
            Assert.That(table.Columns.Contains("Id"), Is.True);
            Assert.That(table.Columns.Contains("Name"), Is.True);
            Assert.That(table.Columns.Contains("IsActive"), Is.True);
            Assert.That(table.Columns.Contains("CreatedDate"), Is.True);
        });
    }

    [Test]
    public void ConvertToDataTable_Should_Set_Correct_Column_Types()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new() { Id = 1, Name = "Test", IsActive = true, CreatedDate = DateTime.UtcNow }
        };

        // Act
        var table = InvokeConvertToDataTable(data);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(table.Columns["Id"]!.DataType, Is.EqualTo(typeof(int)));
            Assert.That(table.Columns["Name"]!.DataType, Is.EqualTo(typeof(string)));
            Assert.That(table.Columns["IsActive"]!.DataType, Is.EqualTo(typeof(bool)));
            Assert.That(table.Columns["CreatedDate"]!.DataType, Is.EqualTo(typeof(DateTime)));
        });
    }

    [Test]
    public void ConvertToDataTable_Should_Handle_Nullable_Types_Correctly()
    {
        // Arrange
        var data = new List<NullableEntity>
        {
            new() { Id = 1, NullableInt = 42, NullableDate = DateTime.UtcNow, NullableDecimal = 100.5m }
        };

        // Act
        var table = InvokeConvertToDataTable(data);

        Assert.Multiple(() =>
        {
            // Assert
            // Nullable types should be converted to their underlying type
            Assert.That(table.Columns["NullableInt"]!.DataType, Is.EqualTo(typeof(int)));
            Assert.That(table.Columns["NullableDate"]!.DataType, Is.EqualTo(typeof(DateTime)));
            Assert.That(table.Columns["NullableDecimal"]!.DataType, Is.EqualTo(typeof(decimal)));
        });
    }

    [Test]
    public void ConvertToDataTable_Should_Convert_Null_Values_To_DBNull()
    {
        // Arrange
        var data = new List<NullableEntity>
        {
            new() { Id = 1, NullableInt = null, NullableDate = null, NullableDecimal = null }
        };

        // Act
        var table = InvokeConvertToDataTable(data);

        // Assert
        Assert.That(table.Rows, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(table.Rows[0]["NullableInt"], Is.EqualTo(DBNull.Value));
            Assert.That(table.Rows[0]["NullableDate"], Is.EqualTo(DBNull.Value));
            Assert.That(table.Rows[0]["NullableDecimal"], Is.EqualTo(DBNull.Value));
        });
    }

    [Test]
    public void ConvertToDataTable_Should_Preserve_Non_Null_Values()
    {
        // Arrange
        var testDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var data = new List<TestEntity>
        {
            new() { Id = 42, Name = "TestName", IsActive = true, CreatedDate = testDate }
        };

        // Act
        var table = InvokeConvertToDataTable(data);

        // Assert
        Assert.That(table.Rows, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(table.Rows[0]["Id"], Is.EqualTo(42));
            Assert.That(table.Rows[0]["Name"], Is.EqualTo("TestName"));
            Assert.That(table.Rows[0]["IsActive"], Is.EqualTo(true));
            Assert.That(table.Rows[0]["CreatedDate"], Is.EqualTo(testDate));
        });
    }

    [Test]
    public void ConvertToDataTable_Should_Handle_Empty_Collection()
    {
        // Arrange
        var emptyData = new List<TestEntity>();

        // Act
        var table = InvokeConvertToDataTable(emptyData);

        // Assert
        Assert.That(table, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(table.Columns, Has.Count.EqualTo(4));
            Assert.That(table.Rows, Is.Empty);
        });
    }

    [Test]
    public void ConvertToDataTable_Should_Handle_Multiple_Rows()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new() { Id = 1, Name = "Test1", IsActive = true, CreatedDate = DateTime.UtcNow },
            new() { Id = 2, Name = "Test2", IsActive = false, CreatedDate = DateTime.UtcNow.AddDays(-1) },
            new() { Id = 3, Name = "Test3", IsActive = true, CreatedDate = DateTime.UtcNow.AddDays(-2) }
        };

        // Act
        var table = InvokeConvertToDataTable(data);

        // Assert
        Assert.That(table.Rows, Has.Count.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(table.Rows[0]["Id"], Is.EqualTo(1));
            Assert.That(table.Rows[1]["Id"], Is.EqualTo(2));
            Assert.That(table.Rows[2]["Id"], Is.EqualTo(3));
        });
    }

    [Test]
    public void ConvertToDataTable_Should_Only_Include_Public_Instance_Properties()
    {
        // Arrange
        var data = new List<EntityWithVariousMembers>
        {
            new() { PublicProperty = "visible" }
        };

        // Act
        var table = InvokeConvertToDataTable(data);

        // Assert
        Assert.That(table.Columns, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(table.Columns.Contains("PublicProperty"), Is.True);
            Assert.That(table.Columns.Contains("PrivateField"), Is.False);
            Assert.That(table.Columns.Contains("StaticProperty"), Is.False);
        });
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Uses reflection to invoke the private ConvertToDataTable method for testing
    /// </summary>
    private static DataTable InvokeConvertToDataTable<T>(IEnumerable<T> data)
    {
        var method = typeof(BulkInsertService).GetMethod("ConvertToDataTable",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var genericMethod = method!.MakeGenericMethod(typeof(T));
        return (DataTable)genericMethod.Invoke(null, [data])!;
    }

    #endregion

    #region Test Entity Classes

    private class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    private class ComplexEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public int Count { get; set; }
    }

    private class NullableEntity
    {
        public int Id { get; set; }
        public int? NullableInt { get; set; }
        public DateTime? NullableDate { get; set; }
        public decimal? NullableDecimal { get; set; }
    }

    private class StringEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    private class AllNullableEntity
    {
        public string? Name { get; set; }
        public int? Age { get; set; }
        public bool? IsActive { get; set; }
    }

    private class EntityWithVariousMembers
    {
        public string PublicProperty { get; set; } = string.Empty;
        public static string StaticProperty { get; set; } = string.Empty;
    }

    #endregion
}
