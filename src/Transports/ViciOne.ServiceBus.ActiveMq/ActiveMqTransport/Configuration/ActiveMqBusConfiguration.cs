using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>Coordinates ActiveMQ bus, host, endpoint, topology, route, and observer configuration.</summary>
public class ActiveMqBusConfiguration :
    ActiveMqEndpointConfiguration,
    IActiveMqBusConfiguration
{
    readonly BusObservable _busObservers;

    /// <summary>Creates an ActiveMQ bus configuration with its host and bus endpoint.</summary>
    /// <param name="topologyConfiguration">The ActiveMQ topology configuration.</param>
    public ActiveMqBusConfiguration(IActiveMqTopologyConfiguration topologyConfiguration)
        : base(topologyConfiguration)
    {
        MessageRoutes = new MessageRouteTable();
        HostConfiguration = new ActiveMqHostConfiguration(this, topologyConfiguration);
        BusEndpointConfiguration = CreateEndpointConfiguration(true);

        _busObservers = new BusObservable();
    }

    IHostConfiguration IBusConfiguration.HostConfiguration => HostConfiguration;
    IMessageRouteTable IBusConfiguration.MessageRoutes => MessageRoutes;
    IEndpointConfiguration IBusConfiguration.BusEndpointConfiguration => BusEndpointConfiguration;
    IBusObserver IBusConfiguration.BusObservers => _busObservers;

    /// <summary>Gets the endpoint configuration used by the bus itself.</summary>
    public IActiveMqEndpointConfiguration BusEndpointConfiguration { get; }
    /// <summary>Gets the ActiveMQ host configuration.</summary>
    public IActiveMqHostConfiguration HostConfiguration { get; }
    /// <summary>Gets the configured message-route table.</summary>
    public MessageRouteTable MessageRoutes { get; }

    /// <summary>Connects an observer to the bus lifecycle.</summary>
    /// <param name="observer">The bus observer.</param>
    /// <returns>A handle that disconnects the observer.</returns>
    public ConnectHandle ConnectBusObserver(IBusObserver observer)
    {
        return _busObservers.Connect(observer);
    }

    /// <summary>Connects an observer to receive-endpoint configuration events.</summary>
    /// <param name="observer">The endpoint-configuration observer.</param>
    /// <returns>A handle that disconnects the observer.</returns>
    public ConnectHandle ConnectEndpointConfigurationObserver(IEndpointConfigurationObserver observer)
    {
        return HostConfiguration.ConnectEndpointConfigurationObserver(observer);
    }
}
