using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.InMemoryTransport.Runtime;

/// <summary>Resolves publish transports through an in-memory transport provider.</summary>
internal sealed class InMemoryPublishTransportProvider :
    IPublishTransportProvider
{
    readonly ReceiveEndpointContext _context;
    readonly IInMemoryTransportProvider _transportProvider;

    /// <summary>Creates a publish-transport resolver for a receive endpoint.</summary>
    /// <param name="transportProvider">The provider that owns the message fabric.</param>
    /// <param name="context">The endpoint context used to construct send transports.</param>
    public InMemoryPublishTransportProvider(IInMemoryTransportProvider transportProvider, ReceiveEndpointContext context)
    {
        _transportProvider = transportProvider ?? throw new ArgumentNullException(nameof(transportProvider));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>Gets the publish transport for a message contract and its resolved address.</summary>
    /// <typeparam name="T">The published message contract.</typeparam>
    /// <param name="publishAddress">The publish address resolved by topology.</param>
    /// <param name="cancellationToken">The token that cancels transport acquisition.</param>
    /// <returns>A task that produces the publish transport.</returns>
    public Task<ISendTransport> GetPublishTransportAsync<T>(Uri? publishAddress, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(publishAddress);
        return _transportProvider.CreatePublishTransportAsync<T>(_context, publishAddress, cancellationToken);
    }
}
