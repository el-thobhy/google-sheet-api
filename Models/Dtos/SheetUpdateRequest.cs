namespace GoogleSheetAPI.Models.Dtos
{
    public class SheetUpdateRequest
    {
        public string SheetName { get; set; }

        public string Id { get; set; }

        public Dictionary<string, object> Data { get; set; }

        public string UpdatedBy { get; set; }
    }
}
