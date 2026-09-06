using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.AzureServiceBus;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;
using ViciOne.ServiceBus.AzureServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Describes the Azure Service Bus publish topic for a message contract.</summary>
/// <typeparam name="TMessage">The published message contract.</typeparam>
public interface IServiceBusMessagePublishTopology<TMessage> :
    IMessagePublishTopology<TMessage>,
    IServiceBusMessagePublishTopology
    where TMessage : class
{
    /// <summary>Gets the topic declaration options for the message contract.</summary>
    CreateTopicOptions CreateTopicOptions { get; }

    /// <summary>Builds the settings used to publish to the message topic.</summary>
    /// <returns>The topic send settings.</returns>
    SendSettings GetSendSettings();

    /// <summary>Gets a configurator for a subscription to the message topic.</summary>
    /// <param name="subscriptionName">The subscription name.</param>
    /// <returns>A configurator bound to the message topic.</returns>
    ServiceBusSubscriptionConfigurator GetSubscriptionConfigurator(string subscriptionName);
}


/// <summary>Applies runtime-typed Azure Service Bus publish topology to a broker builder.</summary>
public interface IServiceBusMessagePublishTopology
{
    /// <summary>Applies the message topic and implemented-message topology to a broker builder.</summary>
    /// <param name="builder">The publish topology builder.</param>
    void Apply(IPublishEndpointBrokerTopologyBuilder builder);
}
