using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Infrastructure.Examine;

namespace IISHF.Core.Models.ServiceBusMessage
{
    public class ITCApprover
    {
        public string NmaApproverName { get; set; }

        public string NmaApproverEmail { get; set; }
    }
}
