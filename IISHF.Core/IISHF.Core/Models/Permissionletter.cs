using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace IISHF.Core.Models
{
    public class Permissionletter
    {
        public IPublishedContent Players { get; set; }

        public IPublishedContent PermissionDocuments { get; set; }
    }
}
