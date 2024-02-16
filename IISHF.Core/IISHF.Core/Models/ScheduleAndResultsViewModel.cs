namespace IISHF.Core.Models
{
    public class ScheduleAndResultsViewModel
    {
        public ScheduleAndResultsViewModel()
        {
            ScheduleAndResults = new List<ScheduleAndResults>();
        }

        public List<ScheduleAndResults> ScheduleAndResults { get; set; }
    }
}
