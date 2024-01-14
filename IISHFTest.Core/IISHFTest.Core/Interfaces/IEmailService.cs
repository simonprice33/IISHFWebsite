using IISHFTest.Core.Models;

namespace IISHFTest.Core.Interfaces
{
    public interface IEmailService
    {
        Task SendRegistrationConfirmation(CreateMemberRegistration registration);

        Task SendContactFormMessage(ContactFormViewModel contactModel);
    }
}
