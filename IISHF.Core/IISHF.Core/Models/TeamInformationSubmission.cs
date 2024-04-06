using System.Text.Json.Serialization;

namespace IISHF.Core.Models
{
    public class TeamInformationSubmission : RosterMembers
    {
        [JsonPropertyName("teamHistory")]
        public string TeamHistory { get; set; } = string.Empty;

        [JsonPropertyName("jerseyOne")]
        public string JerseyOne { get; set; }

        [JsonPropertyName("jerseyTwo")]
        public string JerseyTwo { get; set; }

        [JsonPropertyName("submitToHost")]
        public bool SubmitToHost { get; set; }

        public string TeamSignatory { get; set; }

        public string IssuingCountry { get; set; }

        public int NmaTeamId { get; set; }

        public Guid nmaTeamKey { get; set; } = Guid.Empty;
    }
}
