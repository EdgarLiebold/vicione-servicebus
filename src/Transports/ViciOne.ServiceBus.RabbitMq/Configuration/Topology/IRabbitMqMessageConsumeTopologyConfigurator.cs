using System;
using ViciOne.ServiceBus.RabbitMq.Topology;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Configures RabbitMQ consume topology for one message contract.</summary>
/// <typeparam name="TMessage">The message contract.</typeparam>
public interface IRabbitMqMessageConsumeTopologyConfigurator<TMessage> :
    IMessageConsumeTopologyConfigurator<TMessage>,
    IRabbitMqMessageConsumeTopology<TMessage>
    where TMessage : class
{
    /// <summary>Adds a binding from this message contract's publish exchange to the receive endpoint exchange.</summary>
    /// <param name="configure">An optional callback that customizes the source exchange and binding.</param>
    void Bind(Action<IRabbitMqExchangeBindingConfigurator>? configure = null);
}


/// <summary>Applies one message contract's RabbitMQ consume topology to an endpoint.</summary>
public interface IRabbitMqMessageConsumeTopologyConfigurator :
    IMessageConsumeTopologyConfigurator
{
    /// <summary>Applies the message exchange bindings to a receive-endpoint topology builder.</summary>
    /// <param name="builder">The receive-endpoint topology builder.</param>
    void Apply(IReceiveEndpointBrokerTopologyBuilder builder);
}
