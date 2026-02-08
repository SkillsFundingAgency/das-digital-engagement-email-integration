using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.Models.Infrastructure;
using System.Dynamic;
using System.Text.Json;

namespace DAS.DigitalEngagement.Application.Services
{
    public class PayLoadMapper : IPayLoadMapper
    {
        public IList<DataMartSettings> _dataMartSettings { get; }

        public PayLoadMapper(IList<DataMartSettings> dataMartSettings)
        {
            _dataMartSettings = dataMartSettings;
        }
    
        public IList<ExpandoObject> MapToPayload<T>(IList<T> leads, string objectName)
        {
            ArgumentNullException.ThrowIfNull(leads);

            var maps = JsonSerializer.Deserialize<List<FieldMap>>(
                           _dataMartSettings?
                               .FirstOrDefault(x => x.ObjectName == objectName)?
                               .FieldMapping ?? "[]"
                       ) ?? new List<FieldMap>();

            return leads
                .Where(lead => !Equals(lead, default(T)))
                .Select(lead => MapDynamic(lead!, maps))
                .ToList();
        }


        public static ExpandoObject MapDynamic(dynamic source, IEnumerable<FieldMap> maps)
        {
            var result = new ExpandoObject();
            var resultDict = (IDictionary<string, object?>)result;

            foreach (var map in maps)
            {
                var value = GetValueByPath(source, map.Source);
                if (value == null) continue;

                resultDict[map.Target] = value;
            }

            return result;
        }

        public static object? GetValueByPath(object source, string path)
        {
            if (source == null || string.IsNullOrWhiteSpace(path))
                return null;

            object? current = source;

            foreach (var segment in path.Split('.'))
            {
                if (current is IDictionary<string, object> dict)
                {
                    if (!dict.TryGetValue(segment, out current))
                        return null;
                }
                else
                {
                    return null;
                }
            }

            return current;
        }

        public ExpandoObject AddFields(ExpandoObject target,IDictionary<string, object> extraFields)
        {
            var dict = (IDictionary<string, object?>)target;

            foreach (var kv in extraFields)
            {
                dict[kv.Key] = kv.Value;
            }

            return target;
        }

    }

}

