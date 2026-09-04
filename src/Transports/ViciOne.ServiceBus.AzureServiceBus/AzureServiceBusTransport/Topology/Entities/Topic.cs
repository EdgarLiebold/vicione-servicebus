using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>
/// The exchange details used to declare the exchange to Azure Service Bus
/// </summary>
public interface Topic
{
    /// <summary>
    /// Gets the create topic options value.
    /// </summary>
    CreateTopicOptions CreateTopicOptions { get; }
}
