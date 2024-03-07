using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading.Tasks;

namespace IISHF.Core.Models
{
    public class ITCApproval
    {
        public Guid EventTeamId { get; set; }

        public Guid EventId { get; set; }

        public string EventName { get; set; }

        public string TeamName { get; set; }

        public DateTime? ITCSubmittedDate { get; set; }
        
        public string ITCSubmittedBy { get; set; }

        public string NMAApprover { get; set; } = string.Empty;

        public DateTime? NMAApproveDate { get; set;}

        public string IISHFApprover { get; set; } = string.Empty;

        public DateTime? IISHFApproveDate { get; set; }

        public DateTime EventStartDate { get; set; }
        
        public DateTime EventEndDate { get; set; }

        public bool CanApprove { get; set; }
    }
}
