namespace IISHF.Core.Models.ServiceBusMessage
{
    public class EventInformation
    {
        public string EventName { get; set; }

        public string EventNumber { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }
    }
}
