using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>
/// Defines the contract for in memory receive endpoint context.
/// </summary>
public interface InMemoryReceiveEndpointContext :
    ReceiveEndpointContext
{
    /// <summary>
    /// Gets the send value.
    /// </summary>
    ISendTopology Send { get; }

    /// <summary>
    /// Gets the message fabric value.
    /// </summary>
    IMessageFabric<InMemoryTransportContext, InMemoryTransportMessage> MessageFabric { get; }

    /// <summary>
    /// Gets the transport context value.
    /// </summary>
    InMemoryTransportContext TransportContext { get; }

    /// <summary>
    /// Configures topology.
    /// </summary>
    void ConfigureTopology();
}
