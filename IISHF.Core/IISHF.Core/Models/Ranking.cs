using System.Text.Json.Serialization;

namespace IISHF.Core.Models
{
    public class Ranking
    {
        [JsonPropertyName("Team")]
        public string? TeamName { get; set; }

        public string? Games { get; set; }

        public int Won { get; set; } = 0;

        public int Tied { get; set; } = 0;

        public int Lost { get; set; } = 0;

        [JsonPropertyName("Goals +")]
        public int? GoalsFor { get; set; }

        [JsonPropertyName("Goals -")]
        public int? GoalsAgainst { get; set; }

        public int? Diff { get; set; }

        public int? Points { get; set; }

        public decimal? TieWeight { get; set; }

        public int Place { get; set; } = 0;
    }
}
