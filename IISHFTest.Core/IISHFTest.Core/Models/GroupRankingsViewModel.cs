using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISHFTest.Core.Models
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
