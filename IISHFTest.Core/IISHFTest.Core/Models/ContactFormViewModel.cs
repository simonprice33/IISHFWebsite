using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISHFTest.Core.Models
{
    public class ContactFormViewModel
    {
        public ContactFormViewModel()
        {
            
        }

        [Required]
        [MaxLength(80, ErrorMessage = "Name limited to 80 characters")]
        public string Name { get; set; }

        [Required]
        [DataType(DataType.EmailAddress, ErrorMessage = "Please enter a valid email address")]
        public string Email { get; set; }

        [Required]
        [MaxLength(80, ErrorMessage = "Subject limited to 80 characters")]
        public string Subject { get; set; }

        [Required]
        [MaxLength(500, ErrorMessage = "Message limited to 500 characters")]
        public string Message { get; set; }


    }
}
