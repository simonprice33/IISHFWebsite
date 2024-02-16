namespace IISHF.Core.Models
{
    public class ITCEventInformationViewModel
    {
        public bool IsChampionship { get; set; }

        public string EventName { get; set; }

        public string EventDescription { get; set; }

        public DateTime EventStartDate { get; set; }

        public DateTime EventEndDate { get; set; }

        public string AgeGroup { get; set; }

        public string EvenLocation { get; set; }

        public string SanctionNumber { get; set; }

        public string HostingCountry { get; set; }

        public string HostingClub{ get; set; }

        public string ShortCode { get; set; }

        public List<string> Teams { get; set; }
    }
}
