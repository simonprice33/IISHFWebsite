using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IISHFTest.Core.Models;
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

namespace IISHFTest.Core.Controllers
{
    [Controller]
    public class ContactController : SurfaceController
    {
        private readonly IPublishedContentQuery _contentQuery;

        public ContactController(
            IUmbracoContextAccessor umbracoContextAccessor,
            IUmbracoDatabaseFactory databaseFactory,
            ServiceContext services, AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IPublishedUrlProvider publishedUrlProvider,
            IPublishedContentQuery contentQuery)
            : base(umbracoContextAccessor,
                databaseFactory,
                services,
                appCaches,
                profilingLogger,
                publishedUrlProvider)
        {
            _contentQuery = contentQuery;
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

            // Programatically create new contact form for umbraco
            var rootContent = _contentQuery.ContentAtRoot().ToList();
            var contactItems = rootContent.FirstOrDefault(x => x.Name == "Data")!.Children.FirstOrDefault(x => x.Name == "Contact Items");
            ////var myContentItem = rootContent?.DescendantsOrSelfOfType().FirstOrDefault();

            if (contactItems != null)
            {
                var newContact = Services.ContentService?.Create("Contact", contactItems.Id, "contactItem");
                newContact.SetValue("sender", model.Name);
                newContact.SetValue("senderEmail", model.Email);
                newContact.SetValue("subject", model.Subject);
                newContact.SetValue("message", model.Message);

                Services.ContentService?.SaveAndPublish(newContact);
            }

            // Send email 
            TempData["Status"] = "Message Sent Ok";
            // confirmation to user

            return RedirectToCurrentUmbracoPage();
        }

        [HttpGet]
        public ActionResult RenderContactForm()
        {
            var model = new ContactFormViewModel();
            return PartialView("~/Views/Partials/Contact/ContactForm.cshtml", model);
        }

    }
}
