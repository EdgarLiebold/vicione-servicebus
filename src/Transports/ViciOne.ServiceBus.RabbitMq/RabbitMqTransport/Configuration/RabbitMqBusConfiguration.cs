using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>Composes RabbitMQ bus, host, endpoint, topology, route, and observer configuration.</summary>
public class RabbitMqBusConfiguration :
    RabbitMqEndpointConfiguration,
    IRabbitMqBusConfiguration
{
    readonly BusObservable _busObservers;

    /// <summary>Creates bus configuration around a shared RabbitMQ topology.</summary>
    /// <param name="topologyConfiguration">The RabbitMQ topology configuration.</param>
    public RabbitMqBusConfiguration(IRabbitMqTopologyConfiguration topologyConfiguration)
        : base(topologyConfiguration)
    {
        MessageRoutes = new MessageRouteTable();
        HostConfiguration = new RabbitMqHostConfiguration(this, topologyConfiguration);
        BusEndpointConfiguration = CreateEndpointConfiguration(true);

        _busObservers = new BusObservable();
    }

    IHostConfiguration IBusConfiguration.HostConfiguration => HostConfiguration;
    IMessageRouteTable IBusConfiguration.MessageRoutes => MessageRoutes;
    IEndpointConfiguration IBusConfiguration.BusEndpointConfiguration => BusEndpointConfiguration;
    IBusObserver IBusConfiguration.BusObservers => _busObservers;

    /// <summary>Gets the configuration reserved for the bus endpoint.</summary>
    public IRabbitMqEndpointConfiguration BusEndpointConfiguration { get; }
    /// <summary>Gets the RabbitMQ host and receive-endpoint configuration.</summary>
    public IRabbitMqHostConfiguration HostConfiguration { get; }
    /// <summary>Gets the bus message-route table.</summary>
    public MessageRouteTable MessageRoutes { get; }

    /// <summary>Registers a bus lifecycle observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectBusObserver(IBusObserver observer)
    {
        return _busObservers.Connect(observer);
    }

    /// <summary>Registers an observer for receive-endpoint configuration.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectEndpointConfigurationObserver(IEndpointConfigurationObserver observer)
    {
        return HostConfiguration.ConnectEndpointConfigurationObserver(observer);
    }
}
