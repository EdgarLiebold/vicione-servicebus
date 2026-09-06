using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>Exposes state for in memory receive endpoint operations.</summary>
public interface InMemoryReceiveEndpointContext :
    ReceiveEndpointContext
{
    /// <summary>Gets the send.</summary>
    ISendTopology Send { get; }

    /// <summary>Gets the message fabric.</summary>
    IMessageFabric<InMemoryTransportContext, InMemoryTransportMessage> MessageFabric { get; }

    /// <summary>Gets the transport context.</summary>
    InMemoryTransportContext TransportContext { get; }

    /// <summary>Configures topology.</summary>
    void ConfigureTopology();
}
