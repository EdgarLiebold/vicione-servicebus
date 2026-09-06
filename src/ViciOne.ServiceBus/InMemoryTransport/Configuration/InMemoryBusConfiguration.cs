using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>Stores and validates in memory bus configuration.</summary>
public class InMemoryBusConfiguration :
    InMemoryEndpointConfiguration,
    IInMemoryBusConfiguration
{
    readonly BusObservable _busObservers;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="topologyConfiguration">The topology configuration.</param>
    /// <param name="baseAddress">The base address.</param>
    public InMemoryBusConfiguration(IInMemoryTopologyConfiguration topologyConfiguration, Uri? baseAddress)
        : base(topologyConfiguration)
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

    /// <summary>Gets the bus endpoint configuration.</summary>
    public IInMemoryEndpointConfiguration BusEndpointConfiguration { get; }
    /// <summary>Gets the host configuration.</summary>
    public IInMemoryHostConfiguration HostConfiguration { get; }
    /// <summary>Gets the message routes.</summary>
    public MessageRouteTable MessageRoutes { get; }

    /// <summary>Connects bus observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectBusObserver(IBusObserver observer)
    {
        return _busObservers.Connect(observer);
    }

    /// <summary>Connects endpoint configuration observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectEndpointConfigurationObserver(IEndpointConfigurationObserver observer)
    {
        return HostConfiguration.ConnectEndpointConfigurationObserver(observer);
    }
}
