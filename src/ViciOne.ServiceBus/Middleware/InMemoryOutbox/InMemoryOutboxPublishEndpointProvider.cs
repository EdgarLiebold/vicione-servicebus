using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware.InMemoryOutbox;

/// <summary>Provides in memory outbox publish endpoint services.</summary>
public class InMemoryOutboxPublishEndpointProvider :
    IPublishEndpointProvider
{
    readonly OutboxContext _outboxContext;
    readonly IPublishEndpointProvider _publishEndpointProvider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="outboxContext">The outbox context.</param>
    /// <param name="publishEndpointProvider">The publish endpoint provider.</param>
    public InMemoryOutboxPublishEndpointProvider(OutboxContext outboxContext, IPublishEndpointProvider publishEndpointProvider)
    {
        _outboxContext = outboxContext;
        _publishEndpointProvider = publishEndpointProvider;
    }

    /// <summary>Connects publish observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        return _publishEndpointProvider.ConnectPublishObserver(observer);
    }

    /// <summary>Gets publish send endpoint.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public async Task<ISendEndpoint> GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        var endpoint = await _publishEndpointProvider.GetPublishSendEndpointAsync<T>(cancellationToken: cancellationToken).ConfigureAwait(false);

        return new OutboxSendEndpoint(_outboxContext, endpoint);
    }
}
