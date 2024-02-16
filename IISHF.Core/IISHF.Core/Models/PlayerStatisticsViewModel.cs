namespace IISHF.Core.Models
{
    public class PlayerStatisticsViewModel
    {
        public PlayerStatisticsViewModel()
        {
            PlayerStatistics = new List<PlayerStatistics>();
        }

        public List<PlayerStatistics> PlayerStatistics { get; set; }
    }
}
