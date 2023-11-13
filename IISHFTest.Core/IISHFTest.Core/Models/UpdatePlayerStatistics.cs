using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISHFTest.Core.Models
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
