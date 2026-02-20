using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Mail;
using Umbraco.Cms.Core.Models.Email;

namespace IISHF.Core.Infrastructure.Mail
{
    public sealed class UmbracoMailSender : IEmailSender
    {
        private readonly GlobalSettings _global;

        public UmbracoMailSender(IOptions<GlobalSettings> globalSettings)
        {
            _global = globalSettings.Value;
        }

        public bool CanSendRequiredEmail()
        {
            return _global?.Smtp != null;
        }

        public async Task SendAsync(EmailMessage message, string emailType)
        {
            await SendAsync(message, emailType, false);
        }

        public async Task SendAsync(EmailMessage message, string emailType, bool enableNotification)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var smtp = _global.Smtp ?? throw new InvalidOperationException("Umbraco SMTP settings are missing.");

            var mime = new MimeMessage();
            mime.From.Add(MailboxAddress.Parse(message.From ?? smtp.From));

            foreach (var to in message.To ?? Enumerable.Empty<string>())
                mime.To.Add(MailboxAddress.Parse(to));

            foreach (var cc in message.Cc ?? Enumerable.Empty<string>())
                mime.Cc.Add(MailboxAddress.Parse(cc));

            foreach (var bcc in message.Bcc ?? Enumerable.Empty<string>())
                mime.Bcc.Add(MailboxAddress.Parse(bcc));

            mime.Subject = message.Subject ?? string.Empty;

            mime.Body = new BodyBuilder
            {
                HtmlBody = message.Body,
                TextBody = StripHtml(message.Body)
            }.ToMessageBody();

            using var client = new SmtpClient();

            // Key line: avoid failing when CRL/OCSP endpoints are unreachable
            client.CheckCertificateRevocation = false;

            await client.ConnectAsync(smtp.Host, smtp.Port, true);

            if (!string.IsNullOrWhiteSpace(smtp.Username))
                await client.AuthenticateAsync(smtp.Username, smtp.Password);

            await client.SendAsync(mime);
            await client.DisconnectAsync(true);
        }

        private static string StripHtml(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            return System.Text.RegularExpressions.Regex
                .Replace(input, "<.*?>", string.Empty);
        }
    }
}
