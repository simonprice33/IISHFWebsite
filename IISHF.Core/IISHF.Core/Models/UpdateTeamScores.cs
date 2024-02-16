namespace IISHF.Core.Models
{
    public class UpdateTeamScores : TournamentBaseModel
    {
        public UpdateTeamScores()
        {
            Scores = new List<UpdateTeamScore>();
        }

        public IEnumerable<UpdateTeamScore> Scores { get; set; }
    }
}
