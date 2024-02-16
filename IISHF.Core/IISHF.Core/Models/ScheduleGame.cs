namespace IISHF.Core.Models
{
    public class ScheduleGame 
    {
        public string? HomeTeam { get; set; }

        public string? AwayTeam { get; set; }

        public int? GameNumber { get; set; }

        public DateTime? GameDateTime { get; set; }

        public string? Group { get; set; } = string.Empty;

        public string? Remarks { get; set; }
    }
}
