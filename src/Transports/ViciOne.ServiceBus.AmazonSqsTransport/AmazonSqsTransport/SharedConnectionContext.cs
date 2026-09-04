using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.AmazonSqsTransport.Topology;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.AmazonSqsTransport;

public class SharedConnectionContext :
    ProxyPipeContext,
    ConnectionContext
{
    readonly ConnectionContext _context;

    public SharedConnectionContext(ConnectionContext context, CancellationToken cancellationToken)
        : base(context)
    {
        _context = context;

        CancellationToken = cancellationToken;
    }

    public override CancellationToken CancellationToken { get; }

    public IConnection Connection => _context.Connection;
    public Uri HostAddress => _context.HostAddress;
    public IAmazonSqsBusTopology Topology => _context.Topology;

    public async Task<QueueInfo> GetQueueAsync(Queue queue, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.GetQueueAsync(queue, tokenSource.Token).ConfigureAwait(false);
    }

    public async Task<QueueInfo> GetQueueByNameAsync(string name, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.GetQueueByNameAsync(name, tokenSource.Token).ConfigureAwait(false);
    }

    public Task<bool> RemoveQueueByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return _context.RemoveQueueByNameAsync(name, cancellationToken: cancellationToken);
    }

    public async Task<TopicInfo> GetTopicAsync(Topic topic, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.GetTopicAsync(topic, tokenSource.Token).ConfigureAwait(false);
    }

    public async Task<TopicInfo> GetTopicByNameAsync(string name, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.GetTopicByNameAsync(name, tokenSource.Token).ConfigureAwait(false);
    }

    public Task<bool> RemoveTopicByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return _context.RemoveTopicByNameAsync(name, cancellationToken: cancellationToken);
    }

    public ClientContext CreateClientContext(CancellationToken cancellationToken)
    {
        return _context.CreateClientContext(cancellationToken);
    }
}
