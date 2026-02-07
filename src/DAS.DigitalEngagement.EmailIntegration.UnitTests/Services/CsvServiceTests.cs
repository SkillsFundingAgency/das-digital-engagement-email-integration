using DAS.DigitalEngagement.Application.Services;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Text;

namespace DAS.DigitalEngagement.EmailIntegration.UnitTests.Services
{
    [TestFixture]
    public class CsvServiceTests
    {
        private CsvService _service;

        [SetUp]
        public void SetUp()
        {
            _service = new CsvService();
        }

        private class TestLead
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        [Test]
        public void GetByteCount_ReturnsCorrectByteCount_ForSimpleObjectList()
        {
            var leads = new List<TestLead>
            {
                new TestLead { Id = 1, Name = "Alice" },
                new TestLead { Id = 2, Name = "Bob" }
            };

            var csv = _service.ToCsv(leads);
            var expected = Encoding.Unicode.GetByteCount(csv);

            Assert.That(_service.GetByteCount(leads), Is.EqualTo(expected));
        }

        [Test]
        public void ToCsv_ReturnsCsv_ForSimpleObjectList()
        {
            var leads = new List<TestLead>
            {
                new TestLead { Id = 1, Name = "Alice" },
                new TestLead { Id = 2, Name = "Bob" }
            };

            var csv = _service.ToCsv(leads);

            StringAssert.Contains("Id,Name", csv);
            StringAssert.Contains("1,Alice", csv);
            StringAssert.Contains("2,Bob", csv);
        }

        [Test]
        public void ToCsv_ReturnsCsv_ForExpandoObjectList()
        {
            dynamic lead1 = new ExpandoObject();
            lead1.Id = 1;
            lead1.Name = "Alice";
            dynamic lead2 = new ExpandoObject();
            lead2.Id = 2;
            lead2.Name = "Bob";

            var leads = new List<ExpandoObject> { lead1, lead2 };

            var csv = _service.ToCsv(leads);

            StringAssert.Contains("Id,Name", csv);
            StringAssert.Contains("1,Alice", csv);
            StringAssert.Contains("2,Bob", csv);
        }

        [Test]
        public void ToCsv_ReturnsEmptyString_ForEmptyExpandoList()
        {
            var leads = new List<ExpandoObject>();

            var csv = _service.ToCsv(leads);

            Assert.That(csv, Is.Empty);
        }

        [Test]
        public void IsEmpty_ReturnsTrue_ForEmptyStream()
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Flush();
            stream.Position = 0;
            using var reader = new StreamReader(stream);

            var result = _service.IsEmpty(reader);

            Assert.That(result, Is.True);
        }

        [Test]
        public void IsEmpty_ReturnsFalse_ForNonEmptyStream()
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write("header1,header2\nvalue1,value2");
            writer.Flush();
            stream.Position = 0;
            using var reader = new StreamReader(stream);

            var result = _service.IsEmpty(reader);

            Assert.That(result, Is.False);
        }

        [Test]
        public void HasData_ReturnsFalse_WhenOnlyHeadersPresent()
        {
            var csv = "header1,header2\n";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
            using var reader = new StreamReader(stream);

            var result = _service.HasData(reader);

            Assert.That(result, Is.False);
        }

        [Test]
        public void HasData_ReturnsTrue_WhenDataPresent()
        {
            var csv = "header1,header2\nvalue1,value2\n";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
            using var reader = new StreamReader(stream);

            var result = _service.HasData(reader);

            Assert.That(result, Is.True);
        }

        [Test]
        public void GenerateStreamFromString_ReturnsStreamWithCorrectContent()
        {
            var content = "test,data";
            using var stream = _service.GenerateStreamFromString(content);
            using var reader = new StreamReader(stream);

            var result = reader.ReadToEnd();

            Assert.That(result, Is.EqualTo(content));
        }
    }
}