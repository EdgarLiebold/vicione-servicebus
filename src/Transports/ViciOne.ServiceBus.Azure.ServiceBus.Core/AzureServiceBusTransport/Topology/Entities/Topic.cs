using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBusTransport.Topology;

/// <summary>
/// The exchange details used to declare the exchange to Azure Service Bus
/// </summary>
public interface Topic
{
    CreateTopicOptions CreateTopicOptions { get; }
}
