using HandlebarsDotNet;
using IISHF.Core.Configurations;
using IISHF.Core.Interfaces;
using IISHF.Core.Models;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using Umbraco.Cms.Core.Configuration.Models;
using IMember = Umbraco.Cms.Core.Models.IMember;
using Member = IISHF.Core.Models.Member;

namespace IISHF.Core.Services
{
    /// <summary>
    /// SMTP implementation of IEmailService (SendGrid replacement).
    /// Uses Umbraco GlobalSettings.Smtp (appsettings.json -> Umbraco:CMS:Global:Smtp).
    /// </summary>
    public class SmtpEmailService : IEmailService
    {
        private readonly EmailConfiguration _iishfOptions;
        private readonly IHttpClient _httpClient;
        private readonly GlobalSettings _globalSettings;

        public SmtpEmailService(
            IOptions<EmailConfiguration> iishfOptions,
            IOptions<GlobalSettings> globalSettings,
            IHttpClient httpClient)
        {
            _iishfOptions = iishfOptions.Value;
            _globalSettings = globalSettings.Value;
            _httpClient = httpClient;
        }

        public async Task SendRegistrationConfirmation(Member member, string templateName, string subject)
        {
            var renderedEmail = await GetHtmlTemplate(member, templateName);

            await SendEmailAsync(
                subject: subject,
                htmlBody: renderedEmail,
                textBody: string.Empty,
                fromEmail: _iishfOptions.NoReplyEmailAdddress,
                fromName: _iishfOptions.DisplayName,
                toEmails: new List<string> { member.EmailAddress });
        }

        public async Task SendUserInvitation(IMember member, string email, string recipientName, Uri uri, string templateName, string subject)
        {
            var invitationObject = new
            {
                Name = recipientName,
                Uri = uri,
                MemberName = member.Name,
            };

            var renderedEmail = await GetHtmlTemplate(invitationObject, templateName);

            await SendEmailAsync(
                subject: subject,
                htmlBody: renderedEmail,
                textBody: string.Empty,
                fromEmail: _iishfOptions.NoReplyEmailAdddress,
                fromName: _iishfOptions.DisplayName,
                toEmails: new List<string> { email });
        }

        public async Task SendItc(
            string email,
            List<string> ccEmails,
            string recipientName,
            string eventName,
            string templateName,
            string subject,
            string teamName,
            byte[] attachment,
            string fileName)
        {
            var data = new
            {
                Name = recipientName,
                EventName = eventName,
                TeamName = teamName
            };

            var renderedEmail = await GetHtmlTemplate(data, templateName);

            var attachments = new List<EmailAttachment>
            {
                new EmailAttachment
                {
                    FileName = fileName,
                    FileBytes = attachment,
                }
            };

            await SendEmailAsync(
                subject: subject,
                htmlBody: renderedEmail,
                textBody: string.Empty,
                fromEmail: _iishfOptions.NoReplyEmailAdddress,
                fromName: _iishfOptions.DisplayName,
                toEmails: new List<string> { email },
                ccEmails: ccEmails,
                attachments: attachments);
        }

        public async Task SendContactFormMessage(ContactFormViewModel contactModel)
        {
            // NOTE: this matches the behaviour in your earlier service (sends to the submitter).
            // If you want it to send to your support mailbox instead, change the recipient list here.
            await SendEmailAsync(
                subject: contactModel.Subject ?? string.Empty,
                htmlBody: contactModel.Message ?? string.Empty,
                textBody: contactModel.Message ?? string.Empty,
                fromEmail: _iishfOptions.NoReplyEmailAdddress,
                fromName: _iishfOptions.DisplayName,
                toEmails: new List<string> { contactModel.Email });
        }

        private async Task SendEmailAsync(
            string subject,
            string htmlBody,
            string textBody,
            string fromEmail,
            string fromName,
            List<string> toEmails,
            List<string> ccEmails = null,
            List<string> bccEmails = null,
            List<EmailAttachment> attachments = null)
        {
            if (toEmails == null || !toEmails.Any())
                throw new ArgumentException("At least one recipient is required.", nameof(toEmails));

            var smtp = _globalSettings?.Smtp;
            if (smtp == null)
                throw new InvalidOperationException("Umbraco SMTP settings are missing (GlobalSettings.Smtp).");

            var message = new MimeMessage();

            // From (prefer explicit, otherwise fall back to Umbraco smtp.From)
            var effectiveFrom = !string.IsNullOrWhiteSpace(fromEmail) ? fromEmail : smtp.From;
            message.From.Add(new MailboxAddress(fromName ?? string.Empty, effectiveFrom));

            foreach (var to in toEmails.Where(x => !string.IsNullOrWhiteSpace(x)))
                message.To.Add(MailboxAddress.Parse(to));

            if (ccEmails != null)
            {
                foreach (var cc in ccEmails.Where(x => !string.IsNullOrWhiteSpace(x)))
                    message.Cc.Add(MailboxAddress.Parse(cc));
            }

            if (bccEmails != null)
            {
                foreach (var bcc in bccEmails.Where(x => !string.IsNullOrWhiteSpace(x)))
                    message.Bcc.Add(MailboxAddress.Parse(bcc));
            }

            message.Subject = subject ?? string.Empty;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody ?? string.Empty,
                TextBody = !string.IsNullOrWhiteSpace(textBody) ? textBody : StripHtml(htmlBody)
            };

            if (attachments != null && attachments.Any())
            {
                foreach (var a in attachments)
                {
                    if (a == null || a.FileBytes == null || a.FileBytes.Length == 0)
                    {
                        continue;
                    }

                    // Explicit MimeKit.ContentType to avoid collision with Umbraco ContentType
                    ContentType contentType = !string.IsNullOrWhiteSpace(a.MimeType)
                        ? ContentType.Parse(a.MimeType)
                        : new ContentType("application", "octet-stream");

                    bodyBuilder.Attachments.Add(a.FileName, a.FileBytes, contentType);
                }
            }

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            client.CheckCertificateRevocation = false;

            var secure = ResolveSecureSocketOptions(smtp);

            try
            {
                await client.ConnectAsync(smtp.Host, smtp.Port, secure);

                if (!string.IsNullOrWhiteSpace(smtp.Username))
                    await client.AuthenticateAsync(smtp.Username, smtp.Password);

                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("SMTP send failed. Check SMTP host/port/credentials and TLS settings.", ex);
            }
        }

        private static MailKit.Security.SecureSocketOptions ResolveSecureSocketOptions(SmtpSettings smtp)
        {
            if (smtp.Port == 465) return MailKit.Security.SecureSocketOptions.SslOnConnect;
            if (smtp.Port == 587) return MailKit.Security.SecureSocketOptions.StartTls;
            return MailKit.Security.SecureSocketOptions.Auto;
        }

        private async Task<string> GetHtmlTemplate(object data, string templateName)
        {
            Handlebars.RegisterHelper("formatDate", (writer, context, parameters) =>
            {
                if (parameters.Length != 1 || !(parameters[0] is DateTime dt))
                    throw new HandlebarsException("{{formatDate}} helper requires a single DateTime parameter.");

                writer.WriteSafeString(dt.ToShortDateString());
            });

            var templateUri = new Uri($"{_iishfOptions.EmailTemplateBaseUrl}{templateName}");
            var emailTemplate = await _httpClient.GetStringAsync(templateUri);

            return Handlebars.Compile(emailTemplate)(data);
        }

        private static string StripHtml(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            return System.Text.RegularExpressions.Regex.Replace(input, "<.*?>", string.Empty);
        }
    }
}