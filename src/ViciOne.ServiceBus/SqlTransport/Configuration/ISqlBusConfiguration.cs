namespace ViciOne.ServiceBus.SqlTransport.Configuration
{
    using ViciOne.ServiceBus.Configuration;


    public interface ISqlBusConfiguration :
        IBusConfiguration
    {
        new ISqlHostConfiguration HostConfiguration { get; }

        new ISqlEndpointConfiguration BusEndpointConfiguration { get; }

        new ISqlTopologyConfiguration Topology { get; }

        ISqlEndpointConfiguration CreateEndpointConfiguration(bool isBusEndpoint = false);
    }
}
