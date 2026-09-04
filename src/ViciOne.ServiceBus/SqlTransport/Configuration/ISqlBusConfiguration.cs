using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

public interface ISqlBusConfiguration :
    IBusConfiguration
{
    new ISqlHostConfiguration HostConfiguration { get; }

    new ISqlEndpointConfiguration BusEndpointConfiguration { get; }

    new ISqlTopologyConfiguration Topology { get; }

    ISqlEndpointConfiguration CreateEndpointConfiguration(bool isBusEndpoint = false);
}
