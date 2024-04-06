using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISHF.Core.Models
{
    public class EmailAttachment
    {
        public byte[] FileBytes { get; set; }

        public string FileName { get; set; } = string.Empty;

        public string Extension { get; set; } = string.Empty;

        public string MimeType => MimeTypes.GetMimeType(Extension);

        public Uri Path { get; set; }
    }
}
