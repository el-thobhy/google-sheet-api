using System.Text.Json;

namespace GoogleSheetAPI.Helper
{
    public static class SheetHelper
    {
        public static IList<IList<object>> ConvertToSheetValues(
            List<Dictionary<string, object>> rows,
            bool includeHeader = true)
        {
            var result = new List<IList<object>>();

            if (rows == null || rows.Count == 0)
                throw new Exception("Rows kosong");

            var headers = rows.First().Keys.ToList();

            if (includeHeader)
                result.Add(headers.Cast<object>().ToList());

            foreach (var row in rows)
            {
                var rowData = new List<object>();

                foreach (var header in headers)
                {
                    object value =
                        row.ContainsKey(header)
                            ? ConvertJsonValue(row[header])
                            : "";

                    rowData.Add(value);
                }

                result.Add(rowData);
            }

            return result;
        }

        public static IList<IList<object>> ConvertRowsWithoutHeader(
            List<Dictionary<string, object>> rows)
        {
            return ConvertToSheetValues(rows, false);
        }

        private static object ConvertJsonValue(object value)
        {
            if (value is JsonElement element)
            {
                return element.ValueKind switch
                {
                    JsonValueKind.String => element.GetString(),

                    JsonValueKind.Number =>
                        element.TryGetInt64(out var l)
                            ? l
                            : element.GetDouble(),

                    JsonValueKind.True => true,

                    JsonValueKind.False => false,

                    JsonValueKind.Null => "",

                    _ => element.ToString()
                };
            }

            return value ?? "";
        }
    }
}
