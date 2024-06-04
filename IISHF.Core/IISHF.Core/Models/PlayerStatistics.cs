using System.Text.Json.Serialization;

namespace IISHF.Core.Models
{
    public class PlayerStatistics
    {
        public string TeamName { get; set; } = string.Empty;

        public string License { get; set; } = string.Empty;

        public string PlayerName { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public bool IsBenchOfficial { get; set; }

        public int JerseyNumber { get; set; }

        [JsonPropertyName("Games played")]
        public int GamesPlayed { get; set; }

        public int Goals { get; set; } = 0;

        public int Assists { get; set; } = 0;

        public int Total => Goals + Assists;

        public decimal Penalties { get; set; } = 0m;

        public DateOnly DateOfBirth { get; set; }

        public bool NmaCheck { get; set; }

        public bool IISHFCheck { get; set; }

        public string Comments { get; set; } = string.Empty;

        public string TeamLogoUrl { get; set; } = string.Empty;
    }
}
