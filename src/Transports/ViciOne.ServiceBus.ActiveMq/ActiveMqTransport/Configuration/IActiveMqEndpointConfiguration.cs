using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>Exposes ActiveMQ topology configuration for an endpoint.</summary>
public interface IActiveMqEndpointConfiguration :
    IEndpointConfiguration
{
    /// <summary>Gets the endpoint's ActiveMQ topology configuration.</summary>
    new IActiveMqTopologyConfiguration Topology { get; }
}
