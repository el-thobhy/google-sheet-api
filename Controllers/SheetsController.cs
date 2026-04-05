using Google.Apis.Sheets.v4.Data;
using GoogleSheetAPI.Helper;
using GoogleSheetAPI.Models.Dtos;
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
            return Ok(db.SystemsWithSentralsAndMachine);
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
