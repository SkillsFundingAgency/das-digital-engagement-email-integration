using DAS.DigitalEngagement.Application.Services.Interfaces;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DAS.DigitalEngagement.Application.Services
{
    public sealed class JsonToDataTableConverter : IJsonToDataTableConverter
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
                if (!TryExtractAndRepairJson(json, out var repaired))
                    return new DataTable();

                try
                {
                    rootToken = JToken.Parse(repaired);
                }
                catch
                {
                    return new DataTable();
                }
            }

            JArray? valueArray = null;

            if (rootToken.Type == JTokenType.Array)
            {
                valueArray = (JArray)rootToken;
            }
            else if (rootToken.Type == JTokenType.Object)
            {
                var rootObj = (JObject)rootToken;

                // Simplified: only consider "value"
                JToken? candidate = rootObj["value"];
                if (candidate == null)
                    return new DataTable();

                if (candidate.Type == JTokenType.Array)
                    valueArray = (JArray)candidate;
                else if (candidate.Type == JTokenType.Object)
                    valueArray = new JArray { candidate };
                else
                    valueArray = new JArray { candidate };
            }
            else
            {
                return new DataTable();
            }

            if (valueArray == null || valueArray.Count == 0)
                return new DataTable();

            var firstElem = valueArray.FirstOrDefault();
            if (firstElem == null)
                return new DataTable();

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

            var columnTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            foreach (var col in allProps)
                columnTypes[col] = InferColumnType(items, col);

            var table = new DataTable();

            foreach (var col in allProps)
            {
                var type = columnTypes[col] ?? typeof(string);
                var finalType = type.IsValueType && Nullable.GetUnderlyingType(type) == null ? typeof(object) : type;
                table.Columns.Add(col, finalType);
            }

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

        public DataSet ConvertODataPageToDataSet(string json)
        {
            var ds = new DataSet();

            if (string.IsNullOrWhiteSpace(json))
                return ds;

            JToken rootToken;
            try
            {
                rootToken = JToken.Parse(json);
            }
            catch
            {
                if (!TryExtractAndRepairJson(json, out var repaired))
                    return ds;
                try
                {
                    rootToken = JToken.Parse(repaired);
                }
                catch
                {
                    return ds;
                }
            }

            JArray? valueArray = null;
            if (rootToken.Type == JTokenType.Array)
                valueArray = (JArray)rootToken;
            else if (rootToken.Type == JTokenType.Object)
            {
                var rootObj = (JObject)rootToken;

                // Simplified: only consider "value"
                JToken? candidate = rootObj["value"];
                if (candidate == null)
                    return ds;

                if (candidate.Type == JTokenType.Array)
                    valueArray = (JArray)candidate;
                else if (candidate.Type == JTokenType.Object)
                    valueArray = new JArray { candidate };
                else
                    valueArray = new JArray { candidate };
            }
            else
            {
                return ds;
            }

            if (valueArray == null || valueArray.Count == 0)
                return ds;

            var items = valueArray.Children<JObject>().ToList();
            if (items.Count == 0)
                return ds;

            var parentCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var nestedProps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var it in items)
            {
                foreach (var prop in it.Properties())
                {
                    if (prop.Value is JObject || prop.Value is JArray)
                        nestedProps.Add(prop.Name);
                    else
                        parentCols.Add(prop.Name);
                }
            }

            var parent = new DataTable("Root");
            var parentIdCol = new DataColumn("RowId", typeof(int)) { AutoIncrement = true, AutoIncrementSeed = 1, AutoIncrementStep = 1 };
            parent.Columns.Add(parentIdCol);
            foreach (var col in parentCols)
                parent.Columns.Add(col, typeof(object));
            ds.Tables.Add(parent);

            var childTables = new Dictionary<string, DataTable>(StringComparer.OrdinalIgnoreCase);

            foreach (var nested in nestedProps)
            {
                var childName = nested;
                int dup = 1;
                while (ds.Tables.Contains(childName))
                {
                    childName = nested + "_" + dup++;
                }

                var child = new DataTable(childName);
                child.Columns.Add(new DataColumn("ParentRowId", typeof(int)));

                var childProps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var childItems = new List<JObject>();

                foreach (var it in items)
                {
                    var token = it[nested];
                    if (token == null || token.Type == JTokenType.Null) continue;

                    if (token is JObject jo)
                    {
                        childItems.Add(jo);
                        foreach (var p in jo.Properties())
                            childProps.Add(p.Name);
                    }
                    else if (token is JArray ja)
                    {
                        foreach (var el in ja.Children())
                        {
                            if (el is JObject joel)
                            {
                                childItems.Add(joel);
                                foreach (var p in joel.Properties())
                                    childProps.Add(p.Name);
                            }
                            else
                            {
                                childProps.Add("Value");
                            }
                        }
                    }
                    else
                    {
                        childProps.Add("Value");
                    }
                }

                foreach (var c in childProps)
                    child.Columns.Add(c, typeof(object));

                ds.Tables.Add(child);
                childTables[nested] = child;
            }

            var originalParentIds = new List<int>(items.Count);
            foreach (var it in items)
            {
                var parentRow = parent.NewRow();
                foreach (var col in parentCols)
                {
                    var tok = it[col];
                    parentRow[col] = tok == null || tok.Type == JTokenType.Null ? DBNull.Value : ConvertJTokenToClr(tok);
                }
                parent.Rows.Add(parentRow);
                originalParentIds.Add((int)parentRow["RowId"]);
            }

            var parentIdMap = RemoveDuplicateRowsWithMapping(parent, parentIdCol);

            foreach (var nested in nestedProps)
            {
                var child = childTables[nested];
                foreach (var idx in Enumerable.Range(0, items.Count))
                {
                    var it = items[idx];
                    var originalParentId = originalParentIds[idx];
                    if (!parentIdMap.TryGetValue(originalParentId, out var mappedParentId))
                        continue;

                    var token = it[nested];
                    if (token == null || token.Type == JTokenType.Null) continue;

                    if (token is JObject jo)
                    {
                        var crow = child.NewRow();
                        crow["ParentRowId"] = mappedParentId;
                        foreach (DataColumn cc in child.Columns)
                        {
                            if (cc.ColumnName == "ParentRowId") continue;
                            var valTok = jo[cc.ColumnName];
                            crow[cc.ColumnName] = valTok == null || valTok.Type == JTokenType.Null ? DBNull.Value : ConvertJTokenToClr(valTok);
                        }
                        child.Rows.Add(crow);
                    }
                    else if (token is JArray ja)
                    {
                        foreach (var el in ja.Children())
                        {
                            var crow = child.NewRow();
                            crow["ParentRowId"] = mappedParentId;
                            if (el is JObject joel)
                            {
                                foreach (DataColumn cc in child.Columns)
                                {
                                    if (cc.ColumnName == "ParentRowId") continue;
                                    var valTok = joel[cc.ColumnName];
                                    crow[cc.ColumnName] = valTok == null || valTok.Type == JTokenType.Null ? DBNull.Value : ConvertJTokenToClr(valTok);
                                }
                            }
                            else
                            {
                                if (child.Columns.Contains("Value"))
                                {
                                    crow["Value"] = el.Type == JTokenType.Null ? DBNull.Value : ConvertJTokenToClr(el);
                                }
                            }
                            child.Rows.Add(crow);
                        }
                    }
                    else
                    {
                        var crow = child.NewRow();
                        crow["ParentRowId"] = mappedParentId;
                        if (child.Columns.Contains("Value"))
                            crow["Value"] = token.Type == JTokenType.Null ? DBNull.Value : ConvertJTokenToClr(token);
                        child.Rows.Add(crow);
                    }
                }

                RemoveDuplicateRows(child);
            }

            foreach (var kvp in childTables)
            {
                var child = kvp.Value;
                var relationName = $"FK_{parent.TableName}_{child.TableName}";
                var childCol = child.Columns["ParentRowId"];
                if (childCol != null)
                {
                    try
                    {
                        ds.Relations.Add(relationName, parentIdCol, childCol);
                    }
                    catch
                    {
                    }
                }
            }

            return RemoveAllDuplicateRows(ds);
        }

        static object ConvertJTokenToClr(JToken t)
        {
            switch (t.Type)
            {
                case JTokenType.Boolean: return t.ToObject<bool>();
                case JTokenType.Integer: return t.ToObject<long>();
                case JTokenType.Float: return t.ToObject<double>();
                case JTokenType.Date: return t.ToObject<DateTime>();
                case JTokenType.String:
                    var strVal = t.ToObject<string>();
                    return strVal != null ? strVal : DBNull.Value;
                case JTokenType.Null: return DBNull.Value;
                default: return t.Type == JTokenType.Null ? DBNull.Value : t.ToString();
            }
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

            foreach (JsonProperty property in rows.Current.EnumerateObject())
            {
                table.Columns.Add(property.Name);
            }

            AddRow(table, rows.Current);

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
                        stack.Pop();
                    }

                    if (stack.Count == 0)
                    {
                        result = input.Substring(start, i - start + 1);
                        result = CleanJsonString(result);
                        return true;
                    }
                }
            }

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

            s = new string(s.Where(c => !char.IsControl(c) || c == '\r' || c == '\n' || c == '\t').ToArray());
            s = Regex.Replace(s, @",\s*(\]|\})", "$1", RegexOptions.Compiled);

            return s;
        }

        private static object? GetValueForColumn(JObject item, string columnName, Type? desiredType)
        {
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

                return token.ToString();
            }
            catch
            {
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
            return typeof(string);
        }

        private static JToken? GetNestedToken(JObject it, string columnName)
        {
            var parts = columnName.Split('.', 2);
            var top = parts[0];
            var sub = parts[1];
            return it[top]?.Type == JTokenType.Object ? it[top]?[sub] : null;
        }

        private static void RemoveDuplicateRows(DataTable table)
        {
            if (table == null || table.Rows.Count <= 1) return;

            var seen = new HashSet<string>(StringComparer.Ordinal);
            var toRemove = new List<DataRow>(capacity: table.Rows.Count);

            var cols = table.Columns.Cast<DataColumn>().ToArray();

            foreach (DataRow row in table.Rows)
            {
                var key = BuildRowKey(row, cols);
                if (!seen.Add(key))
                {
                    toRemove.Add(row);
                }
            }

            foreach (var r in toRemove)
            {
                try { table.Rows.Remove(r); } catch { }
            }
        }

        private static Dictionary<int, int> RemoveDuplicateRowsWithMapping(DataTable parent, DataColumn parentIdCol)
        {
            var map = new Dictionary<int, int>();
            if (parent == null || parent.Rows.Count == 0) return map;

            var seen = new Dictionary<string, int>(StringComparer.Ordinal);
            var toRemove = new List<DataRow>(capacity: parent.Rows.Count);

            var cols = parent.Columns.Cast<DataColumn>().Where(c => c != parentIdCol).ToArray();

            foreach (DataRow row in parent.Rows)
            {
                var oldId = Convert.ToInt32(row[parentIdCol]);
                var key = BuildRowKey(row, cols);
                if (seen.TryGetValue(key, out var keptId))
                {
                    map[oldId] = keptId;
                    toRemove.Add(row);
                }
                else
                {
                    seen[key] = oldId;
                    map[oldId] = oldId;
                }
            }

            foreach (var r in toRemove)
            {
                try { parent.Rows.Remove(r); } catch { }
            }

            return map;
        }

        private static string BuildRowKey(DataRow row, IEnumerable<DataColumn> columns)
        {
            var sb = new StringBuilder();
            foreach (var c in columns)
            {
                if (sb.Length > 0) sb.Append("||");
                var val = row[c];
                if (val == null || val == DBNull.Value)
                {
                    sb.Append("<NULL>");
                }
                else if (val is DateTime dt)
                {
                    sb.Append(dt.ToString("o", CultureInfo.InvariantCulture));
                }
                else if (val is IFormattable fmt)
                {
                    sb.Append(fmt.ToString(null, CultureInfo.InvariantCulture));
                }
                else
                {
                    sb.Append(val.ToString() ?? string.Empty);
                }
            }
            return sb.ToString();
        }

        private static DataSet RemoveAllDuplicateRows(DataSet ds)
        {
            var result = new DataSet();

            foreach (DataTable table in ds.Tables)
            {
                var newTable = table.Clone();

                foreach (var row in table.AsEnumerable()
                             .GroupBy(r => r["ID"])
                             .Select(g => g.First()))
                {
                    newTable.ImportRow(row);
                }

                result.Tables.Add(newTable);
            }

            return result;
        }
    }
}