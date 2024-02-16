using System.Text;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using IISHF.Core.Interfaces;
using IISHF.Core.Settings;
using Microsoft.Extensions.Options;

namespace IISHF.Core.Services
{
    public class ServiceBusMessagingService : IMessageSender
    {
        private readonly ServiceBusSettings _serviceBusSettings;

        public ServiceBusMessagingService(IOptions<ServiceBusSettings> serviceBusSettings)
        {
            _serviceBusSettings = serviceBusSettings.Value;
        }

        public async Task SendMessage<T>(T submittedInformation, string subject)
        {
            await using ServiceBusClient client = new ServiceBusClient(_serviceBusSettings.ConnectionString);
            var sender = client.CreateSender(_serviceBusSettings.Topic);

            try
            {
                var messageBody = JsonSerializer.Serialize(submittedInformation);
                var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(messageBody))
                {
                    ApplicationProperties =
                    {
                        ["subject"] = subject
                    }
                };
                // Send the message to the topic
                await sender.SendMessageAsync(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }
}
