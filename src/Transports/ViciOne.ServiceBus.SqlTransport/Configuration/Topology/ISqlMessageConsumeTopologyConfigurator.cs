using System;
using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Configures sql message consume topology.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface ISqlMessageConsumeTopologyConfigurator<TMessage> :
    IMessageConsumeTopologyConfigurator<TMessage>,
    ISqlMessageConsumeTopology<TMessage>
    where TMessage : class
{
    /// <summary>Adds the topic subscriptions for this message type.</summary>
    /// <param name="configure">The callback that configures the topic subscription.</param>
    void Subscribe(Action<ISqlTopicSubscriptionConfigurator>? configure = null);
}
