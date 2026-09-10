using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.InMemoryTransport.Runtime;

/// <summary>Resolves point-to-point send transports through an in-memory transport provider.</summary>
internal sealed class InMemorySendTransportProvider :
    ISendTransportProvider
{
    readonly ReceiveEndpointContext _context;
    readonly IInMemoryTransportProvider _transportProvider;

    /// <summary>Creates a send-transport resolver for a receive endpoint.</summary>
    /// <param name="transportProvider">The provider that owns the message fabric.</param>
    /// <param name="context">The endpoint context used to construct send transports.</param>
    public InMemorySendTransportProvider(IInMemoryTransportProvider transportProvider, ReceiveEndpointContext context)
    {
        _transportProvider = transportProvider ?? throw new ArgumentNullException(nameof(transportProvider));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>Resolves and validates a destination address against the provider's host.</summary>
    /// <param name="address">The destination address.</param>
    /// <returns>The canonical absolute loopback URI.</returns>
    public Uri NormalizeAddress(Uri address)
    {
        return _transportProvider.NormalizeAddress(address);
    }

    /// <summary>Gets a send transport for a destination address.</summary>
    /// <param name="address">The destination address.</param>
    /// <param name="cancellationToken">The token that cancels transport acquisition.</param>
    /// <returns>A task that produces the send transport.</returns>
    public Task<ISendTransport> GetSendTransportAsync(Uri address, CancellationToken cancellationToken = default)
    {
        return _transportProvider.CreateSendTransportAsync(_context, address, cancellationToken: cancellationToken);
    }
}
