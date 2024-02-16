using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DataAnnotationsExtensions;

namespace IISHF.Core.Models
{
    public class RegisterViewModel
    {
        [DisplayName("First Name")]
        [Required(ErrorMessage = "Please enter your first name")]
        public string FirstName { get; set; }

        [DisplayName("Family Name")]
        [Required(ErrorMessage = "Please enter your family name")]
        public string LastName { get; set; }

        [DisplayName("Email Address")]
        [Required(ErrorMessage = "Please enter your email address")]
        public string EmailAddress { get; set; }

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
