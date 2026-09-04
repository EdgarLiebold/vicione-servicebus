using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>
/// Provides an in memory bus configuration implementation.
/// </summary>
public class InMemoryBusConfiguration :
    InMemoryEndpointConfiguration,
    IInMemoryBusConfiguration
{
    readonly BusObservable _busObservers;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="topologyConfiguration">The topology configuration value.</param>
    /// <param name="baseAddress">The base address value.</param>
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

    /// <summary>
    /// Gets the bus endpoint configuration value.
    /// </summary>
    public IInMemoryEndpointConfiguration BusEndpointConfiguration { get; }
    /// <summary>
    /// Gets the host configuration value.
    /// </summary>
    public IInMemoryHostConfiguration HostConfiguration { get; }
    /// <summary>
    /// Gets the message routes value.
    /// </summary>
    public MessageRouteTable MessageRoutes { get; }

    /// <summary>
    /// Connects bus observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectBusObserver(IBusObserver observer)
    {
        return _busObservers.Connect(observer);
    }

    /// <summary>
    /// Connects endpoint configuration observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectEndpointConfigurationObserver(IEndpointConfigurationObserver observer)
    {
        return HostConfiguration.ConnectEndpointConfigurationObserver(observer);
    }
}
