using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISHF.Core.Models
{
    public class RejectedRosterMember
    {
        public int Id { get; set; }

        public string LicenseNumber { get; set; }

        public string Name { get; set; }

        public string Reason { get; set; } = string.Empty;
    }
}
