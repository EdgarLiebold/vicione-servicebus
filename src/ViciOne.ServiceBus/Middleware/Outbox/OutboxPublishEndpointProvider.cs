using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware.Outbox;

/// <summary>
/// Provides an outbox publish endpoint provider implementation.
/// </summary>
public class OutboxPublishEndpointProvider :
    IPublishEndpointProvider
{
    readonly OutboxSendContext _context;
    readonly IPublishEndpointProvider _publishEndpointProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="publishEndpointProvider">The publish endpoint provider value.</param>
    public OutboxPublishEndpointProvider(OutboxSendContext context, IPublishEndpointProvider publishEndpointProvider)
    {
        _context = context;
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

        return new OutboxSendEndpoint(_context, endpoint);
    }
}
