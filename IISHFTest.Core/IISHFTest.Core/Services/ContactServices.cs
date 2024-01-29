using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IISHFTest.Core.Interfaces;
using IISHFTest.Core.Models;
using Microsoft.AspNetCore.Http;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;

namespace IISHFTest.Core.Services
{
    public class ContactServices : IContactServices
    {
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;
        private readonly IUmbracoDatabaseFactory _databaseFactory;
        private readonly ServiceContext _services;
        private readonly AppCaches _appCaches;
        private readonly IProfilingLogger _profilingLogger;
        private readonly IPublishedUrlProvider _publishedUrlProvider;
        private readonly IPublishedContentQuery _contentQuery;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ContactServices(IUmbracoContextAccessor umbracoContextAccessor,
            IUmbracoDatabaseFactory databaseFactory,
            ServiceContext services,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IPublishedUrlProvider publishedUrlProvider,
            IPublishedContentQuery contentQuery,
            IHttpContextAccessor httpContextAccessor)
        {
            _umbracoContextAccessor = umbracoContextAccessor;
            _databaseFactory = databaseFactory;
            _services = services;
            _appCaches = appCaches;
            _profilingLogger = profilingLogger;
            _publishedUrlProvider = publishedUrlProvider;
            _contentQuery = contentQuery;
            _httpContextAccessor = httpContextAccessor;
        }

        public IContent? CreateContactRecord(ContactFormViewModel model)
        {
            // Programatically create new contact form for umbraco
            var rootContent = _contentQuery.ContentAtRoot().ToList();
            var contactItems = rootContent.FirstOrDefault(x => x.Name == "Data")!.Children.FirstOrDefault(x => x.Name == "Contact Items");
            ////var myContentItem = rootContent?.DescendantsOrSelfOfType().FirstOrDefault();

            if (contactItems != null)
            {
                var newContact = _services.ContentService?.Create($"Contact from {model.Name} at {DateTime.UtcNow:s}", contactItems.Id, "contactItem");
                newContact.SetValue("sender", model.Name);
                newContact.SetValue("senderEmail", model.Email);
                newContact.SetValue("subject", model.Subject);
                newContact.SetValue("message", model.Message);

                _services.ContentService?.SaveAndPublish(newContact);
                return newContact;
            }

            return null;
        }
    }
}
