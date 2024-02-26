using DataAnnotationsExtensions;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace IISHF.Core.Models
{
    public class PasswordResetRequestModel
    {
        [DisplayName("Password")]
        [Required(ErrorMessage = "Please enter a password")]
        [MinLength(10, ErrorMessage = "Minimum password length needs to be at least 10 characters")]
        public string Password { get; set; }

        [DisplayName("Password")]
        [Required(ErrorMessage = "Please enter a password confirmation")]
        [EqualTo(nameof(Password), ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }
    }
}
