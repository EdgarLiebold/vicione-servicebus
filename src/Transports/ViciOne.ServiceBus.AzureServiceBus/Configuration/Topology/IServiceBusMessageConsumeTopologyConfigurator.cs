using System;
using ViciOne.ServiceBus.AzureServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Configures the Azure Service Bus subscription used to consume a message contract.</summary>
/// <typeparam name="TMessage">The consumed message contract.</typeparam>
public interface IServiceBusMessageConsumeTopologyConfigurator<TMessage> :
    IMessageConsumeTopologyConfigurator<TMessage>,
    IServiceBusMessageConsumeTopology<TMessage>
    where TMessage : class
{
    /// <summary>Adds a subscription to this message type's publish topic.</summary>
    /// <param name="subscriptionName">The subscription name.</param>
    /// <param name="configure">Optionally configures the subscription.</param>
    void Subscribe(string subscriptionName, Action<IServiceBusSubscriptionConfigurator>? configure = null);
}


/// <summary>Applies runtime-typed Azure Service Bus consume topology to an endpoint builder.</summary>
public interface IServiceBusMessageConsumeTopologyConfigurator :
    IMessageConsumeTopologyConfigurator
{
    /// <summary>Applies the message subscriptions to a receive-endpoint topology builder.</summary>
    /// <param name="builder">The topology builder receiving the subscriptions.</param>
    void Apply(IReceiveEndpointBrokerTopologyBuilder builder);
}
