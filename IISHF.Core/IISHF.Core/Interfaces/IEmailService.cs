using IISHF.Core.Models;

namespace IISHF.Core.Interfaces
{
    public interface IEmailService
    {
        Task SendRegistrationConfirmation(Member member, string templateName, string subject);

        Task SendContactFormMessage(ContactFormViewModel contactModel);
    }
}
