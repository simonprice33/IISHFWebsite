using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace IISHF.Core.Models
{
    public class ForgotPasswordResetRequestModel
    {
        [DisplayName("Email Address")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string EmailAddress { get; set; }
    }
}
