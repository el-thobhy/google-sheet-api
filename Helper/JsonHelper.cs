using System.Text.Json;

namespace GoogleSheetAPI.Helper
{
    public static class JsonHelper
    {
        // Helper: Konversi JsonElement → IList<IList<object>>
        public static IList<IList<object>> ConvertJsonElementToArray(JsonElement element)
        {
            var result = new List<IList<object>>();

            if (element.ValueKind != JsonValueKind.Array)
                throw new Exception("Body harus berupa array 2D");

            foreach (var row in element.EnumerateArray())
            {
                var rowList = new List<object>();

                if (row.ValueKind != JsonValueKind.Array)
                    throw new Exception("Setiap row harus berupa array");

                foreach (var cell in row.EnumerateArray())
                {
                    // Konversi JsonElement ke primitive value
                    rowList.Add(ConvertJsonValue(cell));
                }

                result.Add(rowList);
            }

            return result;
        }

        private static object ConvertJsonValue(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.GetDouble(), // atau GetInt64()
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => element.ToString() // Fallback
            };
        }
    }
}
