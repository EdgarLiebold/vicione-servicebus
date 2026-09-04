using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>
/// Provides a sql bus configuration implementation.
/// </summary>
public class SqlBusConfiguration :
    SqlEndpointConfiguration,
    ISqlBusConfiguration
{
    readonly BusObservable _busObservers;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="topologyConfiguration">The topology configuration value.</param>
    public SqlBusConfiguration(ISqlTopologyConfiguration topologyConfiguration)
        : base(topologyConfiguration)
    {
        MessageRoutes = new MessageRouteTable();
        HostConfiguration = new SqlHostConfiguration(this, topologyConfiguration);
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
    public ISqlEndpointConfiguration BusEndpointConfiguration { get; }
    /// <summary>
    /// Gets the host configuration value.
    /// </summary>
    public ISqlHostConfiguration HostConfiguration { get; }
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
