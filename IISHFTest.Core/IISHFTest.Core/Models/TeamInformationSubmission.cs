using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IISHFTest.Core.Models
{
    public class TeamInformationSubmission : RosterMembers
    {
        [JsonPropertyName("teamHistory")]
        public string TeamHistory { get; set; } = string.Empty;

        [JsonPropertyName("jerseyOne")]
        public string JerseyOne { get; set; }

        [JsonPropertyName("jerseyTwo")]
        public string JerseyTwo { get; set; }

        [JsonPropertyName("status")]
        public bool SubmitToHost { get; set; }
    }
}
