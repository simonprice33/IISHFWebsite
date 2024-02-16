namespace IISHF.Core.Models.ServiceBusMessage
{
    public class Roster
    {
        /// <summary>
        /// Gets or Sets Id 
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or Sets Given Name
        /// </summary>
        public string GivenName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or Sets Last Name
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or Sets Jersey Number
        /// </summary>
        public int? JerseyNumber { get; set; }

        /// <summary>
        /// Gets or Sets Position
        /// </summary>
        public string Role { get; set; }
    }
}
