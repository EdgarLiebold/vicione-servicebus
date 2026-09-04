using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.AzureServiceBus;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;
using ViciOne.ServiceBus.AzureServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Defines the contract for service bus message publish topology.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IServiceBusMessagePublishTopology<TMessage> :
    IMessagePublishTopology<TMessage>,
    IServiceBusMessagePublishTopology
    where TMessage : class
{
    /// <summary>
    /// Returns the topic options for the message type
    /// </summary>
    CreateTopicOptions CreateTopicOptions { get; }

    /// <summary>
    /// Gets send settings.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    SendSettings GetSendSettings();

    /// <summary>
    /// Gets subscription configurator.
    /// </summary>
    /// <param name="subscriptionName">The subscription name value.</param>
    /// <returns>The result of the operation.</returns>
    ServiceBusSubscriptionConfigurator GetSubscriptionConfigurator(string subscriptionName);
}


/// <summary>
/// Defines the contract for service bus message publish topology.
/// </summary>
public interface IServiceBusMessagePublishTopology
{
    /// <summary>
    /// Apply the message topology to the builder, including any implemented types
    /// </summary>
    /// <param name="builder">The topology builder</param>
    void Apply(IPublishEndpointBrokerTopologyBuilder builder);
}
