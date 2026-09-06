using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>Owns the Azure Service Bus host, bus endpoint, message routes, and bus observers.</summary>
public class ServiceBusBusConfiguration :
    ServiceBusEndpointConfiguration,
    IServiceBusBusConfiguration
{
    readonly BusObservable _busObservers;

    /// <summary>Initializes a bus configuration with a host and a dedicated bus endpoint.</summary>
    /// <param name="topologyConfiguration">The topology configuration inherited by the bus endpoint.</param>
    public ServiceBusBusConfiguration(IServiceBusTopologyConfiguration topologyConfiguration)
        : base(topologyConfiguration)
    {
        MessageRoutes = new MessageRouteTable();
        HostConfiguration = new ServiceBusHostConfiguration(this, topologyConfiguration);
        BusEndpointConfiguration = CreateEndpointConfiguration(true);

        _busObservers = new BusObservable();
    }

    IHostConfiguration IBusConfiguration.HostConfiguration => HostConfiguration;
    IMessageRouteTable IBusConfiguration.MessageRoutes => MessageRoutes;
    IEndpointConfiguration IBusConfiguration.BusEndpointConfiguration => BusEndpointConfiguration;
    IBusObserver IBusConfiguration.BusObservers => _busObservers;

    /// <summary>Gets the endpoint-level configuration used for the bus endpoint.</summary>
    public IServiceBusEndpointConfiguration BusEndpointConfiguration { get; }
    /// <summary>Gets the namespace host configuration.</summary>
    public IServiceBusHostConfiguration HostConfiguration { get; }
    /// <summary>Gets the message route table populated during bus configuration.</summary>
    public MessageRouteTable MessageRoutes { get; }

    /// <summary>Subscribes an observer to bus lifecycle events.</summary>
    /// <param name="observer">The bus observer to subscribe.</param>
    /// <returns>A handle that disconnects the observer.</returns>
    public ConnectHandle ConnectBusObserver(IBusObserver observer)
    {
        return _busObservers.Connect(observer);
    }

    /// <summary>Subscribes an observer to receive-endpoint configuration events.</summary>
    /// <param name="observer">The endpoint-configuration observer to subscribe.</param>
    /// <returns>A handle that disconnects the observer.</returns>
    public ConnectHandle ConnectEndpointConfigurationObserver(IEndpointConfigurationObserver observer)
    {
        return HostConfiguration.ConnectEndpointConfigurationObserver(observer);
    }
}
