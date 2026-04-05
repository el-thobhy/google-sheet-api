
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using GoogleSheetAPI.Models;
using System.Text;

namespace GoogleSheetAPI.Services
{
    public class GoogleSheetsService : IGoogleSheetsService
    {
        private readonly SheetsService _sheetsService;
        private readonly string _spreadsheetId;
        public GoogleSheetsService(IConfiguration config)
        {
            var jsonCred = Encoding.UTF8.GetString(Convert.FromBase64String(config["GoogleSheets:CredentialString"]));
            var credential = GoogleCredential.FromJson(jsonCred);
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

                // Flat join: Systems dengan list Sentrals-nya
                SystemsWithSentralsAndMachine = systems.Select(s => new MasterSystem
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

        public async Task<bool> HeaderExistsAsync(string range)
        {
            var request = _sheetsService.Spreadsheets.Values.Get(
               _spreadsheetId,
               range);

            var response = await request.ExecuteAsync();

            return response.Values != null &&
                   response.Values.Count > 0;
        }
        public async Task<int?> FindRowByIdAsync(
            string sheetName,
            string keyColumn,
            string keyValue)
        {
            var range = $"{sheetName}!{keyColumn}:{keyColumn}";

            var request =
                _sheetsService.Spreadsheets.Values.Get(
                    _spreadsheetId,
                    range);

            var response =
                await request.ExecuteAsync();

            if (response.Values == null)
                return null;

            for (int i = 0; i < response.Values.Count; i++)
            {
                var value =
                    response.Values[i][0]?.ToString();

                if (value == keyValue)
                    return i + 1;
            }

            return null;
        }
        public async Task DeleteRowAsync(
            string sheetName,
            int rowNumber)
        {
            var deleteRequest =
                new DeleteDimensionRequest
                {
                    Range = new DimensionRange
                    {
                        SheetId = await GetSheetId(sheetName),
                        Dimension = "ROWS",
                        StartIndex = rowNumber - 1,
                        EndIndex = rowNumber
                    }
                };

            var batchRequest =
                new BatchUpdateSpreadsheetRequest
                {
                    Requests =
                        new List<Request>
                        {
                    new Request
                    {
                        DeleteDimension =
                            deleteRequest
                    }
                        }
                };

            await _sheetsService.Spreadsheets.BatchUpdate(
                batchRequest,
                _spreadsheetId)
                .ExecuteAsync();
        }
        private async Task<int> GetSheetId(string sheetName)
        {
            var spreadsheet =
                await _sheetsService.Spreadsheets.Get(_spreadsheetId)
                    .ExecuteAsync();

            var sheet =
                spreadsheet.Sheets
                    .FirstOrDefault(s =>
                        s.Properties.Title == sheetName);

            if (sheet == null)
                throw new Exception(
                    $"Sheet '{sheetName}' tidak ditemukan");

            return sheet.Properties.SheetId.Value;
        }

        public async Task<Dictionary<string, object>> GetRowAsync(
     string sheetName,
     int rowNumber)
        {
            // Ambil header dulu
            var headerRange = $"{sheetName}!A1:Z1";

            var headerResponse =
                await _sheetsService.Spreadsheets.Values
                    .Get(_spreadsheetId, headerRange)
                    .ExecuteAsync();

            var headers =
                headerResponse.Values?.FirstOrDefault();

            if (headers == null)
                return null;

            // Ambil row
            var rowRange =
                $"{sheetName}!A{rowNumber}:Z{rowNumber}";

            var rowResponse =
                await _sheetsService.Spreadsheets.Values
                    .Get(_spreadsheetId, rowRange)
                    .ExecuteAsync();

            var rowValues =
                rowResponse.Values?.FirstOrDefault();

            if (rowValues == null)
                return null;

            var result =
                new Dictionary<string, object>();

            for (int i = 0; i < headers.Count; i++)
            {
                var header =
                    headers[i].ToString();

                var value =
                    i < rowValues.Count
                        ? rowValues[i]
                        : "";

                result[header] = value;
            }

            return result;
        }
    }
}
