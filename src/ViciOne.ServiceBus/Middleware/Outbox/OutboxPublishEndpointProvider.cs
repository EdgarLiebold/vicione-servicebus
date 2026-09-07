using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware.Outbox;

/// <summary>Wraps transport publish endpoints so published messages are captured by a durable outbox.</summary>
internal sealed class OutboxPublishEndpointProvider :
    IPublishEndpointProvider
{
    readonly OutboxSendContext _context;
    readonly IPublishEndpointProvider _publishEndpointProvider;

    /// <summary>Initializes the provider over an outbox capture context and a transport provider.</summary>
    /// <param name="context">The outbox context that captures outgoing messages.</param>
    /// <param name="publishEndpointProvider">The transport provider that resolves publish endpoints.</param>
    public OutboxPublishEndpointProvider(OutboxSendContext context, IPublishEndpointProvider publishEndpointProvider)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
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
    /// <returns>A task containing an endpoint that captures the publish in the outbox.</returns>
    public async Task<ISendEndpoint> GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        var endpoint = await _publishEndpointProvider.GetPublishSendEndpointAsync<T>(cancellationToken: cancellationToken).ConfigureAwait(false);

        return new OutboxSendEndpoint(_context, endpoint);
    }
}
