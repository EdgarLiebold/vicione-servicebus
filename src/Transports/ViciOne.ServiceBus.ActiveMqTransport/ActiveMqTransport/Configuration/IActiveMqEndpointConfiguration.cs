using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.ActiveMqTransport.Configuration;

public interface IActiveMqEndpointConfiguration :
    IEndpointConfiguration
{
    new IActiveMqTopologyConfiguration Topology { get; }
}
