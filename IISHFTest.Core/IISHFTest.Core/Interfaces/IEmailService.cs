using IISHFTest.Core.Models;

namespace IISHFTest.Core.Interfaces
{
    public interface IEmailService
    {
        Task SendRegistrationConfirmation(Member member, string templateName, string subject);

        Task SendContactFormMessage(ContactFormViewModel contactModel);
    }
}
