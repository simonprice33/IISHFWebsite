using System.ComponentModel;

namespace IISHF.Core.Models
{
    public class AccountViewModel
    {
        public AccountViewModel()
        {
            Invitations = new List<EventInvitation>();
        }

        public string? Name { get; set; } = string.Empty;

        [DisplayName("User Name")]
        public string? Email { get; set; } = string.Empty;

        public string? Username { get; set; } = string.Empty;

        [DisplayName("Password")]
        public string? Password { get; set; } = string.Empty;

        [DisplayName("Confirm Password")]
        public string? ConfirmPassword { get; set; } = string.Empty;

        public List<EventInvitation> Invitations { get; set; }
    }
}
