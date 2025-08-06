using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISHF.Core.Models
{
    public class GroupInformation : TournamentBaseModel
    {
        public IEnumerable<TeamGroup> TeamGroups { get; set; }
    }
}
