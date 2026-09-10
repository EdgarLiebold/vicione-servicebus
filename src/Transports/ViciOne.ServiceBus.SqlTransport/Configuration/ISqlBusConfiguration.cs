using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>Defines sql bus configuration.</summary>
public interface ISqlBusConfiguration :
    IBusConfiguration
{
    /// <summary>Gets the host configuration.</summary>
    new ISqlHostConfiguration HostConfiguration { get; }

    /// <summary>Gets the bus endpoint configuration.</summary>
    new ISqlEndpointConfiguration BusEndpointConfiguration { get; }

    /// <summary>Gets the topology.</summary>
    new ISqlTopologyConfiguration Topology { get; }

    /// <summary>Creates endpoint configuration.</summary>
    /// <param name="isBusEndpoint">The is bus endpoint.</param>
    /// <returns>The created endpoint configuration.</returns>
    ISqlEndpointConfiguration CreateEndpointConfiguration(bool isBusEndpoint = false);
}
