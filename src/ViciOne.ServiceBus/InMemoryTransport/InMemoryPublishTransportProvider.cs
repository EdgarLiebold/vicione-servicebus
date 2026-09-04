using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

#nullable enable
namespace ViciOne.ServiceBus.InMemoryTransport;

public class InMemoryPublishTransportProvider :
    IPublishTransportProvider
{
    readonly ReceiveEndpointContext _context;
    readonly IInMemoryTransportProvider _transportProvider;

    public InMemoryPublishTransportProvider(IInMemoryTransportProvider transportProvider, ReceiveEndpointContext context)
    {
        _transportProvider = transportProvider;
        _context = context;
    }

    public Task<ISendTransport> GetPublishTransportAsync<T>(Uri? publishAddress, CancellationToken cancellationToken = default)
        where T : class
    {
        return _transportProvider.CreatePublishTransportAsync<T>(_context, publishAddress!, cancellationToken: cancellationToken);
    }
}
