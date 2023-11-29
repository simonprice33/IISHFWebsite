using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISHFTest.Core.Models
{
    public class UpdateTeamScore 
    {
        public int GameNumber { get; set; }

        public int HomeScore { get; set; }

        public int AwayScore { get; set; }
    }
}
