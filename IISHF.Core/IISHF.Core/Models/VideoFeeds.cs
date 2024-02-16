namespace IISHF.Core.Models
{
    public class VideoFeeds : TournamentBaseModel
    {
        public VideoFeeds()
        {
            Feeds = new List<VideoFeed>();
        }

        public IEnumerable<VideoFeed> Feeds { get; set; }
    }
}
