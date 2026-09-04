using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.AmazonSqs.Configuration;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Provides an amazon sqs connection context implementation.
/// </summary>
public class AmazonSqsConnectionContext :
    BasePipeContext,
    ConnectionContext,
    IAsyncDisposable
{
    readonly IAmazonSqsHostConfiguration _hostConfiguration;

    readonly QueueCache _queueCache;
    readonly TopicCache _topicCache;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
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

    /// <summary>
    /// Gets the connection value.
    /// </summary>
    public IConnection Connection { get; }
    /// <summary>
    /// Gets the topology value.
    /// </summary>
    public IAmazonSqsBusTopology Topology { get; }

    /// <summary>
    /// Gets the host address value.
    /// </summary>
    public Uri HostAddress => _hostConfiguration.HostAddress;

    /// <summary>
    /// Gets queue.
    /// </summary>
    /// <param name="queue">The queue value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<QueueInfo> GetQueueAsync(Queue queue, CancellationToken cancellationToken)
    {
        return _queueCache.GetAsync(queue, cancellationToken);
    }

    /// <summary>
    /// Gets queue by name.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<QueueInfo> GetQueueByNameAsync(string name, CancellationToken cancellationToken)
    {
        return _queueCache.GetByNameAsync(name, cancellationToken);
    }

    /// <summary>
    /// Performs the remove queue by name operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<bool> RemoveQueueByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return _queueCache.RemoveByNameAsync(name, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Gets topic.
    /// </summary>
    /// <param name="topic">The topic value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<TopicInfo> GetTopicAsync(Topic topic, CancellationToken cancellationToken)
    {
        return _topicCache.GetAsync(topic, cancellationToken);
    }

    /// <summary>
    /// Gets topic by name.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<TopicInfo> GetTopicByNameAsync(string name, CancellationToken cancellationToken)
    {
        return _topicCache.GetByNameAsync(name, cancellationToken);
    }

    /// <summary>
    /// Performs the remove topic by name operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<bool> RemoveTopicByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return _topicCache.RemoveByNameAsync(name, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Creates client context.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public ClientContext CreateClientContext(CancellationToken cancellationToken)
    {
        return new AmazonSqsClientContext(this, Connection.SqsClient, Connection.SnsClient, cancellationToken);
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public async ValueTask DisposeAsync()
    {
        await _queueCache.DisposeAsync().ConfigureAwait(false);

        await _topicCache.DisposeAsync().ConfigureAwait(false);

        Connection?.Dispose();
    }
}
