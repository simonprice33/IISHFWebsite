using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISHFTest.Core.Models
{
    public class Member
    {
        public string Name { get; set; }

        public string EmailAddress { get; set; }

        public Guid Token { get; set; }

        public Uri TokenUrl { get; set; }
    }
}
