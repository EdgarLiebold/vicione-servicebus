namespace ViciOne.ServiceBus.RabbitMqTransport.Configuration
{
    using ViciOne.ServiceBus.Configuration;
    using Observables;


    public class RabbitMqBusConfiguration :
        RabbitMqEndpointConfiguration,
        IRabbitMqBusConfiguration
    {
        readonly BusObservable _busObservers;

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

        public IRabbitMqEndpointConfiguration BusEndpointConfiguration { get; }
        public IRabbitMqHostConfiguration HostConfiguration { get; }
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
}
