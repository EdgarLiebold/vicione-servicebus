using System;
using ViciOne.ServiceBus.SqlTransport.Topology;

#nullable enable
namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>
/// Defines the contract for sql message consume topology configurator.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface ISqlMessageConsumeTopologyConfigurator<TMessage> :
    IMessageConsumeTopologyConfigurator<TMessage>,
    ISqlMessageConsumeTopology<TMessage>
    where TMessage : class
{
    /// <summary>
    /// Adds the exchange bindings for this message type
    /// </summary>
    /// <param name="configure">Configure the binding and the exchange</param>
    void Subscribe(Action<ISqlTopicSubscriptionConfigurator>? configure = null);
}


/// <summary>
/// Defines the contract for db message consume topology configurator.
/// </summary>
public interface IDbMessageConsumeTopologyConfigurator :
    IMessageConsumeTopologyConfigurator
{
    /// <summary>
    /// Apply the message topology to the builder
    /// </summary>
    /// <param name="builder"></param>
    void Apply(IReceiveEndpointBrokerTopologyBuilder builder);
}
