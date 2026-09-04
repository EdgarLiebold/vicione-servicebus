using System;
using ViciOne.ServiceBus.AzureServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Defines the contract for service bus message consume topology configurator.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IServiceBusMessageConsumeTopologyConfigurator<TMessage> :
    IMessageConsumeTopologyConfigurator<TMessage>,
    IServiceBusMessageConsumeTopology<TMessage>
    where TMessage : class
{
    /// <summary>
    /// Create a topic subscription for the message type
    /// </summary>
    /// <param name="subscriptionName"></param>
    /// <param name="configure">Configure the binding and the exchange</param>
    void Subscribe(string subscriptionName, Action<IServiceBusSubscriptionConfigurator>? configure = null);
}


/// <summary>
/// Defines the contract for service bus message consume topology configurator.
/// </summary>
public interface IServiceBusMessageConsumeTopologyConfigurator :
    IMessageConsumeTopologyConfigurator
{
    /// <summary>
    /// Apply the message topology to the builder
    /// </summary>
    /// <param name="builder"></param>
    void Apply(IReceiveEndpointBrokerTopologyBuilder builder);
}
