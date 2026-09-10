using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport.Runtime;

/// <summary>Exposes the transport services and topology owned by an in-memory receive endpoint.</summary>
internal interface IInMemoryReceiveEndpointContext :
    ReceiveEndpointContext
{
    /// <summary>Gets the endpoint's send topology.</summary>
    ISendTopology Send { get; }

    /// <summary>Gets the host's shared message fabric.</summary>
    IMessageFabric<InMemoryTransportMessage> MessageFabric { get; }

    /// <summary>Gets the host-specific context that isolates fabric entities.</summary>
    IInMemoryTransportContext TransportContext { get; }

    /// <summary>Declares the endpoint queue, exchange, and configured consume bindings.</summary>
    void ConfigureTopology();
}
