namespace IISHF.Core.Models
{
    public class VideoFeed
    {
        public Uri FeedUri { get; set; }

        public string Title { get; set; } = string.Empty;

        public int FeedOrder { get; set; }

        public DateTime LIveFeedDateTimeUtc { get; set; }
    }
}
