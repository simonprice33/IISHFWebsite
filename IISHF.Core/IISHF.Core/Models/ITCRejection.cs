using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISHF.Core.Models
{
    public class ITCRejection : SubmittedITCInformation
    {
        public ITCRejection()
        {
            RejectionReasons = new List<string>();
        }

        public ITCApprover TeamApprover { get; set; }

        public ITCApprover NmaiItcApprover { get; set; }

        public List<string> RejectionReasons { get; set; }
    }
}
