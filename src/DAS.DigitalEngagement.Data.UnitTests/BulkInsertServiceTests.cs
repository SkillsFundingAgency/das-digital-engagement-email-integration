using DAS.DigitalEngagement.CampaignInterest.Data.Service;
using System.Data;

namespace DAS.DigitalEngagement.CampaignInterest.Data.UnitTests;

/// <summary>
/// Unit tests for BulkInsertService.
/// </summary>
[TestFixture]
public class BulkInsertServiceTests
{
    #region Data Transformation Tests - ConvertToDataTable

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

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(table.Columns["Id"]!.DataType, Is.EqualTo(typeof(int)));
            Assert.That(table.Columns["Name"]!.DataType, Is.EqualTo(typeof(string)));
            Assert.That(table.Columns["IsActive"]!.DataType, Is.EqualTo(typeof(bool)));
            Assert.That(table.Columns["CreatedDate"]!.DataType, Is.EqualTo(typeof(DateTime)));
        });
    }

    [Test]
    public void ConvertToDataTable_Should_Handle_Nullable_Types()
    {
        // Arrange
        var data = new List<NullableEntity>
        {
            new() { Id = 1, NullableInt = 42, NullableDate = DateTime.UtcNow, NullableDecimal = 100.5m }
        };

        // Act
        var table = InvokeConvertToDataTable(data);

        // Assert - Nullable types should be converted to their underlying type
        Assert.Multiple(() =>
        {
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
    public void ConvertToDataTable_Should_Only_Include_Public_Properties()
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
        Assert.That(table.Columns.Contains("PublicProperty"), Is.True);
    }

    [Test]
    public void ConvertToDataTable_Should_Handle_Complex_Entity_With_Multiple_Types()
    {
        // Arrange
        var data = new List<ComplexEntity>
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

        // Act
        var table = InvokeConvertToDataTable(data);

        // Assert
        Assert.That(table.Columns, Has.Count.EqualTo(6));
        Assert.Multiple(() =>
        {
            Assert.That(table.Columns["Id"]!.DataType, Is.EqualTo(typeof(int)));
            Assert.That(table.Columns["Name"]!.DataType, Is.EqualTo(typeof(string)));
            Assert.That(table.Columns["Amount"]!.DataType, Is.EqualTo(typeof(decimal)));
            Assert.That(table.Columns["IsActive"]!.DataType, Is.EqualTo(typeof(bool)));
            Assert.That(table.Columns["CreatedDate"]!.DataType, Is.EqualTo(typeof(DateTime)));
            Assert.That(table.Columns["Count"]!.DataType, Is.EqualTo(typeof(int)));
        });
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Uses reflection to invoke the private ConvertToDataTable method.
    /// This allows testing the core data transformation logic without SqlBulkCopy dependencies.
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

    private class EntityWithVariousMembers
    {
        public string PublicProperty { get; set; } = string.Empty;
        public static string StaticProperty { get; set; } = string.Empty;
    }

    #endregion
}
