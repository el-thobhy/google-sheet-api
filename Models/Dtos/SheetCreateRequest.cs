namespace GoogleSheetAPI.Models.Dtos
{
    public class SheetCreateRequest
    {
        public string SheetName { get; set; }

        public Dictionary<string, object> Data { get; set; }

        public string CreatedBy { get; set; }
    }
}
