using GoogleSheetAPI.Helper;
using GoogleSheetAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace GoogleSheetAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SheetsController : ControllerBase
    {
        private readonly IGoogleSheetsService _sheetsService;

        public SheetsController(IGoogleSheetsService sheetsService)
        {
            _sheetsService = sheetsService;
        }

        [HttpGet("data")]
        public async Task<IActionResult> GetData([FromQuery] string range = "A1:Z100")
        {
            var values = await _sheetsService.GetValuesAsync(range);
            return Ok(values);
        }

        [HttpPost("data")]
        public async Task<IActionResult> AddData([FromBody] JsonElement jsonElement,  // ← Terima sebagai JsonElement dulu
    [FromQuery] string range = "Sheet1!A1")
        {
            var values = JsonHelper.ConvertJsonElementToArray(jsonElement);

            await _sheetsService.AppendValuesAsync(range, values);
            return Ok(new { message = "Data berhasil ditambahkan" });
        }

        [HttpPut("data")]
        public async Task<IActionResult> UpdateData([FromBody] JsonElement jsonElement,  // ← Terima sebagai JsonElement dulu
            [FromQuery] string range = "A1:Z100")
        {
            var data = JsonHelper.ConvertJsonElementToArray(jsonElement);
            await _sheetsService.UpdateValuesAsync(range, data);
            return Ok(new { message = "Data berhasil diupdate" });
        }
    }
}
