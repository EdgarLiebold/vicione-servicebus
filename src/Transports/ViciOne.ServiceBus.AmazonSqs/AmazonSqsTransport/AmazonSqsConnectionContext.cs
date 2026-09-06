using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.AmazonSqs.Configuration;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Owns AWS clients and cached Amazon SQS queue and Amazon SNS topic contexts.</summary>
public class AmazonSqsConnectionContext :
    BasePipeContext,
    ConnectionContext,
    IAsyncDisposable
{
    readonly IAmazonSqsHostConfiguration _hostConfiguration;

    readonly QueueCache _queueCache;
    readonly TopicCache _topicCache;

    /// <summary>Creates a connection context with bounded queue and topic caches.</summary>
    /// <param name="connection">The AWS client connection owned by the context.</param>
    /// <param name="hostConfiguration">The Amazon SQS host and topology configuration.</param>
    /// <param name="cancellationToken">The token that signals connection-context shutdown.</param>
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

    /// <summary>Gets the AWS client connection.</summary>
    public IConnection Connection { get; }
    /// <summary>Gets the Amazon SQS bus topology.</summary>
    public IAmazonSqsBusTopology Topology { get; }

    /// <summary>Gets the configured transport host address.</summary>
    public Uri HostAddress => _hostConfiguration.HostAddress;

    /// <summary>Gets or creates cached provider state for a queue declaration.</summary>
    /// <param name="queue">The queue declaration.</param>
    /// <param name="cancellationToken">The token used to cancel lookup or creation.</param>
    /// <returns>A task that produces the queue context.</returns>
    public Task<QueueInfo> GetQueueAsync(Queue queue, CancellationToken cancellationToken)
    {
        return _queueCache.GetAsync(queue, cancellationToken);
    }

    /// <summary>Gets or discovers cached provider state for a queue name.</summary>
    /// <param name="name">The Amazon SQS queue name.</param>
    /// <param name="cancellationToken">The token used to cancel lookup or discovery.</param>
    /// <returns>A task that produces the queue context.</returns>
    public Task<QueueInfo> GetQueueByNameAsync(string name, CancellationToken cancellationToken)
    {
        return _queueCache.GetByNameAsync(name, cancellationToken);
    }

    /// <summary>Removes and disposes cached provider state for a queue name.</summary>
    /// <param name="name">The Amazon SQS queue name.</param>
    /// <param name="cancellationToken">The token used to cancel removal.</param>
    /// <returns>A task that reports whether a cached queue context was removed.</returns>
    public Task<bool> RemoveQueueByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return _queueCache.RemoveByNameAsync(name, cancellationToken: cancellationToken);
    }

    /// <summary>Gets or creates cached provider state for a topic declaration.</summary>
    /// <param name="topic">The Amazon SNS topic declaration.</param>
    /// <param name="cancellationToken">The token used to cancel lookup or creation.</param>
    /// <returns>A task that produces the topic context.</returns>
    public Task<TopicInfo> GetTopicAsync(Topic topic, CancellationToken cancellationToken)
    {
        return _topicCache.GetAsync(topic, cancellationToken);
    }

    /// <summary>Gets or discovers cached provider state for a topic name.</summary>
    /// <param name="name">The Amazon SNS topic name.</param>
    /// <param name="cancellationToken">The token used to cancel lookup or discovery.</param>
    /// <returns>A task that produces the topic context.</returns>
    public Task<TopicInfo> GetTopicByNameAsync(string name, CancellationToken cancellationToken)
    {
        return _topicCache.GetByNameAsync(name, cancellationToken);
    }

    /// <summary>Removes and disposes cached provider state for a topic name.</summary>
    /// <param name="name">The Amazon SNS topic name.</param>
    /// <param name="cancellationToken">The token used to cancel removal.</param>
    /// <returns>A task that reports whether a cached topic context was removed.</returns>
    public Task<bool> RemoveTopicByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return _topicCache.RemoveByNameAsync(name, cancellationToken: cancellationToken);
    }

    /// <summary>Creates an operation-scoped context over the connection's AWS clients.</summary>
    /// <param name="cancellationToken">The token associated with the operation scope.</param>
    /// <returns>The scoped client context.</returns>
    public ClientContext CreateClientContext(CancellationToken cancellationToken)
    {
        return new AmazonSqsClientContext(this, Connection.SqsClient, Connection.SnsClient, cancellationToken);
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    /// <returns>A task that completes after queue cache, topic cache, and connection cleanup.</returns>
    public async ValueTask DisposeAsync()
    {
        await _queueCache.DisposeAsync().ConfigureAwait(false);

        await _topicCache.DisposeAsync().ConfigureAwait(false);

        Connection?.Dispose();
    }
}
