using System.Text.Json.Serialization;

namespace IISHF.Core.Models
{
    public class ScheduleGame
    {
        [JsonPropertyName("Home Team")]
        public string HomeTeam { get; set; }

        [JsonPropertyName("Away Team")]
        public string AwayTeam { get; set; }

        public int? GameNumber { get; set; }

        public DateTime? GameDateTime { get; set; }

        public string? Group { get; set; } = string.Empty;

        public string? Remarks { get; set; }
    }
}
