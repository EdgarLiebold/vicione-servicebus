using System;
using ViciOne.ServiceBus.ActiveMq;
using ViciOne.ServiceBus.ActiveMq.Topology;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Defines the contract for active mq consume topology.
/// </summary>
public interface IActiveMqConsumeTopology :
    IConsumeTopology
{
    /// <summary>
    /// Gets the consumer endpoint queue name formatter value.
    /// </summary>
    IActiveMqConsumerEndpointQueueNameFormatter? ConsumerEndpointQueueNameFormatter { get; }

    /// <summary>
    /// Gets the temporary queue name formatter value.
    /// </summary>
    IActiveMqTemporaryQueueNameFormatter? TemporaryQueueNameFormatter { get; }

    /// <summary>
    /// Gets message topology.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    new IActiveMqMessageConsumeTopology<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>
    /// Apply the entire topology to the builder
    /// </summary>
    /// <param name="builder"></param>
    void Apply(IReceiveEndpointBrokerTopologyBuilder builder);

    /// <summary>
    /// Bind an exchange, using the configurator
    /// </summary>
    /// <param name="topicName"></param>
    /// <param name="configure"></param>
    void Bind(string topicName, Action<IActiveMqTopicBindingConfigurator>? configure = null);
}
