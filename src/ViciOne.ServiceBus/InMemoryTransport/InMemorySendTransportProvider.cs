using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>
/// Provides an in memory send transport provider implementation.
/// </summary>
public class InMemorySendTransportProvider :
    ISendTransportProvider
{
    readonly ReceiveEndpointContext _context;
    readonly IInMemoryTransportProvider _transportProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="transportProvider">The transport provider value.</param>
    /// <param name="context">The operation context.</param>
    public InMemorySendTransportProvider(IInMemoryTransportProvider transportProvider, ReceiveEndpointContext context)
    {
        _transportProvider = transportProvider;
        _context = context;
    }

    /// <summary>
    /// Performs the normalize address operation.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <returns>The result of the operation.</returns>
    public Uri NormalizeAddress(Uri address)
    {
        return _transportProvider.NormalizeAddress(address);
    }

    /// <summary>
    /// Gets send transport.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<ISendTransport> GetSendTransportAsync(Uri address, CancellationToken cancellationToken = default)
    {
        return _transportProvider.CreateSendTransportAsync(_context, address, cancellationToken: cancellationToken);
    }
}
