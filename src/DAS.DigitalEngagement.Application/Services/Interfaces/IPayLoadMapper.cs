using System.Dynamic;

namespace DAS.DigitalEngagement.Application.Services.Interfaces
{
    public interface IPayLoadMapper
    {
        IList<ExpandoObject> MapToPayload<T>(IList<T> leads, string objectName);
        ExpandoObject AddFields(ExpandoObject target, IDictionary<string, object> extraFields);
    }
}