using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.ActiveMqTransport.Configuration;

public interface IActiveMqBusConfiguration :
    IBusConfiguration
{
    new IActiveMqHostConfiguration HostConfiguration { get; }

    new IActiveMqEndpointConfiguration BusEndpointConfiguration { get; }

    new IActiveMqTopologyConfiguration Topology { get; }

    /// <summary>
    /// Create an endpoint configuration on the bus, which can later be turned into a receive endpoint
    /// </summary>
    /// <returns></returns>
    IActiveMqEndpointConfiguration CreateEndpointConfiguration(bool isBusEndpoint = false);
}
