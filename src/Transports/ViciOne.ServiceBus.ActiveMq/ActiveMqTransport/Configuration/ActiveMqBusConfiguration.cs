using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>
/// Provides an active mq bus configuration implementation.
/// </summary>
public class ActiveMqBusConfiguration :
    ActiveMqEndpointConfiguration,
    IActiveMqBusConfiguration
{
    readonly BusObservable _busObservers;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="topologyConfiguration">The topology configuration value.</param>
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

    /// <summary>
    /// Gets the bus endpoint configuration value.
    /// </summary>
    public IActiveMqEndpointConfiguration BusEndpointConfiguration { get; }
    /// <summary>
    /// Gets the host configuration value.
    /// </summary>
    public IActiveMqHostConfiguration HostConfiguration { get; }
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
