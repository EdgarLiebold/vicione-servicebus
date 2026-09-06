using ViciOne.ServiceBus.ActiveMq;
using ViciOne.ServiceBus.ActiveMq.Topology;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Configures ActiveMQ receive bindings and per-message consume topology.</summary>
public interface IActiveMqConsumeTopologyConfigurator :
    IConsumeTopologyConfigurator,
    IActiveMqConsumeTopology
{
    /// <summary>Sets the formatter for virtual-topic consumer queues or subscriptions.</summary>
    new IActiveMqConsumerEndpointQueueNameFormatter? ConsumerEndpointQueueNameFormatter { set; }

    /// <summary>Sets the formatter applied to generated temporary queue names.</summary>
    new IActiveMqTemporaryQueueNameFormatter? TemporaryQueueNameFormatter { set; }

    /// <summary>Gets configurable consume topology for a message type.</summary>
    /// <typeparam name="T">The consumed message type.</typeparam>
    /// <returns>The ActiveMQ message consume-topology configurator.</returns>
    new IActiveMqMessageConsumeTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>Adds a receive-topology specification.</summary>
    /// <param name="specification">The specification to add.</param>
    void AddSpecification(IActiveMqConsumeTopologySpecification specification);
}
