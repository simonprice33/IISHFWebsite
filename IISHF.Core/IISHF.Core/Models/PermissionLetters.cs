using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace IISHF.Core.Models
{
    public class PermissionLetters
    {
        public List<IPublishedContent> Permissionletters { get; set; }
       
        public List<IPublishedContent> Players { get; set; }
    }
}
