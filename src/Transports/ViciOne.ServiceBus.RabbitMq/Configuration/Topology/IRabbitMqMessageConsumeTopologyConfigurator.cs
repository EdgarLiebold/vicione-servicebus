using System;
using ViciOne.ServiceBus.RabbitMq.Topology;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Defines the contract for rabbit mq message consume topology configurator.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IRabbitMqMessageConsumeTopologyConfigurator<TMessage> :
    IMessageConsumeTopologyConfigurator<TMessage>,
    IRabbitMqMessageConsumeTopology<TMessage>
    where TMessage : class
{
    /// <summary>
    /// Adds the exchange bindings for this message type
    /// </summary>
    /// <param name="configure">Configure the binding and the exchange</param>
    void Bind(Action<IRabbitMqExchangeBindingConfigurator>? configure = null);
}


/// <summary>
/// Defines the contract for rabbit mq message consume topology configurator.
/// </summary>
public interface IRabbitMqMessageConsumeTopologyConfigurator :
    IMessageConsumeTopologyConfigurator
{
    /// <summary>
    /// Apply the message topology to the builder
    /// </summary>
    /// <param name="builder"></param>
    void Apply(IReceiveEndpointBrokerTopologyBuilder builder);
}
