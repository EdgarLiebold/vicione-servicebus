using ViciOne.ServiceBus.ActiveMq;
using ViciOne.ServiceBus.ActiveMq.Topology;

#nullable enable
namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Defines the contract for active mq consume topology configurator.
/// </summary>
public interface IActiveMqConsumeTopologyConfigurator :
    IConsumeTopologyConfigurator,
    IActiveMqConsumeTopology
{
    /// <summary>
    /// Gets or sets the consumer endpoint queue name formatter value.
    /// </summary>
    new IActiveMqConsumerEndpointQueueNameFormatter? ConsumerEndpointQueueNameFormatter { set; }

    /// <summary>
    /// Gets or sets the temporary queue name formatter value.
    /// </summary>
    new IActiveMqTemporaryQueueNameFormatter? TemporaryQueueNameFormatter { set; }

    /// <summary>
    /// Gets message topology.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    new IActiveMqMessageConsumeTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>
    /// Adds specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    void AddSpecification(IActiveMqConsumeTopologySpecification specification);
}
