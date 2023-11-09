using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISHFTest.Core.Models
{
    public class ScheduleAndResultsViewModel
    {
        public ScheduleAndResultsViewModel()
        {
            ScheduleAndResults = new List<ScheduleAndResults>();
        }

        public List<ScheduleAndResults> ScheduleAndResults { get; set; }
    }
}
