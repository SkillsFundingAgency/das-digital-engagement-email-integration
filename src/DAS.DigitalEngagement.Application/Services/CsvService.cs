using System.Globalization;
using CsvHelper;

namespace DAS.DigitalEngagement.Application.Services
{
    public class CsvService : ICsvService
    {
        //public CsvValidationeResult Validate(StreamReader csvStream, IList<string> fields)
        //{
        //    var config = new ValidatorConfiguration();

        //    config.ColumnSeperator = ",";
        //    config.HasHeaderRow = true;
        //    config.RowSeperator = Environment.NewLine;

        //    for (int i = 0; i < fields.Count; i++)
        //    {
        //        config.Columns.Add(i, new ColumnValidatorConfiguration()
        //        {
        //            Name = fields[i],
        //            IsNumeric = false,
        //            Unique = true
        //        });
        //    }

        //    Validator validator = Validator.FromConfiguration(config);
        //    var sourceReader = new StreamSourceReader(csvStream.BaseStream);

        //    var validationResult = new CsvValidationeResult
        //    {
        //        Errors = validator.Validate(sourceReader).ToList()
        //    };




        //    return validationResult;
        //}

        //public async Task<IList<dynamic>> ConvertToList(StreamReader personCsv)
        //{
        //    personCsv.DiscardBufferedData();
        //    personCsv.BaseStream.Seek(0, System.IO.SeekOrigin.Begin);

        //    using (var csv = new CsvReader(personCsv, CultureInfo.InvariantCulture))
        //    {
        //        var records = csv.GetRecords<dynamic>();
        //        return records.ToList<dynamic>();
        //    }
        //}

        public int GetByteCount<T>(IList<T> leads)
        {
            var csvString = ToCsv(leads);

            return System.Text.Encoding.Unicode.GetByteCount(csvString);
        }

        public string ToCsv<T>(IList<T> leads)
        {
            if (typeof(T) == typeof(System.Dynamic.ExpandoObject) ||
                    typeof(IDictionary<string, object?>).IsAssignableFrom(typeof(T)))
            {
                var dicts = leads.Cast<System.Dynamic.ExpandoObject>()
                                 .Select(e => (IDictionary<string, object?>)e)
                                 .ToList();
                if (!dicts.Any()) return string.Empty;

                var headers = new List<string>();
                foreach (var d in dicts)
                {
                    foreach (var k in d.Keys)
                    {
                        if (!headers.Contains(k))
                            headers.Add(k);
                    }
                }

                using var writer = new StringWriter();
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

                foreach (var h in headers) csv.WriteField(h);
                csv.NextRecord();

                foreach (var row in dicts)
                {
                    foreach (var h in headers)
                    {
                        row.TryGetValue(h, out var value);
                        csv.WriteField(value);
                    }
                    csv.NextRecord();
                }

                writer.Flush();
                return writer.ToString();
            }

            using (var writer = new StringWriter())
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                // csv.Context.RegisterClassMap<PersonMap>();

                csv.WriteRecords(leads);

                writer.Flush();
                return writer.ToString();
            }
        }

        public bool IsEmpty(StreamReader stream)
        {

            stream.DiscardBufferedData();
            stream.BaseStream.Seek(0, System.IO.SeekOrigin.Begin);

            if (stream.BaseStream.Length < 2)
            {
                return true;
            }

            return String.IsNullOrWhiteSpace(stream.Peek().ToString());
        }

        public bool HasData(StreamReader stream)
        {
            stream.DiscardBufferedData();
            stream.BaseStream.Seek(0, System.IO.SeekOrigin.Begin);
            stream.ReadLine();

            //if there is data and not just headers, the second line should have data and shouldnt be whitespace

            var secondLine = stream.ReadLine();

            return String.IsNullOrWhiteSpace(secondLine) == false;
        }

        public  Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        //private sealed class PersonMap : ClassMap<Person>
        //{
        //    public PersonMap()
        //    {
        //        AutoMap(CultureInfo.InvariantCulture);
        //        Map(m => m.EmployerUserId).Name("esfaEmployerUserId");
        //    }
        //}
    }
}