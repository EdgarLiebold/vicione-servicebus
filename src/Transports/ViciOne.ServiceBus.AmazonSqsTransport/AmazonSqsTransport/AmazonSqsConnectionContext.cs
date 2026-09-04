using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.AmazonSqsTransport.Configuration;
using ViciOne.ServiceBus.AmazonSqsTransport.Topology;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.AmazonSqsTransport;

public class AmazonSqsConnectionContext :
    BasePipeContext,
    ConnectionContext,
    IAsyncDisposable
{
    readonly IAmazonSqsHostConfiguration _hostConfiguration;

    readonly QueueCache _queueCache;
    readonly TopicCache _topicCache;

    public AmazonSqsConnectionContext(IConnection connection, IAmazonSqsHostConfiguration hostConfiguration, CancellationToken cancellationToken)
        : base(cancellationToken)
    {
        _hostConfiguration = hostConfiguration;
        Connection = connection;

        Topology = hostConfiguration.Topology;

        AmazonSqsClientContextCacheOptions cacheOptions = hostConfiguration.Settings.ClientContextCacheOptions;
        _queueCache = new QueueCache(Connection.SqsClient, cacheOptions, cancellationToken);
        _topicCache = new TopicCache(Connection.SnsClient, cacheOptions, cancellationToken);
    }

    public IConnection Connection { get; }
    public IAmazonSqsBusTopology Topology { get; }

    public Uri HostAddress => _hostConfiguration.HostAddress;

    public Task<QueueInfo> GetQueueAsync(Queue queue, CancellationToken cancellationToken)
    {
        return _queueCache.GetAsync(queue, cancellationToken);
    }

    public Task<QueueInfo> GetQueueByNameAsync(string name, CancellationToken cancellationToken)
    {
        return _queueCache.GetByNameAsync(name, cancellationToken);
    }

    public Task<bool> RemoveQueueByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return _queueCache.RemoveByNameAsync(name, cancellationToken: cancellationToken);
    }

    public Task<TopicInfo> GetTopicAsync(Topic topic, CancellationToken cancellationToken)
    {
        return _topicCache.GetAsync(topic, cancellationToken);
    }

    public Task<TopicInfo> GetTopicByNameAsync(string name, CancellationToken cancellationToken)
    {
        return _topicCache.GetByNameAsync(name, cancellationToken);
    }

    public Task<bool> RemoveTopicByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return _topicCache.RemoveByNameAsync(name, cancellationToken: cancellationToken);
    }

    public ClientContext CreateClientContext(CancellationToken cancellationToken)
    {
        return new AmazonSqsClientContext(this, Connection.SqsClient, Connection.SnsClient, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _queueCache.DisposeAsync().ConfigureAwait(false);

        await _topicCache.DisposeAsync().ConfigureAwait(false);

        Connection?.Dispose();
    }
}
