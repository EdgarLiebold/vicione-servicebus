using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Describes an Azure Service Bus queue declaration.</summary>
public interface Queue
{
    /// <summary>Gets the Azure queue declaration options.</summary>
    CreateQueueOptions CreateQueueOptions { get; }
}
