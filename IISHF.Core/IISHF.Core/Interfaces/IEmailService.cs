using IISHF.Core.Models;
using Umbraco.Cms.Core.Models;
using Member = IISHF.Core.Models.Member;

namespace IISHF.Core.Interfaces
{
    public interface IEmailService
    {
        Task SendRegistrationConfirmation(Member member, string templateName, string subject);

        Task SendContactFormMessage(ContactFormViewModel contactModel);

        Task SendUserInvitation(IMember member, string email, string recipientName, Uri uri, string templateName, string subject);

        Task SendItc(string email, List<string> ccEmails, string recipientName, string eventName, string templateName,
            string subject, string teamName, byte[] attachment, string fileName);
    }
}
