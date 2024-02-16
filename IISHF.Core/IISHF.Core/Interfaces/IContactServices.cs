using IISHF.Core.Models;
using Umbraco.Cms.Core.Models;

namespace IISHF.Core.Interfaces
{
    public interface IContactServices
    {
        IContent? CreateContactRecord(ContactFormViewModel model);
    }
}
