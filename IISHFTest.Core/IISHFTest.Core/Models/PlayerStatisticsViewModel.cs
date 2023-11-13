using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISHFTest.Core.Models
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
