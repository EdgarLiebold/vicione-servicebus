namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides the host, endpoint, routing, and observer state required to build a bus.</summary>
public interface IBusConfiguration :
    IEndpointConfiguration,
    IBusObserverConnector,
    IEndpointConfigurationObserverConnector
{
    /// <summary>Gets the transport host configuration.</summary>
    IHostConfiguration HostConfiguration { get; }

    /// <summary>Gets the route table that is frozen when bus construction begins.</summary>
    IMessageRouteTable MessageRoutes { get; }

    /// <summary>Gets the configuration of the receive endpoint owned by the bus.</summary>
    IEndpointConfiguration BusEndpointConfiguration { get; }

    /// <summary>Gets the observer fan-out for bus lifecycle events.</summary>
    IBusObserver BusObservers { get; }
}
