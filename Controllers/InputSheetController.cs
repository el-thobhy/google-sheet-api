using Google.Apis.Sheets.v4;
using GoogleSheetAPI.Helper;
using GoogleSheetAPI.Models.Dtos;
using GoogleSheetAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace GoogleSheetAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InputSheetController : ControllerBase
    {
        private readonly IGoogleSheetsService _sheetsService;

        public InputSheetController(IGoogleSheetsService sheetsService)
        {
            _sheetsService = sheetsService;
        }

        [HttpGet("data")]
        public async Task<IActionResult> GetData(
            [FromQuery] string sheet = "Sheet1",  // ← Default Sheet1
            [FromQuery] string range = "A1:Z100"
        )
        {
            // Gabungkan sheet + range
            var fullRange = $"{sheet}!{range}";
            var values = await _sheetsService.GetValuesAsync(fullRange);
            return Ok(values);
        }
        [HttpGet("data/by-id")]
        public async Task<IActionResult> GetById(
            string sheetName,
            string keyColumn,
            string keyValue)
        {
            var rowNumber =
                await _sheetsService
                    .FindRowByIdAsync(
                        sheetName,
                        keyColumn,
                        keyValue);

            if (rowNumber == null)
                return NotFound();

            var range =
                $"{sheetName}!A{rowNumber}:Z{rowNumber}";

            var values =
                await _sheetsService
                    .GetValuesAsync(range);

            return Ok(values);
        }
        [HttpPost("data")]
        public async Task<IActionResult> Create(
            [FromBody] SheetCreateRequest request)
        {
            var id = Guid.NewGuid().ToString();

            var now = DateTime.UtcNow;

            var row =
                new Dictionary<string, object>();

            // Insert system columns
            row["Id"] = id;

            foreach (var item in request.Data)
                row[item.Key] = item.Value;

            row["CreatedAt"] = now;
            row["CreatedBy"] = request.CreatedBy;
            row["UpdatedAt"] = "";
            row["UpdatedBy"] = "";
            row["DeletedAt"] = "";
            row["DeletedBy"] = "";
            row["IsDeleted"] = false;

            var range =
                $"{request.SheetName}!A1";

            bool isIncludeHeader = await _sheetsService.HeaderExistsAsync(range);

            var values =
                SheetHelper.ConvertToSheetValues(
                    new List<Dictionary<string, object>>
                    {
                        row
                    }, !isIncludeHeader);


            await _sheetsService.AppendValuesAsync(
                range,
                values);

            // Save Audit
            await SaveAudit(
                request.SheetName,
                id,
                "CREATE",
                null,
                row,
                request.CreatedBy);

            return Ok(new
            {
                Id = id,
                message = "Data berhasil dibuat"
            });
        }

        [HttpPut("data")]
        public async Task<IActionResult> Update(
     [FromBody] SheetUpdateRequest request)
        {
            var rowNumber =
                await _sheetsService
                    .FindRowByIdAsync(
                        request.SheetName,
                        "A",
                        request.Id);

            if (rowNumber == null)
                return NotFound();

            // Ambil data lama
            var oldData =
                await _sheetsService.GetRowAsync(
                    request.SheetName,
                    rowNumber.Value);

            var updatedRow =
                new Dictionary<string, object>();

            updatedRow["Id"] = request.Id;

            foreach (var item in request.Data)
                updatedRow[item.Key] = item.Value;

            updatedRow["UpdatedAt"] = DateTime.UtcNow;
            updatedRow["UpdatedBy"] = request.UpdatedBy;

            var values =
                SheetHelper.ConvertRowsWithoutHeader(
                    new List<Dictionary<string, object>>
                    {
                updatedRow
                    });

            var range =
                $"{request.SheetName}!A{rowNumber}";

            await _sheetsService.UpdateValuesAsync(
                range,
                values);

            await SaveAudit(
                request.SheetName,
                request.Id,
                "UPDATE",
                oldData,
                updatedRow,
                request.UpdatedBy);

            return Ok("Updated");
        }

        [HttpDelete("data")]
        public async Task<IActionResult> Delete(
            string sheetName,
            string id,
            string deletedBy)
        {
            var rowNumber =
                await _sheetsService
                    .FindRowByIdAsync(
                        sheetName,
                        "A",
                        id);

            if (rowNumber == null)
                return NotFound();

            var oldData =
                await _sheetsService.GetRowAsync(
                    sheetName,
                    rowNumber.Value);

            var deleteRow =
                new Dictionary<string, object>();

            foreach (var item in oldData)
                deleteRow[item.Key] = item.Value;

            deleteRow["DeletedAt"] = DateTime.Now;
            deleteRow["DeletedBy"] = deletedBy;
            deleteRow["IsDeleted"] = true;

            var values =
                SheetHelper.ConvertRowsWithoutHeader(
                    new List<Dictionary<string, object>>
                    {
                deleteRow
                    });

            var range =
                $"{sheetName}!A{rowNumber}";

            await _sheetsService.UpdateValuesAsync(
                range,
                values);

            await SaveAudit(
                sheetName,
                id,
                "DELETE",
                oldData,
                deleteRow,
                deletedBy);

            return Ok("Soft deleted");
        }
        private async Task SaveAudit(
            string sheetName,
            string recordId,
            string action,
            object oldData,
            object newData,
            string actionBy)
        {
            var auditSheet =
                $"{sheetName}_Audit";

            var auditRow =
                new Dictionary<string, object>
                {
            { "AuditId", Guid.NewGuid().ToString() },
            { "RecordId", recordId },
            { "Action", action },
            { "OldData",
                JsonSerializer.Serialize(oldData) },
            { "NewData",
                JsonSerializer.Serialize(newData) },
            { "ActionBy", actionBy },
            { "ActionAt", DateTime.UtcNow }
                };
            var range = $"{auditSheet}!A1";
            bool isHeaderExist = await _sheetsService.HeaderExistsAsync(range);
            var values =
                SheetHelper.ConvertToSheetValues(
                    new List<Dictionary<string, object>>
                    {
                auditRow
                    }, !isHeaderExist);

            await _sheetsService.AppendValuesAsync(
                range,
                values);
        }
    }
}
