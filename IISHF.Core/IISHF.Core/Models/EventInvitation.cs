using Umbraco.Cms.Infrastructure.Examine;

namespace IISHF.Core.Models
{
    public class EventInvitation
    {
        public Guid EventTeamId { get; set; }

        public Guid EventId { get; set; } 
        
        public string EventName { get; set; }

        public string ITCStatus { get; set; }

        public DateTime? ItcStatusChangeDate { get; set; }

        public bool TeamInformationSubmitted { get; set; }

        public DateTime? TeamInformationSubmittedDate { get; set; }

        public DateTime? TeamSubmissionRequiredBy { get; set; }

        public DateTime EventStartDate { get; set; }

        public DateTime EventEndDate { get; set; }
        public bool TeamInformationRequired { get; set; }
    }
}
