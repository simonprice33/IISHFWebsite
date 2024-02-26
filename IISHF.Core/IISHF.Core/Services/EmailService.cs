using HandlebarsDotNet;
using IISHF.Core.Configurations;
using IISHF.Core.Interfaces;
using IISHF.Core.Models;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Errors.Model;
using SendGrid.Helpers.Mail;

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

        public async Task SendRegistrationConfirmation(Member member, string templateName, string subject)
        {
            var renderedEmail = await GetHtmlTemplate(member, templateName);
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

        private async Task SendEmail(string plainTextMessage, string htmlMessage, EmailAddress sender, List<EmailAddress> recipients, string subject)
        {
            var sendGridMessage = MailHelper.CreateSingleEmailToMultipleRecipients(
                sender,
                recipients,
                subject,
                plainTextMessage,
                htmlMessage,
                true);

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
