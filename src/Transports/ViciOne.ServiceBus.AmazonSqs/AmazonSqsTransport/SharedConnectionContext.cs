using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Links a nested supervisor lifetime to an underlying Amazon connection context.</summary>
public class SharedConnectionContext :
    ProxyPipeContext,
    ConnectionContext
{
    readonly ConnectionContext _context;

    /// <summary>Initializes a shared connection-context proxy.</summary>
    /// <param name="context">The underlying Amazon connection context.</param>
    /// <param name="cancellationToken">The nested supervisor's cancellation token.</param>
    public SharedConnectionContext(ConnectionContext context, CancellationToken cancellationToken)
        : base(context)
    {
        _context = context;

        CancellationToken = cancellationToken;
    }

    /// <summary>Gets the nested supervisor's cancellation token.</summary>
    public override CancellationToken CancellationToken { get; }

    /// <inheritdoc />
    public IConnection Connection => _context.Connection;
    /// <inheritdoc />
    public Uri HostAddress => _context.HostAddress;
    /// <inheritdoc />
    public IAmazonSqsBusTopology Topology => _context.Topology;

    /// <inheritdoc />
    public async Task<QueueInfo> GetQueueAsync(Queue queue, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.GetQueueAsync(queue, tokenSource.Token).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<QueueInfo> GetQueueByNameAsync(string name, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.GetQueueByNameAsync(name, tokenSource.Token).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> RemoveQueueByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.RemoveQueueByNameAsync(name, cancellationToken: tokenSource.Token).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TopicInfo> GetTopicAsync(Topic topic, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.GetTopicAsync(topic, tokenSource.Token).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TopicInfo> GetTopicByNameAsync(string name, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.GetTopicByNameAsync(name, tokenSource.Token).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> RemoveTopicByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.RemoveTopicByNameAsync(name, cancellationToken: tokenSource.Token).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public ClientContext CreateClientContext(CancellationToken cancellationToken)
    {
        ClientContext sharedContext = _context.CreateClientContext(CancellationToken);
        return new ScopeClientContext(sharedContext, cancellationToken);
    }
}
