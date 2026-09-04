using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>
/// Defines the contract for active mq endpoint configuration.
/// </summary>
public interface IActiveMqEndpointConfiguration :
    IEndpointConfiguration
{
    /// <summary>
    /// Gets the topology value.
    /// </summary>
    new IActiveMqTopologyConfiguration Topology { get; }
}
