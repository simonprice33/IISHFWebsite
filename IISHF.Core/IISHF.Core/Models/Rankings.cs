namespace IISHF.Core.Models
{
    public class Rankings : TournamentBaseModel
    {

        public Rankings()
        {
            Ranking = new List<Ranking>();
        }
        public IEnumerable<Ranking> Ranking { get; set; }
    }
}
