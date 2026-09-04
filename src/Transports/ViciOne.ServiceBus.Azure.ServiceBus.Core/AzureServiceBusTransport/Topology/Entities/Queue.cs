using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBusTransport.Topology;

/// <summary>
/// The queue details used to declare the queue to Azure Service Bus
/// </summary>
public interface Queue
{
    CreateQueueOptions CreateQueueOptions { get; }
}
