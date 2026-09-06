using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>Provides in memory send transport services.</summary>
public class InMemorySendTransportProvider :
    ISendTransportProvider
{
    readonly ReceiveEndpointContext _context;
    readonly IInMemoryTransportProvider _transportProvider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="transportProvider">The transport provider.</param>
    /// <param name="context">The context associated with the operation.</param>
    public InMemorySendTransportProvider(IInMemoryTransportProvider transportProvider, ReceiveEndpointContext context)
    {
        _transportProvider = transportProvider;
        _context = context;
    }

    /// <summary>Normalizes address.</summary>
    /// <param name="address">The address.</param>
    /// <returns>The uri produced by the operation.</returns>
    public Uri NormalizeAddress(Uri address)
    {
        return _transportProvider.NormalizeAddress(address);
    }

    /// <summary>Gets send transport.</summary>
    /// <param name="address">The address.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public Task<ISendTransport> GetSendTransportAsync(Uri address, CancellationToken cancellationToken = default)
    {
        return _transportProvider.CreateSendTransportAsync(_context, address, cancellationToken: cancellationToken);
    }
}
