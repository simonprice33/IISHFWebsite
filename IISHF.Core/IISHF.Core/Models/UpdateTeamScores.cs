using System.Text.Json.Serialization;

namespace IISHF.Core.Models
{
    public class UpdateTeamScores : TournamentBaseModel
    {
        public UpdateTeamScores()
        {
            Scores = new List<UpdateTeamScore>();
        }

        [JsonPropertyName("Games")]
        public IEnumerable<UpdateTeamScore> Scores { get; set; }
    }
}
