
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using GoogleSheetAPI.Models;

namespace GoogleSheetAPI.Services
{
    public class GoogleSheetsService : IGoogleSheetsService
    {
        private readonly SheetsService _sheetsService;
        private readonly string _spreadsheetId;
        public GoogleSheetsService(IConfiguration config)
        {
            var credential = GoogleCredential.FromFile(config["GoogleSheets:CredentialsPath"]);
            _sheetsService = new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "GoogleSheetApi"
            });
            _spreadsheetId = config["GoogleSheets:SpreadsheetId"];
        }

        public async Task AppendValuesAsync(string range, IList<IList<object>> values)
        {
            var valueRange = new ValueRange { Values = values };
            var request = _sheetsService.Spreadsheets.Values.Append(
                valueRange, _spreadsheetId, range);
            request.ValueInputOption = SpreadsheetsResource.ValuesResource
                .AppendRequest.ValueInputOptionEnum.USERENTERED;
            await request.ExecuteAsync();
        }

        public async Task<IList<IList<object>>> GetValuesAsync(string range)
        {
            var request = _sheetsService.Spreadsheets.Values.Get(_spreadsheetId, range);
            var response = await request.ExecuteAsync();
            return response.Values ?? new List<IList<object>>();
        }

        public async Task UpdateValuesAsync(string range, IList<IList<object>> values)
        {
            var valueRange = new ValueRange { Values = values };
            var request = _sheetsService.Spreadsheets.Values.Update(
                valueRange, _spreadsheetId, range);
            request.ValueInputOption = SpreadsheetsResource.ValuesResource
                .UpdateRequest.ValueInputOptionEnum.USERENTERED;
            await request.ExecuteAsync();
        }
        public async Task<DatabaseResult> GetDatabaseAsync()
        {
            // Baca 3 sheet paralel
            var systemsTask = GetValuesAsync("master_system!A1:B100");
            var sentralsTask = GetValuesAsync("master_sentral!A1:C100");
            var machinesTask = GetValuesAsync("master_machine!A1:D200");

            await Task.WhenAll(systemsTask, sentralsTask, machinesTask);

            // Parse ke model
            var systems = ParseSystems(systemsTask.Result);
            var sentrals = ParseSentrals(sentralsTask.Result);
            var machines = ParseMachines(machinesTask.Result);

            // Build relationships
            var result = new DatabaseResult
            {
                Systems = systems,
                Sentrals = sentrals,
                Machines = machines,

                // Flat join: Systems dengan list Sentrals-nya
                SystemsWithSentrals = systems.Select(s => new MasterSystem
                {
                    Id = s.Id,
                    SistemName = s.SistemName,
                    Sentrals = sentrals.Where(x => x.SystemId == s.Id).ToList()
                }).ToList(),

                // Full hierarchy: 3 level nested
                FullHierarchy = systems.Select(s => new MasterSystem
                {
                    Id = s.Id,
                    SistemName = s.SistemName,
                    Sentrals = sentrals
                        .Where(x => x.SystemId == s.Id)
                        .Select(se => new MasterSentral
                        {
                            Id = se.Id,
                            SystemId = se.SystemId,
                            SentralName = se.SentralName,
                            Machines = machines.Where(m => m.SentralId == se.Id).ToList()
                        }).ToList()
                }).ToList()
            };

            return result;
        }

        // Helper parsers
        private List<MasterSystem> ParseSystems(IList<IList<object>> data) =>
            data?.Select(row => new MasterSystem
            {
                Id = row[0]?.ToString(),
                SistemName = row[1]?.ToString()
            }).Skip(1).ToList() ?? new();

        private List<MasterSentral> ParseSentrals(IList<IList<object>> data) =>
            data?.Select(row => new MasterSentral
            {
                Id = row[0]?.ToString(),
                SystemId = row[1]?.ToString(),  // id_system = FK
                SentralName = row[2]?.ToString()
            }).Skip(1).ToList() ?? new();

        private List<MasterMachine> ParseMachines(IList<IList<object>> data) =>
            data?.Select(row => new MasterMachine
            {
                Id = row[0]?.ToString(),
                SentralId = row[1]?.ToString(),  // id_sentral = FK
                MachineName = row[2]?.ToString(),
                Dmn = row[3]?.ToString()
            }).Skip(1).ToList() ?? new();
    }
}
