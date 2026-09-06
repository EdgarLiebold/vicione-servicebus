using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.AmazonSqs.Topology;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Exposes Amazon connection state, topology, and cached entity resolution.</summary>
public interface ConnectionContext :
    PipeContext
{
    /// <summary>Gets the connection that owns the Amazon SQS and Amazon SNS clients.</summary>
    IConnection Connection { get; }

    /// <summary>Gets the Amazon SQS host address for this connection.</summary>
    Uri HostAddress { get; }

    /// <summary>Gets the Amazon SQS bus topology.</summary>
    IAmazonSqsBusTopology Topology { get; }

    /// <summary>Gets or creates entity information for a queue topology entity.</summary>
    /// <param name="queue">The queue topology entity.</param>
    /// <param name="cancellationToken">The token used to cancel queue resolution.</param>
    /// <returns>The resolved queue information.</returns>
    Task<QueueInfo> GetQueueAsync(Queue queue, CancellationToken cancellationToken);
    /// <summary>Gets or creates entity information for a logical queue name.</summary>
    /// <param name="name">The logical queue name.</param>
    /// <param name="cancellationToken">The token used to cancel queue resolution.</param>
    /// <returns>The resolved queue information.</returns>
    Task<QueueInfo> GetQueueByNameAsync(string name, CancellationToken cancellationToken);
    /// <summary>Removes and disposes cached queue information by logical name.</summary>
    /// <param name="name">The logical queue name.</param>
    /// <param name="cancellationToken">The token used to cancel waiting for removal.</param>
    /// <returns><see langword="true"/> when a cached queue was removed; otherwise, <see langword="false"/>.</returns>
    Task<bool> RemoveQueueByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Gets or creates entity information for a topic topology entity.</summary>
    /// <param name="topic">The topic topology entity.</param>
    /// <param name="cancellationToken">The token used to cancel topic resolution.</param>
    /// <returns>The resolved topic information.</returns>
    Task<TopicInfo> GetTopicAsync(Topic topic, CancellationToken cancellationToken);
    /// <summary>Gets or creates entity information for a logical topic name.</summary>
    /// <param name="name">The logical topic name.</param>
    /// <param name="cancellationToken">The token used to cancel topic resolution.</param>
    /// <returns>The resolved topic information.</returns>
    Task<TopicInfo> GetTopicByNameAsync(string name, CancellationToken cancellationToken);
    /// <summary>Removes and disposes cached topic information by logical name.</summary>
    /// <param name="name">The logical topic name.</param>
    /// <param name="cancellationToken">The token used to cancel waiting for removal.</param>
    /// <returns><see langword="true"/> when a cached topic was removed; otherwise, <see langword="false"/>.</returns>
    Task<bool> RemoveTopicByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Creates an operation-scoped client context over this connection.</summary>
    /// <param name="cancellationToken">The token associated with the client context.</param>
    /// <returns>The new client context.</returns>
    ClientContext CreateClientContext(CancellationToken cancellationToken);
}
