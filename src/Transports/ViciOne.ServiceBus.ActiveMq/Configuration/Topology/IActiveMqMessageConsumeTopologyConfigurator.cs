using System;
using ViciOne.ServiceBus.ActiveMq.Topology;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Defines the contract for active mq message consume topology configurator.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IActiveMqMessageConsumeTopologyConfigurator<TMessage> :
    IMessageConsumeTopologyConfigurator<TMessage>,
    IActiveMqMessageConsumeTopology<TMessage>
    where TMessage : class
{
    /// <summary>
    /// Adds the exchange bindings for this message type
    /// </summary>
    /// <param name="configure">Configure the binding and the exchange</param>
    void Bind(Action<IActiveMqTopicBindingConfigurator>? configure = null);
}


/// <summary>
/// Defines the contract for active mq message consume topology configurator.
/// </summary>
public interface IActiveMqMessageConsumeTopologyConfigurator :
    IMessageConsumeTopologyConfigurator
{
    /// <summary>
    /// Apply the message topology to the builder
    /// </summary>
    /// <param name="builder"></param>
    void Apply(IReceiveEndpointBrokerTopologyBuilder builder);
}
