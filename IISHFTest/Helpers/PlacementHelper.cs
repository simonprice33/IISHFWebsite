namespace IISHFTest.Helpers
{
    public static class PlacementHelper
    {
        public static string GetPlacementClass(int placement)
        {
            return placement switch
            {
                1 => "bg-gold",
                2 => "bg-silver",
                3 => "bg-bronze",
                _ => string.Empty // Default case when the placement isn't 1, 2, or 3.
            };
        }
    }
}
