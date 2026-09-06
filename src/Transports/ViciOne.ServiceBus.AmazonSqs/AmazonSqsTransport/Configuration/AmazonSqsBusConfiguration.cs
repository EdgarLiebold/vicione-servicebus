using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.AmazonSqs.Configuration;

/// <summary>Coordinates Amazon SQS host, endpoint, topology, routing, and observer configuration for a bus.</summary>
public class AmazonSqsBusConfiguration :
    AmazonSqsEndpointConfiguration,
    IAmazonSqsBusConfiguration
{
    readonly BusObservable _busObservers;

    /// <summary>Initializes an Amazon SQS bus configuration.</summary>
    /// <param name="topologyConfiguration">The transport topology configuration shared by bus endpoints.</param>
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

    /// <summary>Gets the configuration inherited by the bus endpoint.</summary>
    public IAmazonSqsEndpointConfiguration BusEndpointConfiguration { get; }
    /// <summary>Gets the Amazon SQS host configuration.</summary>
    public IAmazonSqsHostConfiguration HostConfiguration { get; }
    /// <summary>Gets the table of explicit message routes.</summary>
    public MessageRouteTable MessageRoutes { get; }

    /// <summary>Registers an observer for bus lifecycle events.</summary>
    /// <param name="observer">The bus observer.</param>
    /// <returns>A handle that disconnects the observer.</returns>
    public ConnectHandle ConnectBusObserver(IBusObserver observer)
    {
        return _busObservers.Connect(observer);
    }

    /// <summary>Registers an observer for receive-endpoint configuration events.</summary>
    /// <param name="observer">The endpoint configuration observer.</param>
    /// <returns>A handle that disconnects the observer.</returns>
    public ConnectHandle ConnectEndpointConfigurationObserver(IEndpointConfigurationObserver observer)
    {
        return HostConfiguration.ConnectEndpointConfigurationObserver(observer);
    }
}
