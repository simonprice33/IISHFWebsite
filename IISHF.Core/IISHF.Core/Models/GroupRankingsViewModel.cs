namespace IISHF.Core.Models
{
    public class GroupRankingsViewModel
    {
        public GroupRankingsViewModel()
        {
            Rankings = new List<RankingViewModel>();
        }

        public IEnumerable<RankingViewModel> Rankings { get; set; }
    }
}
