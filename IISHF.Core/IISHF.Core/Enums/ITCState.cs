using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISHF.Core.Enums
{
    public enum ItcState
    {
        [Description("Not submitted")]
        NotSubmitted,

        [Description("Pending NMA Approval")]
        PendingNmaApproval,

        [Description("Changes required")]
        ChangesRequired,

        [Description("Changes made")]
        ChangesMade,

        [Description("NMA Approved - Pending IISHF Approval")]
        NmaApprovedPendingIishfApproval
    }
}
