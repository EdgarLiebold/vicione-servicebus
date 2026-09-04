using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware.Outbox;

public class OutboxPublishEndpointProvider :
    IPublishEndpointProvider
{
    readonly OutboxSendContext _context;
    readonly IPublishEndpointProvider _publishEndpointProvider;

    public OutboxPublishEndpointProvider(OutboxSendContext context, IPublishEndpointProvider publishEndpointProvider)
    {
        _context = context;
        _publishEndpointProvider = publishEndpointProvider;
    }

    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        return _publishEndpointProvider.ConnectPublishObserver(observer);
    }

    public async Task<ISendEndpoint> GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        var endpoint = await _publishEndpointProvider.GetPublishSendEndpointAsync<T>(cancellationToken: cancellationToken).ConfigureAwait(false);

        return new OutboxSendEndpoint(_context, endpoint);
    }
}
