using System;
using ViciOne.ServiceBus.ActiveMq;
using ViciOne.ServiceBus.ActiveMq.Topology;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Exposes ActiveMQ receive bindings and per-message consume topology.</summary>
public interface IActiveMqConsumeTopology :
    IConsumeTopology
{
    /// <summary>Gets the formatter for virtual-topic consumer queues or subscriptions.</summary>
    IActiveMqConsumerEndpointQueueNameFormatter? ConsumerEndpointQueueNameFormatter { get; }

    /// <summary>Gets the formatter applied to generated temporary queue names.</summary>
    IActiveMqTemporaryQueueNameFormatter? TemporaryQueueNameFormatter { get; }

    /// <summary>Gets consume topology for a message type.</summary>
    /// <typeparam name="T">The consumed message type.</typeparam>
    /// <returns>The ActiveMQ message consume topology.</returns>
    new IActiveMqMessageConsumeTopology<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>Applies the complete consume topology to a receive-topology builder.</summary>
    /// <param name="builder">The builder to update.</param>
    void Apply(IReceiveEndpointBrokerTopologyBuilder builder);

    /// <summary>Binds a topic to the receive endpoint using the supplied configurator.</summary>
    /// <param name="topicName">The topic name.</param>
    /// <param name="configure">An optional callback that configures the topic binding.</param>
    void Bind(string topicName, Action<IActiveMqTopicBindingConfigurator>? configure = null);
}
