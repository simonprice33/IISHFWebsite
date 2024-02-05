using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IISHFTest.Core.Interfaces;
using IISHFTest.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Media.EmbedProviders;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Website.Controllers;
using Umbraco.Extensions;

namespace IISHFTest.Core.Controllers.SurfaceControllers
{
    [Controller]
    public class ContactController : SurfaceController
    {
        private readonly IPublishedContentQuery _contentQuery;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IContactServices _contactServices;

        public ContactController(
            IUmbracoContextAccessor umbracoContextAccessor,
            IUmbracoDatabaseFactory databaseFactory,
            ServiceContext services, 
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IPublishedUrlProvider publishedUrlProvider,
            IPublishedContentQuery contentQuery,
            IHttpContextAccessor httpContextAccessor,
            IContactServices contactServices)
            : base(umbracoContextAccessor,
                databaseFactory,
                services,
                appCaches,
                profilingLogger,
                publishedUrlProvider)
        {
            _contentQuery = contentQuery;
            _httpContextAccessor = httpContextAccessor;
            _contactServices = contactServices;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Post(ContactFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("Error", "Not enough information has been provided to submit your message");
                return CurrentUmbracoPage();

            }

            var contactItem = _contactServices.CreateContactRecord(model);

            if (contactItem == null)
            {
                return CurrentUmbracoPage();
            }
           
            //ToDo Send email 

            TempData["Status"] = "Message Sent Ok";
            // confirmation to user

            return RedirectToCurrentUmbracoPage();
        }
    }
}
