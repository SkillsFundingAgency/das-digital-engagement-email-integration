using System.Globalization;
using CsvHelper;
using DAS.DigitalEngagement.Application.Services.Interfaces;

namespace DAS.DigitalEngagement.Application.Services
{
    public class CsvService : ICsvService
    {
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
                    foreach (var k in d.Keys.Where(k => !headers.Contains(k)))
                    {
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
                        csv.WriteField(row.TryGetValue(h, out var value) ? value : null);
                    csv.NextRecord();
                }

                writer.Flush();
                return writer.ToString();
            }

            using var writer2 = new StringWriter();
            using var csv2 = new CsvWriter(writer2, CultureInfo.InvariantCulture);
            csv2.WriteRecords(leads);
            writer2.Flush();
            return writer2.ToString();
        }

        public static bool IsEmpty(StreamReader stream)
        {

            stream.DiscardBufferedData();
            stream.BaseStream.Seek(0, System.IO.SeekOrigin.Begin);

            if (stream.BaseStream.Length < 2)
            {
                return true;
            }

            return String.IsNullOrWhiteSpace(stream.Peek().ToString());
        }

        public static bool HasData(StreamReader stream)
        {
            stream.DiscardBufferedData();
            stream.BaseStream.Seek(0, System.IO.SeekOrigin.Begin);
            stream.ReadLine();
            return !string.IsNullOrWhiteSpace(stream.ReadLine());
        }

        public Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            using var writer = new StreamWriter(stream, leaveOpen: true);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}