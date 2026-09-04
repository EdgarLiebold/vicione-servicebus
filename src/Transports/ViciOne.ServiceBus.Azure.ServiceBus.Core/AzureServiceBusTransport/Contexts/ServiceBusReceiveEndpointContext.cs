using ViciOne.ServiceBus.AzureServiceBusTransport.Topology;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBusTransport;

public interface ServiceBusReceiveEndpointContext :
    ReceiveEndpointContext
{
    BrokerTopology BrokerTopology { get; }

    IClientContextSupervisor ClientContextSupervisor { get; }
}
