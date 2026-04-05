using GoogleSheetAPI.Models;

namespace GoogleSheetAPI.Services
{
    public interface IGoogleSheetsService
    {
        Task<DatabaseResult> GetDatabaseAsync();  // ← Baru
        Task<IList<IList<object>>> GetValuesAsync(string range);

        Task AppendValuesAsync(
            string range,
            IList<IList<object>> values);

        Task UpdateValuesAsync(
            string range,
            IList<IList<object>> values);

        Task<bool> HeaderExistsAsync(string range);

        Task<int?> FindRowByIdAsync(
            string sheetName,
            string keyColumn,
            string keyValue);

        Task DeleteRowAsync(
            string sheetName,
            int rowNumber);

        Task<Dictionary<string, object>> GetRowAsync(
            string sheetName,
            int rowNumber);
    }
}
