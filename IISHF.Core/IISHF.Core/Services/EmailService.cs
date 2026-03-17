using HandlebarsDotNet;
using IISHF.Core.Configurations;
using IISHF.Core.Interfaces;
using IISHF.Core.Models;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Errors.Model;
using SendGrid.Helpers.Mail;
using System;
using Umbraco.Cms.Core.Models;
using Member = IISHF.Core.Models.Member;

namespace IISHF.Core.Services
{
    public class EmailService : IEmailService
    {
        private readonly SendGridConfiguration _sendGridConfiguration;
        private readonly EmailConfiguration _iishfOptions;
        private readonly IHttpClient _httpClient;
        private readonly SendGridClient _sendGridClient;

        public EmailService(
            IOptions<SendGridConfiguration> sendGridOptions,
            IOptions<EmailConfiguration> iishfOptions,
            IHttpClient httpClient)
        {
            _sendGridConfiguration = sendGridOptions.Value;
            _iishfOptions = iishfOptions.Value;
            _httpClient = httpClient;
            _sendGridClient = new SendGridClient(_sendGridConfiguration.ApiKey);

        }

        public async Task SendRegistrationConfirmation(Member member, string templateContent, string subject)
        {
            var renderedEmail = await GetHtmlTemplate(member, templateContent);
            //var htmlPath = $"https://localhost:44322/verify?token={registration.Token}";
            //var renderedEmail = $"<a href=\"{htmlPath}\">Click here</a> to verify your email address and complete registration of your IISHF Account";

            var sender = new EmailAddress(_iishfOptions.NoReplyEmailAdddress, _iishfOptions.DisplayName);

            var recipients = new List<EmailAddress>()
            {
                ////new EmailAddress(_iishfOptions.SenderEmailAdddress),
                new EmailAddress(member.EmailAddress),
            };

            await SendEmail(string.Empty, renderedEmail, sender, recipients, subject);
        }

        public async Task SendUserInvitation(IMember member, string email, string recipientName, Uri uri, string templateContent, string subject)
        {

            var invitationObject = new
            {
                Name = recipientName,
                Uri = uri,
                MemberName = member.Name,
            };

            var renderedEmail = await GetHtmlTemplate(invitationObject, templateContent);

            var sender = new EmailAddress(_iishfOptions.NoReplyEmailAdddress, _iishfOptions.DisplayName);

            var recipients = new List<EmailAddress>()
            {
                new EmailAddress(email),
            };

            await SendEmail(string.Empty, renderedEmail, sender, recipients, subject);
        }

        public async Task SendItc(string email, List<string> ccEmails, string recipientName, string eventName,
            string templateName, string subject, string teamName, byte[] attachment, string fileName)
        {
            var invitationObject = new
            {
                Name = recipientName,
            };

            var attachments = new List<EmailAttachment>()
            {
                new EmailAttachment()
                {
                    Extension = ".xlsx",
                    FileBytes = attachment,
                    FileName = fileName
                }
            };

            var renderedEmail = await GetHtmlTemplate(invitationObject, templateName);

            var sender = new EmailAddress(_iishfOptions.NoReplyEmailAdddress, _iishfOptions.DisplayName);

            var recipients = new List<EmailAddress>()
            {
                new EmailAddress(email),
            };

            var ccRecipients = new List<EmailAddress>();
            ccRecipients.AddRange(ccEmails.Select(cc => new EmailAddress(cc)));

            await SendEmail(string.Empty, renderedEmail, sender, recipients, ccRecipients, subject, true, attachments);
        }

        public async Task SendContactFormMessage(ContactFormViewModel contactModel)
        {
            var sender = new EmailAddress(_iishfOptions.NoReplyEmailAdddress, _iishfOptions.DisplayName);
            var subject = $"{contactModel.Subject}";

            var recipients = new List<EmailAddress>()
            {
                new EmailAddress(contactModel.Email, contactModel.Name)
            };

            await SendEmail(contactModel.Message, contactModel.Message, sender, recipients, subject);
            return;
        }

        private async Task SendEmail(string plainTextMessage, string htmlMessage, EmailAddress sender, List<EmailAddress> recipients, string subject, bool showAllRecipients = true)
        {
            var sendGridMessage = MailHelper.CreateSingleEmailToMultipleRecipients(
                sender,
                recipients,
                subject,
                plainTextMessage,
                htmlMessage,
                showAllRecipients);

            var result = await _sendGridClient.SendEmailAsync(sendGridMessage, CancellationToken.None);

            if (result.IsSuccessStatusCode)
            {
                return;
            }

            throw new SendGridInternalException("Something went wrong in sending");
        }

        private async Task SendEmail(string plainTextMessage, string htmlMessage, EmailAddress sender, List<EmailAddress> recipients, List<EmailAddress> cc, string subject, bool showAllRecipients = true, List<EmailAttachment>? attachments = null)
        {
            var sendGridMessage = MailHelper.CreateSingleEmailToMultipleRecipients(
                sender,
                recipients,
                subject,
                plainTextMessage,
                htmlMessage,
                showAllRecipients);

            if (attachments != null && attachments.Any())
            {
                foreach (var attachment in attachments)
                {
                    var base64Content = Convert.ToBase64String(attachment.FileBytes);
                    sendGridMessage.AddAttachment(attachment.FileName, base64Content, attachment.MimeType);
                }
            }

            var result = await _sendGridClient.SendEmailAsync(sendGridMessage, CancellationToken.None);

            if (result.IsSuccessStatusCode)
            {
                return;
            }

            throw new SendGridInternalException("Something went wrong in sending");
        }

        private async Task<string> GetHtmlTemplate(object invitation, string templateName)
        {
            Handlebars.RegisterHelper("formatDate", (writer, context, parameters) =>
            {
                if (parameters.Length != 1 || !(parameters[0] is DateTime dateTime))
                {
                    throw new HandlebarsException("{{formatDate}} helper requires a single DateTime parameter.");
                }

                // Format the DateTime object and write it to the output
                writer.WriteSafeString(dateTime.ToShortDateString());
            });

            var templateUri = new Uri($"{_iishfOptions.EmailTemplateBaseUrl.ToString()}{templateName}");

            var emailTemplate = await _httpClient.GetStringAsync(templateUri);
            string renderedEmail = Handlebars.Compile(emailTemplate)(invitation);
            return renderedEmail;
        }
    }
}
