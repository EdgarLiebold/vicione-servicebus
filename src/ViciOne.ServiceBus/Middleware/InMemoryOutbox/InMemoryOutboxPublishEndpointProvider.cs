using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware.InMemoryOutbox;

public class InMemoryOutboxPublishEndpointProvider :
    IPublishEndpointProvider
{
    readonly OutboxContext _outboxContext;
    readonly IPublishEndpointProvider _publishEndpointProvider;

    public InMemoryOutboxPublishEndpointProvider(OutboxContext outboxContext, IPublishEndpointProvider publishEndpointProvider)
    {
        _outboxContext = outboxContext;
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

        return new OutboxSendEndpoint(_outboxContext, endpoint);
    }
}
