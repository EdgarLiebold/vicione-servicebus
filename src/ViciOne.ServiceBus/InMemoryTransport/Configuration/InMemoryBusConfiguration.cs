using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

public class InMemoryBusConfiguration :
    InMemoryEndpointConfiguration,
    IInMemoryBusConfiguration
{
    readonly BusObservable _busObservers;

    public InMemoryBusConfiguration(IInMemoryTopologyConfiguration topologyConfiguration, Uri baseAddress)
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

    public IInMemoryEndpointConfiguration BusEndpointConfiguration { get; }
    public IInMemoryHostConfiguration HostConfiguration { get; }
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
