using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Describes an Azure Service Bus topic declaration.</summary>
public interface Topic
{
    /// <summary>Gets the Azure topic declaration options.</summary>
    CreateTopicOptions CreateTopicOptions { get; }
}
