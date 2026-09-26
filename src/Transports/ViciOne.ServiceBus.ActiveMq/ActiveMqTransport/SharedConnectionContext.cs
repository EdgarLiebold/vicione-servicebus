using System;
using System.Threading;
using System.Threading.Tasks;
using Apache.NMS;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Applies an operation-specific cancellation scope to a shared ActiveMQ connection context.</summary>
public class SharedConnectionContext :
    ProxyPipeContext,
    ConnectionContext
{
    readonly ConnectionContext _context;

    /// <summary>Creates a scoped proxy over an established connection context.</summary>
    /// <param name="context">The underlying connection context.</param>
    /// <param name="cancellationToken">The token associated with this operation scope.</param>
    public SharedConnectionContext(ConnectionContext context, CancellationToken cancellationToken)
        : base(context)
    {
        _context = context;
        CancellationToken = cancellationToken;
    }

    /// <summary>Gets the token associated with this operation scope.</summary>
    public override CancellationToken CancellationToken { get; }

    IConnection ConnectionContext.Connection => _context.Connection;
    /// <inheritdoc />
    public string Description => _context.Description;
    /// <inheritdoc />
    public Uri HostAddress => _context.HostAddress;
    /// <inheritdoc />
    public IActiveMqBusTopology Topology => _context.Topology;

    Task<ISession> ConnectionContext.CreateSessionAsync(CancellationToken cancellationToken)
    {
        return _context.CreateSessionAsync(cancellationToken);
    }

    /// <inheritdoc />
    public bool IsVirtualTopicConsumer(string name)
    {
        return _context.IsVirtualTopicConsumer(name);
    }

    /// <inheritdoc />
    public IQueue GetTemporaryQueue(ISession session, string topicName)
    {
        return _context.GetTemporaryQueue(session, topicName);
    }

    /// <inheritdoc />
    public ITopic GetTemporaryTopic(ISession session, string topicName)
    {
        return _context.GetTemporaryTopic(session, topicName);
    }

    /// <inheritdoc />
    public bool TryGetTemporaryEntity(string name, DestinationType destinationType, out IDestination? destination)
    {
        return _context.TryGetTemporaryEntity(name, destinationType, out destination);
    }

    /// <inheritdoc />
    public bool TryRemoveTemporaryEntity(ISession session, string name, DestinationType destinationType)
    {
        return _context.TryRemoveTemporaryEntity(session, name, destinationType);
    }
}
