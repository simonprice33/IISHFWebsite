namespace IISHF.Core.Models
{
    public class Member
    {
        public string Name { get; set; }

        public string EmailAddress { get; set; }

        public Guid Token { get; set; }

        public Uri TokenUrl { get; set; }
    }
}
