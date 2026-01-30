using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.Models.Infrastructure;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAS.DigitalEngagement.Application.Services
{
    public class PayLoadMapper : IPayLoadMapper
    {
        public IList<ExpandoObject> MapToPayload<T>(IList<T> leads)
        {
            ArgumentNullException.ThrowIfNull(leads);
            var result = new List<ExpandoObject>();

            var maps = new List<FieldMap>
                   {
                       new() { Source = "Email",     Target = "Email" },
                       new() { Source = "FirstName", Target = "FirstName" },
                       new() { Source = "LastName",  Target = "LastName" },
                       new() { Source = "LastLogin", Target = "LastSentDate" }
                   };

            // var customFields = new Dictionary<string, object> { ["SubaccountID"] = 2 };

            //return [.. leads
            //    .Where(lead => lead != null)
            //    .Select(lead => AddFields(MapDynamic(lead!, maps), customFields))];

            //result.Add(MapDynamic(lead, maps));

            //return MapDynamic(leads!, maps);
            return [.. leads
                .Where(lead => lead != null)
                .Select(lead => MapDynamic(lead!, maps))];
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

