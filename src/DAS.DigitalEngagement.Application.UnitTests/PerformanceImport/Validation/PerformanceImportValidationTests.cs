using System;
using System.Data;
using DAS.DigitalEngagement.Application.PerformanceImport.Validation;

namespace DAS.DigitalEngagement.EmailIntegration.UnitTests.PerformanceImport.Validation;

[TestFixture]
public class PerformanceImportValidationTests
{
    [TestCase("Name", "550 5.7.1 Message rejected; contact <postmaster> for details")]
    [TestCase("Name", "Hello <FirstName>, your offer")]
    [TestCase("Name", "if score < threshold and value > 5")]
    [TestCase("Name", "John Smith <john.smith@example.com>")]
    [TestCase("Name", "<https://example.com/path?online=true>")]
    [TestCase("Name", "Smith &amp; Sons")]
    [TestCase("Name", "Family 👨‍👩‍👧")]
    [TestCase("Name", "javascript: is a language")]
    [TestCase("URL", "https://example.com/?online=true&source=portal")]
    public void ValidateTable_WhenValueIsLegitimate_DoesNotThrow(string columnName, string value)
    {
        Assert.DoesNotThrow(() => PerformanceImportValidation.ValidateTable(CreateTable(columnName, value), "import.Test"));
    }

    [TestCase("Name", "<svg/onload=alert(1)>", "HTML markup")]
    [TestCase("Name", "<x onmouseover=alert(1)>hover</x>", "HTML markup")]
    [TestCase("Name", "&amp;lt;script&amp;gt;alert(1)", "HTML markup")]
    [TestCase("Name", "<![CDATA[x]]>", "HTML markup")]
    [TestCase("URL", " JavaScript:alert(1)", "URL schemes")]
    [TestCase("Name", "RTL \u202Egpj.exe", "control characters")]
    [TestCase("Name", "C1 \u009B control", "control characters")]
    public void ValidateTable_WhenValueIsUnsafe_Throws(string columnName, string value, string expectedReason)
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => PerformanceImportValidation.ValidateTable(CreateTable(columnName, value), "import.Test"));

        Assert.That(exception!.Message, Does.Contain($"import.Test.{columnName}"));
        Assert.That(exception.Message, Does.Contain(expectedReason));
    }

    [Test]
    public void ValidateTable_WhenValueContainsLoneSurrogate_Throws()
    {
        var value = "Lone " + (char)0xD800 + " surrogate";

        var exception = Assert.Throws<InvalidOperationException>(
            () => PerformanceImportValidation.ValidateTable(CreateTable("Name", value), "import.Test"));

        Assert.That(exception!.Message, Does.Contain("control characters"));
    }

    private static DataTable CreateTable(string columnName, string value)
    {
        var table = new DataTable();
        table.Columns.Add(columnName, typeof(string));
        table.Rows.Add(value);
        return table;
    }
}
