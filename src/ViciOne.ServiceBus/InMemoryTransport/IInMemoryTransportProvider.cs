using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>Provides in memory transport services.</summary>
public interface IInMemoryTransportProvider :
    InMemoryTransportContext,
    IAgent,
    IProbeSite
{
    /// <summary>Gets the message fabric.</summary>
    IMessageFabric<InMemoryTransportContext, InMemoryTransportMessage> MessageFabric { get; }

    /// <summary>Creates send transport.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="address">The address.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the created value.</returns>
    Task<ISendTransport> CreateSendTransportAsync(ReceiveEndpointContext context, Uri address, CancellationToken cancellationToken = default);

    /// <summary>Creates publish transport.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="publishAddress">The publish address.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the created value.</returns>
    Task<ISendTransport> CreatePublishTransportAsync<T>(ReceiveEndpointContext context, Uri publishAddress, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Normalizes address.</summary>
    /// <param name="address">The address.</param>
    /// <returns>The uri produced by the operation.</returns>
    Uri NormalizeAddress(Uri address);
}
