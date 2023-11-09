using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models.ContentEditing;

namespace IISHFTest.Core.Models
{
    public class FinalPlacementsViewModel
    {

        public FinalPlacementsViewModel()
        {
            TeamPlacements = new List<TeamPlacements>();
        }
        public IEnumerable<TeamPlacements> TeamPlacements { get; set; }
    }
}
