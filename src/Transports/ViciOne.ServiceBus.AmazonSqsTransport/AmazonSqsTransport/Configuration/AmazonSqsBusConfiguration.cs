namespace ViciOne.ServiceBus.AmazonSqsTransport.Configuration;

using ViciOne.ServiceBus.Configuration;
using Observables;


public class AmazonSqsBusConfiguration :
    AmazonSqsEndpointConfiguration,
    IAmazonSqsBusConfiguration
{
    readonly BusObservable _busObservers;

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

    public IAmazonSqsEndpointConfiguration BusEndpointConfiguration { get; }
    public IAmazonSqsHostConfiguration HostConfiguration { get; }
    public MessageRouteTable MessageRoutes { get; }

    public ConnectHandle ConnectBusObserver(IBusObserver observer)
    {
        return _busObservers.Connect(observer);
    }

    public ConnectHandle ConnectEndpointConfigurationObserver(IEndpointConfigurationObserver observer)
    {
        return HostConfiguration.ConnectEndpointConfigurationObserver(observer);
    }
}
