// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.AzureServiceBusTransport.Topology
{
    using Azure.Messaging.ServiceBus.Administration;


    /// <summary>
    /// The queue details used to declare the queue to Azure Service Bus
    /// </summary>
    public interface Queue
    {
        CreateQueueOptions CreateQueueOptions { get; }
    }
}
