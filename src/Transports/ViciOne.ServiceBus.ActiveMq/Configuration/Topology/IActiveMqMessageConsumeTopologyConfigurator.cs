using System;
using ViciOne.ServiceBus.ActiveMq.Topology;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Configures ActiveMQ consume bindings for one message type.</summary>
/// <typeparam name="TMessage">The consumed message type.</typeparam>
public interface IActiveMqMessageConsumeTopologyConfigurator<TMessage> :
    IMessageConsumeTopologyConfigurator<TMessage>,
    IActiveMqMessageConsumeTopology<TMessage>
    where TMessage : class
{
    /// <summary>Adds the topic subscriptions for this message type.</summary>
    /// <param name="configure">The callback that configures the topic subscription.</param>
    void Bind(Action<IActiveMqTopicBindingConfigurator>? configure = null);
}


/// <summary>Applies untyped ActiveMQ message consume topology to a receive endpoint.</summary>
public interface IActiveMqMessageConsumeTopologyConfigurator :
    IMessageConsumeTopologyConfigurator
{
    /// <summary>Applies the message topology to a receive-topology builder.</summary>
    /// <param name="builder">The builder to update.</param>
    void Apply(IReceiveEndpointBrokerTopologyBuilder builder);
}
