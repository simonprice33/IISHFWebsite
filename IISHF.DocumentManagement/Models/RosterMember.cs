using System.Text.Json.Serialization;

namespace IISHF.DocumentManagement.Models
{
    public class RosterMember
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("license")]
        public string License { get; set; } = string.Empty;

        [JsonPropertyName("firstName")]
        public string FirstName { get; set; }

        [JsonPropertyName("lastName")]
        public string LastName { get; set; }

        public string PlayerName => $"{FirstName.Trim()} {LastName.Trim()}";

        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("isBenchOfficial")]
        public bool IsBenchOfficial { get; set; }

        [JsonPropertyName("jerseyNumber")]
        public int? JerseyNumber { get; set; }

        [JsonPropertyName("dateOfBirth")]
        public DateOnly? DateOfBirth { get; set; } = default;
        
        [JsonPropertyName("gender")]
        public string Gender { get; set; }

        [JsonPropertyName("nmaCheck")]
        public bool NmaCheck { get; set; }

        [JsonPropertyName("iishfCheck")]
        public bool IISHFCheck { get; set; }
        
        [JsonPropertyName("comments")]
        public string Comments { get; set; } = string.Empty;

        [JsonPropertyName("nationality")]
        public string Nationality { get; set; }
    }
}
