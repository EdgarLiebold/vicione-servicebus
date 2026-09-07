using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>Owns the endpoint, host, routing, and observer configuration for one in-memory bus.</summary>
public class InMemoryBusConfiguration :
    InMemoryEndpointConfiguration,
    IInMemoryBusConfiguration
{
    readonly BusObservable _busObservers;

    /// <summary>Initializes a bus configuration over a shared in-memory topology.</summary>
    /// <param name="topologyConfiguration">The topology used to address and connect in-memory entities.</param>
    /// <param name="baseAddress">The transport base address, or <see langword="null" /> for the default.</param>
    public InMemoryBusConfiguration(IInMemoryTopologyConfiguration topologyConfiguration, Uri? baseAddress)
        : base(topologyConfiguration ?? throw new ArgumentNullException(nameof(topologyConfiguration)))
    {
        MessageRoutes = new MessageRouteTable();
        HostConfiguration = new InMemoryHostConfiguration(this, baseAddress, topologyConfiguration);
        BusEndpointConfiguration = CreateEndpointConfiguration(true);

        _busObservers = new BusObservable();
    }

    IHostConfiguration IBusConfiguration.HostConfiguration => HostConfiguration;
    IMessageRouteTable IBusConfiguration.MessageRoutes => MessageRoutes;
    IEndpointConfiguration IBusConfiguration.BusEndpointConfiguration => BusEndpointConfiguration;
    IBusObserver IBusConfiguration.BusObservers => _busObservers;

    /// <summary>Gets the receive-endpoint configuration owned by the bus runtime.</summary>
    public IInMemoryEndpointConfiguration BusEndpointConfiguration { get; }
    /// <summary>Gets the in-memory transport host configuration.</summary>
    public IInMemoryHostConfiguration HostConfiguration { get; }
    /// <summary>Gets the route table frozen when bus construction begins.</summary>
    public MessageRouteTable MessageRoutes { get; }

    /// <summary>Registers an observer for bus lifecycle events.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectBusObserver(IBusObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _busObservers.Connect(observer);
    }

    /// <summary>Registers an observer for receive-endpoint configuration events.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectEndpointConfigurationObserver(IEndpointConfigurationObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return HostConfiguration.ConnectEndpointConfigurationObserver(observer);
    }
}
