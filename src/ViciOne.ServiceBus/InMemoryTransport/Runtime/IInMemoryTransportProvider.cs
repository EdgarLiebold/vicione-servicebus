using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport.Runtime;

/// <summary>Owns one in-memory message fabric and creates transports within its host boundary.</summary>
internal interface IInMemoryTransportProvider :
    IInMemoryTransportContext,
    IAgent,
    IProbeSite
{
    /// <summary>Gets the message fabric shared by the provider's endpoints.</summary>
    IMessageFabric<IInMemoryTransportContext, InMemoryTransportMessage> MessageFabric { get; }

    /// <summary>Creates a send transport for an address within this provider's host boundary.</summary>
    /// <param name="context">The endpoint context that supplies serialization and observers.</param>
    /// <param name="address">The destination address.</param>
    /// <param name="cancellationToken">The token that cancels transport acquisition.</param>
    /// <returns>A task that produces the send transport.</returns>
    Task<ISendTransport> CreateSendTransportAsync(ReceiveEndpointContext context, Uri address, CancellationToken cancellationToken = default);

    /// <summary>Applies publish topology and creates a transport for a message contract.</summary>
    /// <typeparam name="T">The published message contract.</typeparam>
    /// <param name="context">The endpoint context that supplies serialization and observers.</param>
    /// <param name="publishAddress">The resolved publish address.</param>
    /// <param name="cancellationToken">The token that cancels transport acquisition.</param>
    /// <returns>A task that produces the publish transport.</returns>
    Task<ISendTransport> CreatePublishTransportAsync<T>(ReceiveEndpointContext context, Uri publishAddress, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Resolves and validates a destination address against this provider's host.</summary>
    /// <param name="address">The destination address.</param>
    /// <returns>The canonical absolute loopback URI.</returns>
    Uri NormalizeAddress(Uri address);
}
