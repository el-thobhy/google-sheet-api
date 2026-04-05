namespace GoogleSheetAPI.Models.Dtos
{
    public class SheetAppendRequest
    {
        public string SheetName { get; set; } = "Sheet1";

        public List<Dictionary<string, object>> Rows { get; set; }
            = new();
    }
}
