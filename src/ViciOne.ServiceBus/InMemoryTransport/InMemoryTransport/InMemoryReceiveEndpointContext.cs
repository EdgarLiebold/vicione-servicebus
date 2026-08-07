// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.InMemoryTransport
{
    using Transports;
    using Transports.Fabric;


    public interface InMemoryReceiveEndpointContext :
        ReceiveEndpointContext
    {
        ISendTopology Send { get; }

        IMessageFabric<InMemoryTransportContext, InMemoryTransportMessage> MessageFabric { get; }

        InMemoryTransportContext TransportContext { get; }

        void ConfigureTopology();
    }
}
