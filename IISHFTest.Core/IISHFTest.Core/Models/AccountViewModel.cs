using DataAnnotationsExtensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISHFTest.Core.Models
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
