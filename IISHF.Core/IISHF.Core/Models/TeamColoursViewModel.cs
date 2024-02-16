namespace IISHF.Core.Models
{
    public class TeamColoursViewModel
    {
        public TeamColoursViewModel()
        {
            TeamColours = new List<ColourViewModel>();
        }

        public List<ColourViewModel> TeamColours { get; set; }
    }
}
