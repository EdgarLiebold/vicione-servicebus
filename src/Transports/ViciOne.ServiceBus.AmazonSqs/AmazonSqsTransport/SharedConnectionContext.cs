using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Provides a shared connection context implementation.
/// </summary>
public class SharedConnectionContext :
    ProxyPipeContext,
    ConnectionContext
{
    readonly ConnectionContext _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public SharedConnectionContext(ConnectionContext context, CancellationToken cancellationToken)
        : base(context)
    {
        _context = context;

        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Gets the cancellation token value.
    /// </summary>
    public override CancellationToken CancellationToken { get; }

    /// <summary>
    /// Gets the connection value.
    /// </summary>
    public IConnection Connection => _context.Connection;
    /// <summary>
    /// Gets the host address value.
    /// </summary>
    public Uri HostAddress => _context.HostAddress;
    /// <summary>
    /// Gets the topology value.
    /// </summary>
    public IAmazonSqsBusTopology Topology => _context.Topology;

    /// <summary>
    /// Gets queue.
    /// </summary>
    /// <param name="queue">The queue value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<QueueInfo> GetQueueAsync(Queue queue, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.GetQueueAsync(queue, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets queue by name.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<QueueInfo> GetQueueByNameAsync(string name, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.GetQueueByNameAsync(name, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the remove queue by name operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<bool> RemoveQueueByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return _context.RemoveQueueByNameAsync(name, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Gets topic.
    /// </summary>
    /// <param name="topic">The topic value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<TopicInfo> GetTopicAsync(Topic topic, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.GetTopicAsync(topic, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets topic by name.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<TopicInfo> GetTopicByNameAsync(string name, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.GetTopicByNameAsync(name, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the remove topic by name operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<bool> RemoveTopicByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return _context.RemoveTopicByNameAsync(name, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Creates client context.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public ClientContext CreateClientContext(CancellationToken cancellationToken)
    {
        return _context.CreateClientContext(cancellationToken);
    }
}
