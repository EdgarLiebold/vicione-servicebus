using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>Combines bus, endpoint, host, and topology configuration for Azure Service Bus.</summary>
public interface IServiceBusBusConfiguration :
    IBusConfiguration,
    IServiceBusEndpointConfiguration
{
    /// <summary>Gets the Azure Service Bus host configuration.</summary>
    new IServiceBusHostConfiguration HostConfiguration { get; }

    /// <summary>Gets the endpoint configuration used by the bus endpoint.</summary>
    new IServiceBusEndpointConfiguration BusEndpointConfiguration { get; }

    /// <summary>Gets the transport-specific topology configuration.</summary>
    new IServiceBusTopologyConfiguration Topology { get; }

    /// <summary>Creates a child endpoint configuration with an isolated copy of the current topology settings.</summary>
    /// <param name="isBusEndpoint">Whether the child represents the bus endpoint.</param>
    /// <returns>The child endpoint configuration.</returns>
    IServiceBusEndpointConfiguration CreateEndpointConfiguration(bool isBusEndpoint = false);
}
