// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.AzureServiceBusTransport.Topology
{
    using Azure.Messaging.ServiceBus.Administration;


    /// <summary>
    /// The exchange details used to declare the exchange to Azure Service Bus
    /// </summary>
    public interface Topic
    {
        CreateTopicOptions CreateTopicOptions { get; }
    }
}
