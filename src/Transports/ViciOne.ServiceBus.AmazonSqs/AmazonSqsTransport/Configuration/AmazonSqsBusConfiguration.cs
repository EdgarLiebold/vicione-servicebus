using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.AmazonSqs.Configuration;

/// <summary>
/// Provides an amazon sqs bus configuration implementation.
/// </summary>
public class AmazonSqsBusConfiguration :
    AmazonSqsEndpointConfiguration,
    IAmazonSqsBusConfiguration
{
    readonly BusObservable _busObservers;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="topologyConfiguration">The topology configuration value.</param>
    public AmazonSqsBusConfiguration(IAmazonSqsTopologyConfiguration topologyConfiguration)
        : base(topologyConfiguration)
    {
        MessageRoutes = new MessageRouteTable();
        HostConfiguration = new AmazonSqsHostConfiguration(this, topologyConfiguration);
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
    public IAmazonSqsEndpointConfiguration BusEndpointConfiguration { get; }
    /// <summary>
    /// Gets the host configuration value.
    /// </summary>
    public IAmazonSqsHostConfiguration HostConfiguration { get; }
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
