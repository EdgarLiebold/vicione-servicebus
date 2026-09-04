using System;
using System.Threading;
using System.Threading.Tasks;
using Apache.NMS;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Defines the contract for connection context.
/// </summary>
public interface ConnectionContext :
    PipeContext
{
    /// <summary>
    /// The ActiveMQ Connection
    /// </summary>
    IConnection Connection { get; }

    /// <summary>
    /// The connection description, useful to debug output
    /// </summary>
    string Description { get; }

    /// <summary>
    /// The Host Address for this connection
    /// </summary>
    Uri HostAddress { get; }

    /// <summary>
    /// Gets the topology value.
    /// </summary>
    IActiveMqBusTopology Topology { get; }

    /// <summary>
    /// Create a model on the connection
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ISession> CreateSessionAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Determines whether virtual topic consumer.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool IsVirtualTopicConsumer(string name);

    /// <summary>
    /// Gets temporary queue.
    /// </summary>
    /// <param name="session">The session value.</param>
    /// <param name="topicName">The topic name value.</param>
    /// <returns>The result of the operation.</returns>
    IQueue GetTemporaryQueue(ISession session, string topicName);

    /// <summary>
    /// Gets temporary topic.
    /// </summary>
    /// <param name="session">The session value.</param>
    /// <param name="topicName">The topic name value.</param>
    /// <returns>The result of the operation.</returns>
    ITopic GetTemporaryTopic(ISession session, string topicName);

    /// <summary>
    /// Attempts to get temporary entity.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="destination">The destination value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetTemporaryEntity(string name, out IDestination? destination);

    /// <summary>
    /// Performs the try remove temporary entity operation.
    /// </summary>
    /// <param name="session">The session value.</param>
    /// <param name="name">The name value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryRemoveTemporaryEntity(ISession session, string name);
}
