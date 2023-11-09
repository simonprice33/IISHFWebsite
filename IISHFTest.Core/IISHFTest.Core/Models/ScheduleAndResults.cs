using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISHFTest.Core.Models
{
    public class ScheduleAndResults
    {
        public int GameNumber { get; set; }

        public string HomeTeam { get; set; }

        public string HomeScore { get; set; }

        public string AwayTeam { get; set; }

        public string AwayScore { get; set; }

        public DateTime GameDateTime { get; set; }

        public string Group { get; set; }

        public string Remarks { get; set; }
    }
}
