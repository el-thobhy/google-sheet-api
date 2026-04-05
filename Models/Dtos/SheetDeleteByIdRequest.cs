namespace GoogleSheetAPI.Models.Dtos
{
    public class SheetDeleteByIdRequest
    {
        public string SheetName { get; set; }

        public string KeyColumn { get; set; } = "A";

        public string KeyValue { get; set; }
    }
}
