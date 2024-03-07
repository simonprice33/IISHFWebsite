using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IISHF.Core.Models
{
    public class ITCRosterMemberpproval
    {
        public ITCRosterMemberpproval()
        {
            RosterApprovals = new List<RosterApproval>();
        }

        public IEnumerable<RosterApproval> RosterApprovals { get; set; }
    }

    public class RosterApproval 
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("approved")]
        public bool Approved { get; set; }

        [JsonPropertyName("comments")]
        public string Comments { get; set; } = string.Empty;
    }
}
