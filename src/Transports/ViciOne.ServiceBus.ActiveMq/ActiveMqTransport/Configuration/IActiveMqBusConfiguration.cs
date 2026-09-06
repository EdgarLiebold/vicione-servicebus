using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>Coordinates ActiveMQ host, endpoint, and topology configuration for a bus.</summary>
public interface IActiveMqBusConfiguration :
    IBusConfiguration
{
    /// <summary>Gets the ActiveMQ host configuration.</summary>
    new IActiveMqHostConfiguration HostConfiguration { get; }

    /// <summary>Gets the configuration of the bus endpoint.</summary>
    new IActiveMqEndpointConfiguration BusEndpointConfiguration { get; }

    /// <summary>Gets the ActiveMQ topology configuration.</summary>
    new IActiveMqTopologyConfiguration Topology { get; }

    /// <summary>Creates a child endpoint configuration that can be built as a receive endpoint.</summary>
    /// <param name="isBusEndpoint">Whether the child represents the bus endpoint.</param>
    /// <returns>The child ActiveMQ endpoint configuration.</returns>
    IActiveMqEndpointConfiguration CreateEndpointConfiguration(bool isBusEndpoint = false);
}
