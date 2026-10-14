// Plan (pseudocode, detailed):
// 1. Try to parse the incoming JSON with `JToken.Parse` (handles array or object roots).
// 2. If parsing fails, attempt to extract and repair a JSON root from the raw string:
//    - Find the first '{' or '[' and then scan forward using a stack to match braces/brackets.
//    - If the end is reached without closing, append the required closing characters in reverse order.
//    - Remove trailing commas before closers (",]" -> "]", ",}" -> "}") and strip control characters.
//    - Attempt to parse the repaired substring.
// 3. If parsing still fails -> return empty `DataTable`.
// 4. If parse succeeds, follow existing logic:
//    - If root is array -> use it.
//    - If root is object -> look for common OData array shapes (`value`, `results`, `d.results`, `d`) and normalize to an array.
//    - If no array candidate -> wrap the single object into an array.
//    - If array elements are primitive -> create single-column table of string values.
//    - Otherwise flatten one-level nested objects to `Parent.Child` columns, collect column names case-insensitively.
//    - Infer types for each column from items (dates, bools, ints, floats, else string).
//    - Build `DataTable`, use `object` for value types so `DBNull` can be stored.
//    - Populate rows converting tokens to the inferred type; null -> `DBNull.Value`.
// 5. Return populated `DataTable`.
// This approach makes the converter resilient to truncated or slightly malformed JSON payloads like the provided example.

using DAS.DigitalEngagement.Application.Services.Interfaces;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DAS.DigitalEngagement.Application.Services
{
    public sealed class JsonToDataTableConverterTest 
    {
        public DataTable ConvertODataPageToDataTable(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new DataTable();

            JToken rootToken;
            try
            {
                rootToken = JToken.Parse(json);
            }
            catch
            {
                // Attempt to repair and extract a JSON root from the raw input
                if (!TryExtractAndRepairJson(json, out var repaired))
                    return new DataTable();

                try
                {
                    rootToken = JToken.Parse(repaired);
                }
                catch
                {
                    // Still invalid -> return empty table
                    return new DataTable();
                }
            }

            // Try to locate an items array in multiple common shapes
            JArray? valueArray = null;

            if (rootToken.Type == JTokenType.Array)
            {
                valueArray = (JArray)rootToken;
            }
            else if (rootToken.Type == JTokenType.Object)
            {
                var rootObj = (JObject)rootToken;

                // Common OData shapes: value, results, d.results
                JToken? candidate = rootObj["value"]
                                ?? rootObj["results"]
                                ?? rootObj["d"]?["results"]
                                ?? rootObj["d"];

                if (candidate == null)
                {
                    // treat single top-level object as one item (resilient to non-OData payloads)
                    valueArray = new JArray { rootObj };
                }
                else if (candidate.Type == JTokenType.Array)
                {
                    valueArray = (JArray)candidate;
                }
                else if (candidate.Type == JTokenType.Object)
                {
                    // single object inside value/d -> wrap it
                    valueArray = new JArray { candidate };
                }
                else
                {
                    // candidate is primitive (e.g., value is a string/number) -> wrap into array of primitive tokens
                    valueArray = new JArray { candidate };
                }
            }
            else
            {
                // Other token types are not supported
                return new DataTable();
            }

            if (valueArray == null || valueArray.Count == 0)
                return new DataTable();

            // Safely obtain the first element to avoid nullable dereference warnings (CS8602)
            var firstElem = valueArray.FirstOrDefault();
            if (firstElem == null)
                return new DataTable();

            // If array elements are not objects, produce single-column table with string values
            if (firstElem.Type != JTokenType.Object)
            {
                var tablePrim = new DataTable();
                tablePrim.Columns.Add("Value", typeof(string));
                foreach (var tok in valueArray)
                {
                    var v = tok.Type == JTokenType.Null ? DBNull.Value : (object)tok.ToString();
                    var row = tablePrim.NewRow();
                    row["Value"] = v;
                    tablePrim.Rows.Add(row);
                }
                return tablePrim;
            }

            var items = valueArray.Children<JObject>().ToList();
            if (items.Count == 0)
                return new DataTable();

            // Determine all property names (flat properties only)
            var allProps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var it in items)
            {
                foreach (var prop in it.Properties())
                {
                    if (prop.Value is JObject nested)
                    {
                        foreach (var np in nested.Properties())
                        {
                            allProps.Add($"{prop.Name}.{np.Name}");
                        }
                    }
                    else
                    {
                        allProps.Add(prop.Name);
                    }
                }
            }

            // Type inference per column
            var columnTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            foreach (var col in allProps)
                columnTypes[col] = InferColumnType(items, col);

            // Build DataTable
            var table = new DataTable();

            foreach (var col in allProps)
            {
                var type = columnTypes[col] ?? typeof(string);
                var finalType = type.IsValueType && Nullable.GetUnderlyingType(type) == null ? typeof(object) : type;
                table.Columns.Add(col, finalType);
            }

            // Populate rows
            foreach (var it in items)
            {
                var row = table.NewRow();
                foreach (DataColumn col in table.Columns)
                {
                    object? val = GetValueForColumn(it, col.ColumnName, columnTypes.TryGetValue(col.ColumnName, out var t) ? t : null);
                    row[col.ColumnName] = val ?? DBNull.Value;
                }
                table.Rows.Add(row);
            }

            return table;
        }

        public DataTable JsonToDataTable(string json)
        {
            var table = new DataTable();

            using JsonDocument document = JsonDocument.Parse(json);

            if (document.RootElement.ValueKind != JsonValueKind.Array)
                throw new ArgumentException("JSON must be an array.");

            var rows = document.RootElement.EnumerateArray();

            if (!rows.MoveNext())
                return table;

            // Create columns from the first object
            foreach (JsonProperty property in rows.Current.EnumerateObject())
            {
                table.Columns.Add(property.Name);
            }

            // Add first row
            AddRow(table, rows.Current);

            // Add remaining rows
            while (rows.MoveNext())
            {
                AddRow(table, rows.Current);
            }

            return table;
        }

        private static void AddRow(DataTable table, JsonElement jsonObject)
        {
            DataRow row = table.NewRow();

            foreach (JsonProperty property in jsonObject.EnumerateObject())
            {
                row[property.Name] = property.Value.ValueKind == JsonValueKind.Null
                    ? DBNull.Value
                    : property.Value.ToString();
            }

            table.Rows.Add(row);
        }

        private static bool TryExtractAndRepairJson(string input, out string result)
        {
            result = string.Empty;
            if (string.IsNullOrEmpty(input)) return false;

            // Find first '{' or '['
            int firstObj = input.IndexOf('{');
            int firstArr = input.IndexOf('[');
            int start = -1;
            char openChar;

            if (firstArr >= 0 && (firstArr < firstObj || firstObj == -1))
            {
                start = firstArr;
                openChar = '[';
            }
            else if (firstObj >= 0)
            {
                start = firstObj;
                openChar = '{';
            }
            else
            {
                return false;
            }

            var stack = new Stack<char>();
            var closers = new Dictionary<char, char> { { '{', '}' }, { '[', ']' } };
            int i = start;
            int len = input.Length;
            for (; i < len; i++)
            {
                var c = input[i];
                if (c == '{' || c == '[')
                {
                    stack.Push(c);
                }
                else if (c == '}' || c == ']')
                {
                    if (stack.Count == 0) continue;
                    var top = stack.Peek();
                    if (top == '{' && c == '}' || top == '[' && c == ']')
                        stack.Pop();
                    else
                    {
                        // mismatched, attempt to recover by popping
                        stack.Pop();
                    }

                    if (stack.Count == 0)
                    {
                        // found matching root close
                        result = input.Substring(start, i - start + 1);
                        result = CleanJsonString(result);
                        return true;
                    }
                }
            }

            // If we get here, input ended before closing all opens -> append needed closers
            if (stack.Count > 0)
            {
                var sb = new System.Text.StringBuilder();
                sb.Append(input.Substring(start));
                while (stack.Count > 0)
                {
                    var open = stack.Pop();
                    sb.Append(closers[open]);
                }
                result = CleanJsonString(sb.ToString());
                return true;
            }

            return false;
        }

        private static string CleanJsonString(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;

            // Remove common control characters except valid whitespace
            s = new string(s.Where(c => !char.IsControl(c) || c == '\r' || c == '\n' || c == '\t').ToArray());

            // Remove trailing commas before closing brackets/braces (",]" or ",}")
            s = Regex.Replace(s, @",\s*(\]|\})", "$1", RegexOptions.Compiled);

            return s;
        }

        private static object? GetValueForColumn(JObject item, string columnName, Type? desiredType)
        {
            // Handle flattened nested properties like "Campaign.Name"
            if (columnName.Contains('.'))
            {
                var parts = columnName.Split('.', 2);
                var top = parts[0];
                var sub = parts[1];
                var tok = item[top]?.Type == JTokenType.Object ? item[top]?[sub] : null;
                return ConvertToken(tok, desiredType);
            }

            var token = item[columnName];
            return ConvertToken(token, desiredType);
        }

        private static object? ConvertToken(JToken? token, Type? desiredType)
        {
            if (token == null || token.Type == JTokenType.Null) return null;

            try
            {
                if (desiredType == typeof(DateTime) || desiredType == typeof(DateTime?))
                {
                    var s = token.ToString();
                    if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt))
                        return dt;
                    return null;
                }

                if (desiredType == typeof(bool) || desiredType == typeof(bool?))
                    return token.Type == JTokenType.Boolean ? token.ToObject<bool>() : bool.TryParse(token.ToString(), out var b) ? b : (bool?)null;

                if (desiredType == typeof(long) || desiredType == typeof(long?))
                {
                    if (token.Type == JTokenType.Integer) return token.ToObject<long>();
                    if (long.TryParse(token.ToString(), out var l)) return l;
                    if (double.TryParse(token.ToString(), out var dd)) return (long)dd;
                    return null;
                }

                if (desiredType == typeof(double) || desiredType == typeof(double?))
                {
                    if (token.Type == JTokenType.Float || token.Type == JTokenType.Integer) return token.ToObject<double>();
                    if (double.TryParse(token.ToString(), out var d)) return d;
                    return null;
                }

                // Default -> string
                return token.ToString();
            }
            catch
            {
                // defensive fallback
                return token.ToString();
            }
        }

        private static Type InferColumnType(IList<JObject> items, string columnName)
        {
            bool seenDate = false, seenBool = false, seenInteger = false, seenFloat = false, seenString = false;
            foreach (var it in items)
            {
                var token = columnName.Contains('.') ? GetNestedToken(it, columnName) : it[columnName];
                if (token == null || token.Type == JTokenType.Null) continue;

                if (token.Type == JTokenType.Boolean) { seenBool = true; continue; }
                if (token.Type == JTokenType.Integer) { seenInteger = true; continue; }
                if (token.Type == JTokenType.Float) { seenFloat = true; continue; }
                if (token.Type == JTokenType.Date) { seenDate = true; continue; }

                // attempt parse for date, int, double, bool if string token
                var s = token.ToString();
                if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out _)) { seenDate = true; continue; }
                if (bool.TryParse(s, out _)) { seenBool = true; continue; }
                if (long.TryParse(s, out _)) { seenInteger = true; continue; }
                if (double.TryParse(s, out _)) { seenFloat = true; continue; }
                seenString = true;
            }

            if (seenDate && !seenString) return typeof(DateTime);
            if (seenBool && !seenString && !seenDate) return typeof(bool);
            if (seenFloat && !seenString) return typeof(double);
            if (seenInteger && !seenFloat && !seenString) return typeof(long);
            // fallback to string/object
            return typeof(string);
        }

        private static JToken? GetNestedToken(JObject it, string columnName)
        {
            var parts = columnName.Split('.', 2);
            var top = parts[0];
            var sub = parts[1];
            return it[top]?.Type == JTokenType.Object ? it[top]?[sub] : null;
        }
    }
}