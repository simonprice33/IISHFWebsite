using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISHFTest.Core.Models
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
