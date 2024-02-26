namespace IISHF.Core.Models
{
    public class UpdatePlayerStatistics : TournamentBaseModel
    {
        public UpdatePlayerStatistics()
        {
            PlayerStatistics = new List<PlayerStatistics>();
        }

        public IEnumerable<PlayerStatistics> PlayerStatistics { get; set; }
    }
}
