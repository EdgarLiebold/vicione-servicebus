namespace ViciOne.ServiceBus.Configuration;

/// <summary>The configuration of a bus.</summary>
public interface IBusConfiguration :
    IEndpointConfiguration,
    IBusObserverConnector,
    IEndpointConfigurationObserverConnector
{
    /// <summary>Gets the host configuration.</summary>
    IHostConfiguration HostConfiguration { get; }

    /// <summary>Gets the message routes.</summary>
    IMessageRouteTable MessageRoutes { get; }

    /// <summary>Gets the bus endpoint configuration.</summary>
    IEndpointConfiguration BusEndpointConfiguration { get; }

    /// <summary>Gets the bus observers.</summary>
    IBusObserver BusObservers { get; }
}
