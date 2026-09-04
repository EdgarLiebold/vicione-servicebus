using System;
using System.Threading;
using System.Threading.Tasks;
using Apache.NMS;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.ActiveMq;

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

    IConnection ConnectionContext.Connection => _context.Connection;
    /// <summary>
    /// Gets the description value.
    /// </summary>
    public string Description => _context.Description;
    /// <summary>
    /// Gets the host address value.
    /// </summary>
    public Uri HostAddress => _context.HostAddress;
    /// <summary>
    /// Gets the topology value.
    /// </summary>
    public IActiveMqBusTopology Topology => _context.Topology;

    Task<ISession> ConnectionContext.CreateSessionAsync(CancellationToken cancellationToken)
    {
        return _context.CreateSessionAsync(cancellationToken);
    }

    /// <summary>
    /// Determines whether virtual topic consumer.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool IsVirtualTopicConsumer(string name)
    {
        return _context.IsVirtualTopicConsumer(name);
    }

    /// <summary>
    /// Gets temporary queue.
    /// </summary>
    /// <param name="session">The session value.</param>
    /// <param name="topicName">The topic name value.</param>
    /// <returns>The result of the operation.</returns>
    public IQueue GetTemporaryQueue(ISession session, string topicName)
    {
        return _context.GetTemporaryQueue(session, topicName);
    }

    /// <summary>
    /// Gets temporary topic.
    /// </summary>
    /// <param name="session">The session value.</param>
    /// <param name="topicName">The topic name value.</param>
    /// <returns>The result of the operation.</returns>
    public ITopic GetTemporaryTopic(ISession session, string topicName)
    {
        return _context.GetTemporaryTopic(session, topicName);
    }

    /// <summary>
    /// Attempts to get temporary entity.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="destination">The destination value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetTemporaryEntity(string name, out IDestination? destination)
    {
        return _context.TryGetTemporaryEntity(name, out destination);
    }

    /// <summary>
    /// Performs the try remove temporary entity operation.
    /// </summary>
    /// <param name="session">The session value.</param>
    /// <param name="name">The name value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryRemoveTemporaryEntity(ISession session, string name)
    {
        return _context.TryRemoveTemporaryEntity(session, name);
    }
}
