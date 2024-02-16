namespace IISHF.Core.Models
{
    public class TeamPlacements : TournamentBaseModel
    {
        public TeamPlacements()
        {
            Placements = new List<TeamPlacement>();
        }

        public IEnumerable<TeamPlacement> Placements { get; set; }
    }
}
