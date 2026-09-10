using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>Combines host, endpoint, route, and topology configuration for one in-memory bus.</summary>
internal interface IInMemoryBusConfiguration :
    IBusConfiguration,
    IInMemoryEndpointConfiguration
{
    /// <summary>Gets the in-memory host configuration.</summary>
    new IInMemoryHostConfiguration HostConfiguration { get; }

    /// <summary>Gets the endpoint configuration used by the bus runtime.</summary>
    new IInMemoryEndpointConfiguration BusEndpointConfiguration { get; }

    /// <summary>Creates a child endpoint configuration that inherits the bus pipelines.</summary>
    /// <param name="isBusEndpoint">Whether the child represents the bus endpoint.</param>
    /// <returns>The independently materialized child configuration.</returns>
    IInMemoryEndpointConfiguration CreateEndpointConfiguration(bool isBusEndpoint = false);
}
