using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.AmazonSqs.Topology;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Defines the contract for connection context.
/// </summary>
public interface ConnectionContext :
    PipeContext
{
    /// <summary>
    /// The Amazon Connection
    /// </summary>
    IConnection Connection { get; }

    /// <summary>
    /// The Host Address for this connection
    /// </summary>
    Uri HostAddress { get; }

    /// <summary>
    /// Gets the topology value.
    /// </summary>
    IAmazonSqsBusTopology Topology { get; }

    /// <summary>
    /// Gets queue.
    /// </summary>
    /// <param name="queue">The queue value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<QueueInfo> GetQueueAsync(Queue queue, CancellationToken cancellationToken);
    /// <summary>
    /// Gets queue by name.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<QueueInfo> GetQueueByNameAsync(string name, CancellationToken cancellationToken);
    /// <summary>
    /// Performs the remove queue by name operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<bool> RemoveQueueByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets topic.
    /// </summary>
    /// <param name="topic">The topic value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<TopicInfo> GetTopicAsync(Topic topic, CancellationToken cancellationToken);
    /// <summary>
    /// Gets topic by name.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<TopicInfo> GetTopicByNameAsync(string name, CancellationToken cancellationToken);
    /// <summary>
    /// Performs the remove topic by name operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<bool> RemoveTopicByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates client context.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    ClientContext CreateClientContext(CancellationToken cancellationToken);
}
