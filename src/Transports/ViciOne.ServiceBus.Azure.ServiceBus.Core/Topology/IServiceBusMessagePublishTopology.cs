using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.AzureServiceBusTransport;
using ViciOne.ServiceBus.AzureServiceBusTransport.Configuration;
using ViciOne.ServiceBus.AzureServiceBusTransport.Topology;

namespace ViciOne.ServiceBus;

public interface IServiceBusMessagePublishTopology<TMessage> :
    IMessagePublishTopology<TMessage>,
    IServiceBusMessagePublishTopology
    where TMessage : class
{
    /// <summary>
    /// Returns the topic options for the message type
    /// </summary>
    CreateTopicOptions CreateTopicOptions { get; }

    SendSettings GetSendSettings();

    ServiceBusSubscriptionConfigurator GetSubscriptionConfigurator(string subscriptionName);
}


public interface IServiceBusMessagePublishTopology
{
    /// <summary>
    /// Apply the message topology to the builder, including any implemented types
    /// </summary>
    /// <param name="builder">The topology builder</param>
    void Apply(IPublishEndpointBrokerTopologyBuilder builder);
}
