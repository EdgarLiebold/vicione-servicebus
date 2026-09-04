namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// The configuration of a bus
/// </summary>
public interface IBusConfiguration :
    IEndpointConfiguration,
    IBusObserverConnector,
    IEndpointConfigurationObserverConnector
{
    /// <summary>
    /// Gets the host configuration value.
    /// </summary>
    IHostConfiguration HostConfiguration { get; }

    /// <summary>
    /// Gets the message routes value.
    /// </summary>
    IMessageRouteTable MessageRoutes { get; }

    /// <summary>
    /// Gets the bus endpoint configuration value.
    /// </summary>
    IEndpointConfiguration BusEndpointConfiguration { get; }

    /// <summary>
    /// Gets the bus observers value.
    /// </summary>
    IBusObserver BusObservers { get; }
}
