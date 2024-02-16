namespace IISHF.Core.Interfaces
{
    public interface IMessageSender
    {
        Task SendMessage<T>(T submittedInformation, string subject);
    }
}
