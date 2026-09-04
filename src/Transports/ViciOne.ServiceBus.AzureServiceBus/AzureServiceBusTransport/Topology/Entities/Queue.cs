using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>
/// The queue details used to declare the queue to Azure Service Bus
/// </summary>
public interface Queue
{
    /// <summary>
    /// Gets the create queue options value.
    /// </summary>
    CreateQueueOptions CreateQueueOptions { get; }
}
