using DAS.DigitalEngagement.Application.Services;
using DAS.DigitalEngagement.Models.Infrastructure;
using NUnit.Framework;
using System.Collections.Generic;
using System.Dynamic;

namespace DAS.DigitalEngagement.EmailIntegration.UnitTests.Services
{
    public class PayLoadMapperTests
    {
        [Test]
        public void MapToPayload_ThrowsIfLeadsIsNull()
        {
            var mapper = new PayLoadMapper(new List<DataMartSettings>());
            Assert.Throws<System.ArgumentNullException>(() => mapper.MapToPayload<object>(null, "TestObject"));
        }

        [Test]
        public void MapToPayload_ReturnsMappedPayload()
        {
            var fieldMapping = "[{\"Source\":\"firstName\",\"Target\":\"FirstName\"},{\"Source\":\"lastName\",\"Target\":\"LastName\"}]";
            var dataMartSettings = new List<DataMartSettings>
            {
                new DataMartSettings
                {
                    ObjectName = "Lead",
                    FieldMapping = fieldMapping,
                    ViewName = "LeadView",
                    TemplatedUploadId = new[] { 0 }
                }
            };
            var mapper = new PayLoadMapper(dataMartSettings);

            var lead = new Dictionary<string, object>
            {
                { "firstName", "John" },
                { "lastName", "Doe" }
            };
            var leads = new List<Dictionary<string, object>> { lead };

            var result = mapper.MapToPayload(leads, "Lead");

            Assert.That(result, Has.Count.EqualTo(1));
            var expando = result[0] as IDictionary<string, object>;
            Assert.That(expando["FirstName"], Is.EqualTo("John"));
            Assert.That(expando["LastName"], Is.EqualTo("Doe"));
        }

        [Test]
        public void MapDynamic_MapsFieldsCorrectly()
        {
            var maps = new List<FieldMap>
            {
                new FieldMap { Source = "foo", Target = "bar" }
            };
            var source = new Dictionary<string, object> { { "foo", 123 } };

            var result = PayLoadMapper.MapDynamic(source, maps);

            var dict = (IDictionary<string, object>)result;
            Assert.That(dict["bar"], Is.EqualTo(123));
        }

        [Test]
        public void GetValueByPath_ReturnsValue_WhenPathExists()
        {
            var source = new Dictionary<string, object>
            {
                { "a", new Dictionary<string, object> { { "b", "value" } } }
            };

            var value = PayLoadMapper.GetValueByPath(source, "a.b");

            Assert.That(value, Is.EqualTo("value"));
        }

        [Test]
        public void GetValueByPath_ReturnsNull_WhenPathDoesNotExist()
        {
            var source = new Dictionary<string, object> { { "a", 1 } };

            var value = PayLoadMapper.GetValueByPath(source, "a.b");

            Assert.That(value, Is.Null);
        }

        [Test]
        public void AddFields_AddsFieldsToExpandoObject()
        {
            dynamic expando = new ExpandoObject();
            expando.Existing = "foo";
            var extra = new Dictionary<string, object> { { "NewField", 42 } };

            var mapper = new PayLoadMapper(new List<DataMartSettings>());
            var result = mapper.AddFields(expando, extra);

            var dict = (IDictionary<string, object>)result;
            Assert.That(dict["Existing"], Is.EqualTo("foo"));
            Assert.That(dict["NewField"], Is.EqualTo(42));
        }
    }
}