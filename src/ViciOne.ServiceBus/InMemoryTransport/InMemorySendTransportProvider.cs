using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.InMemoryTransport;

public class InMemorySendTransportProvider :
    ISendTransportProvider
{
    readonly ReceiveEndpointContext _context;
    readonly IInMemoryTransportProvider _transportProvider;

    public InMemorySendTransportProvider(IInMemoryTransportProvider transportProvider, ReceiveEndpointContext context)
    {
        _transportProvider = transportProvider;
        _context = context;
    }

    public Uri NormalizeAddress(Uri address)
    {
        return _transportProvider.NormalizeAddress(address);
    }

    public Task<ISendTransport> GetSendTransportAsync(Uri address, CancellationToken cancellationToken = default)
    {
        return _transportProvider.CreateSendTransportAsync(_context, address, cancellationToken: cancellationToken);
    }
}
