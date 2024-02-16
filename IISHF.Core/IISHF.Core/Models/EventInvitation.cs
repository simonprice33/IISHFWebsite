namespace IISHF.Core.Models
{
    public class EventInvitation
    {
        public Guid EventTeamId { get; set; }

        public Guid EventId { get; set; } 
        
        public string EventName { get; set; }

        public string ITCStatus { get; set; }

        public bool TeamInformationSubmitted { get; set; }
    }
}
