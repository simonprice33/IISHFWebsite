namespace IISHF.Core.Models.ServiceBusMessage
{
    public class SubmittedITCInformation
    {
        public SubmittedITCInformation()
        {
            ItcApprovers = new List<ITCApprover>();
        }

        public Uri ITCApprovalUri { get; set; }

        public string TeamName { get; set; }

        public string SubmittedByName { get; set; }

        public List<ITCApprover> ItcApprovers { get; set; }

        public string TemplateName { get; set; }
    }
}
