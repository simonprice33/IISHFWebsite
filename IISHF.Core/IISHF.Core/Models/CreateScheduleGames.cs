using System.Text.Json.Serialization;

namespace IISHF.Core.Models
{
    public class CreateScheduleGames : TournamentBaseModel
    {
        public CreateScheduleGames()
        {
            Games = new List<ScheduleGame>();
        }

        [JsonPropertyName("Games")]
        public IEnumerable<ScheduleGame> Games { get; set; }
    }
}
