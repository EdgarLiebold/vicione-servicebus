using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>
/// Defines the contract for in memory transport provider.
/// </summary>
public interface IInMemoryTransportProvider :
    InMemoryTransportContext,
    IAgent,
    IProbeSite
{
    /// <summary>
    /// Gets the message fabric value.
    /// </summary>
    IMessageFabric<InMemoryTransportContext, InMemoryTransportMessage> MessageFabric { get; }

    /// <summary>
    /// Creates send transport.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="address">The address value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<ISendTransport> CreateSendTransportAsync(ReceiveEndpointContext context, Uri address, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates publish transport.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="publishAddress">The publish address value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<ISendTransport> CreatePublishTransportAsync<T>(ReceiveEndpointContext context, Uri publishAddress, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Performs the normalize address operation.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <returns>The result of the operation.</returns>
    Uri NormalizeAddress(Uri address);
}
