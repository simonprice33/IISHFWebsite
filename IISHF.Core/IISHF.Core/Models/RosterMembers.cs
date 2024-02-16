using System.Text.Json.Serialization;

namespace IISHF.Core.Models
{
    public class RosterMembers : TournamentBaseModel
    {
        public RosterMembers()
        {
            ItcRosterMembers = new List<RosterMember>();
        }

        [JsonPropertyName("teamName")]
        public string? TeamName { get; set; }

        [JsonPropertyName("itcRosterMembers")]
        public List<RosterMember> ItcRosterMembers { get; set; }
    }
}