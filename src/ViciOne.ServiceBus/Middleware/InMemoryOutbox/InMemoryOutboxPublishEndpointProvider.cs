using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware.InMemoryOutbox;

/// <summary>Wraps publish endpoints so operations are deferred by an in-memory outbox.</summary>
internal sealed class InMemoryOutboxPublishEndpointProvider :
    IPublishEndpointProvider
{
    readonly OutboxContext _outboxContext;
    readonly IPublishEndpointProvider _publishEndpointProvider;

    /// <summary>Initializes the provider over an outbox and a transport publish provider.</summary>
    /// <param name="outboxContext">The outbox that defers outgoing operations.</param>
    /// <param name="publishEndpointProvider">The provider that resolves transport publish endpoints.</param>
    public InMemoryOutboxPublishEndpointProvider(OutboxContext outboxContext, IPublishEndpointProvider publishEndpointProvider)
    {
        _outboxContext = outboxContext ?? throw new ArgumentNullException(nameof(outboxContext));
        _publishEndpointProvider = publishEndpointProvider ?? throw new ArgumentNullException(nameof(publishEndpointProvider));
    }

    /// <summary>Registers an observer with the underlying transport publish provider.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _publishEndpointProvider.ConnectPublishObserver(observer);
    }

    /// <summary>Resolves and decorates the transport endpoint for a published message contract.</summary>
    /// <typeparam name="T">The published message contract.</typeparam>
    /// <param name="cancellationToken">The token that cancels endpoint resolution.</param>
    /// <returns>A task containing an endpoint whose operations are deferred by the outbox.</returns>
    public async Task<ISendEndpoint> GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        var endpoint = await _publishEndpointProvider.GetPublishSendEndpointAsync<T>(cancellationToken: cancellationToken).ConfigureAwait(false);

        return new OutboxSendEndpoint(_outboxContext, endpoint);
    }
}
