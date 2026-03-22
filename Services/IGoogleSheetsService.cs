namespace GoogleSheetAPI.Services
{
    public interface IGoogleSheetsService
    {
        Task<IList<IList<object>>> GetValuesAsync(string range);
        Task AppendValuesAsync(string range, IList<IList<object>> values);
        Task UpdateValuesAsync(string range, IList<IList<object>> values);
    }
}
