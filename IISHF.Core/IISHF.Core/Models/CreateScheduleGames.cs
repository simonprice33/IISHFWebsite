namespace IISHF.Core.Models
{
    public class CreateScheduleGames : TournamentBaseModel
    {
        public CreateScheduleGames()
        {
            Games = new List<ScheduleGame>();
        }

        public IEnumerable<ScheduleGame> Games { get; set; }
    }
}
