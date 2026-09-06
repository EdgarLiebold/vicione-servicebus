using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>Defines in memory bus configuration.</summary>
public interface IInMemoryBusConfiguration :
    IBusConfiguration,
    IInMemoryEndpointConfiguration
{
    /// <summary>Gets the host configuration.</summary>
    new IInMemoryHostConfiguration HostConfiguration { get; }

    /// <summary>Gets the bus endpoint configuration.</summary>
    new IInMemoryEndpointConfiguration BusEndpointConfiguration { get; }

    /// <summary>Create an endpoint configuration on the bus, which can later be turned into a receive endpoint.</summary>
    /// <param name="isBusEndpoint">The is bus endpoint.</param>
    /// <returns>The created endpoint configuration.</returns>
    IInMemoryEndpointConfiguration CreateEndpointConfiguration(bool isBusEndpoint = false);
}
