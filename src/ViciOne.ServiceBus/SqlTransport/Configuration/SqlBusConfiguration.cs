namespace ViciOne.ServiceBus.SqlTransport.Configuration
{
    using ViciOne.ServiceBus.Configuration;
    using Observables;


    public class SqlBusConfiguration :
        SqlEndpointConfiguration,
        ISqlBusConfiguration
    {
        readonly BusObservable _busObservers;

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

        public ISqlEndpointConfiguration BusEndpointConfiguration { get; }
        public ISqlHostConfiguration HostConfiguration { get; }
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
