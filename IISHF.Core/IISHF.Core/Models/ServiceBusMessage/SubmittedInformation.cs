namespace IISHF.Core.Models.ServiceBusMessage
{
    public class SubmittedInformation
    {
        public TeamInformation TeamInformation { get; set; }

        public HostInformation HostInformation { get; set; }

        public EventInformation EventInformation { get; set; }
    }
}
