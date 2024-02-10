using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IISHFTest.Core.Models
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
        public IEnumerable<RosterMember> ItcRosterMembers { get; set; }
    }
}