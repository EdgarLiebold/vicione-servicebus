using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport;

public interface InMemoryReceiveEndpointContext :
    ReceiveEndpointContext
{
    ISendTopology Send { get; }

    IMessageFabric<InMemoryTransportContext, InMemoryTransportMessage> MessageFabric { get; }

    InMemoryTransportContext TransportContext { get; }

    void ConfigureTopology();
}
