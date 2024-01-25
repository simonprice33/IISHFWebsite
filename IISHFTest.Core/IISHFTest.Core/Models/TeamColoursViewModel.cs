using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISHFTest.Core.Models
{
    public class TeamColoursViewModel
    {
        public TeamColoursViewModel()
        {
            TeamColours = new List<ColourViewModel>();
        }

        public List<ColourViewModel> TeamColours { get; set; }
    }
}
