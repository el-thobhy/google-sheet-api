namespace GoogleSheetAPI.Models
{
    // Models/DatabaseModels.cs
    public class MasterSystem

    {
        public string Id { get; set; }
        public string SistemName { get; set; }
        public List<MasterSentral> Sentrals { get; set; } = new();
    }

    public class MasterSentral
    {
        public string Id { get; set; }
        public string SystemId { get; set; }  // FK
        public string SentralName { get; set; }
        public List<MasterMachine> Machines { get; set; } = new();
    }

    public class MasterMachine
    {
        public string Id { get; set; }
        public string SentralId { get; set; }  // FK
        public string MachineName { get; set; }
        public string Dmn { get; set; }
    }
    public class DatabaseResult
    {
        public List<MasterSystem> Systems { get; set; }
        public List<MasterSentral> Sentrals { get; set; }
        public List<MasterMachine> Machines { get; set; }

        // Nested/Joined views
        public List<MasterSystem> SystemsWithSentrals { get; set; }
        public List<MasterSystem> FullHierarchy { get; set; }  // Systems → Sentrals → Machines
    }
}
