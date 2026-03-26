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

        [HttpPost("data")]
        public async Task<IActionResult> AddData([FromBody] JsonElement jsonElement,  // ← Terima sebagai JsonElement dulu
            [FromQuery] string range = "Sheet1!A1")
        {
            var values = JsonHelper.ConvertJsonElementToArray(jsonElement);

            await _sheetsService.AppendValuesAsync(range, values);
            return Ok(new { message = "Data berhasil ditambahkan" });
        }

        [HttpPut("data")]
        public async Task<IActionResult> UpdateData(
            [FromBody] JsonElement jsonElement,  // ← Terima sebagai JsonElement dulu
            [FromQuery] string range = "A1:Z100"
        )
        {
            var data = JsonHelper.ConvertJsonElementToArray(jsonElement);
            await _sheetsService.UpdateValuesAsync(range, data);
            return Ok(new { message = "Data berhasil diupdate" });
        }

        [HttpGet("database")]
        public async Task<IActionResult> GetDatabase()
        {
            var db = await _sheetsService.GetDatabaseAsync();

            return Ok(new
            {
                systems = db.Systems,
                sentrals = db.Sentrals,
                machines = db.Machines
            });
        }

        [HttpGet("all-system")]
        public async Task<IActionResult> GetAllSystem()
        {
            var db = await _sheetsService.GetDatabaseAsync();
            return Ok(db.Systems);
        }

        [HttpGet("systems/{id}")]
        public async Task<IActionResult> GetSystemById(string id)
        {
            var db = await _sheetsService.GetDatabaseAsync();
            var system = db.FullHierarchy.FirstOrDefault(s => s.Id == id);

            if (system == null) return NotFound(new { message = "System not found" });

            return Ok(system);
        }

        [HttpGet("sentrals/{id}/machines")]
        public async Task<IActionResult> GetSentralMachines(string id)
        {
            var db = await _sheetsService.GetDatabaseAsync();
            var sentral = db.Sentrals.FirstOrDefault(s => s.Id == id);

            if (sentral == null) return NotFound(new { message = "Sentral not found" });

            var machines = db.Machines.Where(m => m.SentralId == id).ToList();

            return Ok(new
            {
                sentral = sentral,
                machines = machines,
                machineCount = machines.Count,
                totalDmn = machines.Sum(m => double.TryParse(m.Dmn, out var d) ? d : 0)
            });
        }
    }
}
