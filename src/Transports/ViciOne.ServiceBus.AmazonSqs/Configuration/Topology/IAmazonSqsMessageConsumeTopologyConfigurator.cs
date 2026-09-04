using System;
using ViciOne.ServiceBus.AmazonSqs.Topology;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Defines the contract for amazon sqs message consume topology configurator.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IAmazonSqsMessageConsumeTopologyConfigurator<TMessage> :
    IMessageConsumeTopologyConfigurator<TMessage>,
    IAmazonSqsMessageConsumeTopology<TMessage>
    where TMessage : class
{
    /// <summary>
    /// Adds the exchange bindings for this message type
    /// </summary>
    /// <param name="configure">Configure the binding and the exchange</param>
    void Subscribe(Action<IAmazonSqsTopicSubscriptionConfigurator>? configure = null);
}


/// <summary>
/// Defines the contract for amazon sqs message consume topology configurator.
/// </summary>
public interface IAmazonSqsMessageConsumeTopologyConfigurator :
    IMessageConsumeTopologyConfigurator
{
    /// <summary>
    /// Apply the message topology to the builder
    /// </summary>
    /// <param name="builder"></param>
    void Apply(IReceiveEndpointBrokerTopologyBuilder builder);
}
