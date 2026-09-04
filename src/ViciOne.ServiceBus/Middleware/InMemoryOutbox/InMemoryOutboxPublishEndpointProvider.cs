using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware.InMemoryOutbox;

/// <summary>
/// Provides an in memory outbox publish endpoint provider implementation.
/// </summary>
public class InMemoryOutboxPublishEndpointProvider :
    IPublishEndpointProvider
{
    readonly OutboxContext _outboxContext;
    readonly IPublishEndpointProvider _publishEndpointProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="outboxContext">The outbox context value.</param>
    /// <param name="publishEndpointProvider">The publish endpoint provider value.</param>
    public InMemoryOutboxPublishEndpointProvider(OutboxContext outboxContext, IPublishEndpointProvider publishEndpointProvider)
    {
        _outboxContext = outboxContext;
        _publishEndpointProvider = publishEndpointProvider;
    }

    /// <summary>
    /// Connects publish observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        return _publishEndpointProvider.ConnectPublishObserver(observer);
    }

    /// <summary>
    /// Gets publish send endpoint.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<ISendEndpoint> GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        var endpoint = await _publishEndpointProvider.GetPublishSendEndpointAsync<T>(cancellationToken: cancellationToken).ConfigureAwait(false);

        return new OutboxSendEndpoint(_outboxContext, endpoint);
    }
}
