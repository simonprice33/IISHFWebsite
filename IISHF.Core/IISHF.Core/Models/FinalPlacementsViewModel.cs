namespace IISHF.Core.Models
{
    public class FinalPlacementsViewModel
    {

        public FinalPlacementsViewModel()
        {
            TeamPlacements = new List<TeamPlacement>();
        }
        public IEnumerable<TeamPlacement> TeamPlacements { get; set; }
    }
}
