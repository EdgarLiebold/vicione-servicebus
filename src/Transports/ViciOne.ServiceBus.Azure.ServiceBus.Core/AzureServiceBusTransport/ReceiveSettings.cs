using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBusTransport;

public interface ReceiveSettings :
    ClientSettings
{
    CreateQueueOptions GetCreateQueueOptions();
}
