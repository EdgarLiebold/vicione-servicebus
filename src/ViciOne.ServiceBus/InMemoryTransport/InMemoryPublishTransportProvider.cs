using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>Provides in memory publish transport services.</summary>
public class InMemoryPublishTransportProvider :
    IPublishTransportProvider
{
    readonly ReceiveEndpointContext _context;
    readonly IInMemoryTransportProvider _transportProvider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="transportProvider">The transport provider.</param>
    /// <param name="context">The context associated with the operation.</param>
    public InMemoryPublishTransportProvider(IInMemoryTransportProvider transportProvider, ReceiveEndpointContext context)
    {
        _transportProvider = transportProvider;
        _context = context;
    }

    /// <summary>Gets publish transport.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="publishAddress">The publish address.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public Task<ISendTransport> GetPublishTransportAsync<T>(Uri? publishAddress, CancellationToken cancellationToken = default)
        where T : class
    {
        return _transportProvider.CreatePublishTransportAsync<T>(_context, publishAddress!, cancellationToken: cancellationToken);
    }
}
