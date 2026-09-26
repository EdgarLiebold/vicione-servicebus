using System;
using System.Threading;
using System.Threading.Tasks;
using Apache.NMS;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Exposes an established Apache NMS connection and its ActiveMQ destination services.</summary>
public interface ConnectionContext :
    PipeContext
{
    /// <summary>Gets the underlying Apache NMS connection.</summary>
    IConnection Connection { get; }

    /// <summary>Gets the credential-safe broker description used for diagnostics.</summary>
    string Description { get; }

    /// <summary>Gets the configured broker address.</summary>
    Uri HostAddress { get; }

    /// <summary>Gets the ActiveMQ bus topology.</summary>
    IActiveMqBusTopology Topology { get; }

    /// <summary>Creates an Apache NMS session that uses individual acknowledgement.</summary>
    /// <param name="cancellationToken">The token used to cancel session creation.</param>
    /// <returns>A task that produces the newly created session.</returns>
    Task<ISession> CreateSessionAsync(CancellationToken cancellationToken);

    /// <summary>Determines whether a destination name matches the configured virtual-topic consumer pattern.</summary>
    /// <param name="name">The destination name to test.</param>
    /// <returns><see langword="true" /> when the name identifies a virtual-topic consumer; otherwise, <see langword="false" />.</returns>
    bool IsVirtualTopicConsumer(string name);

    /// <summary>Gets or creates the cached temporary queue for a destination name.</summary>
    /// <param name="session">The session used to resolve the destination.</param>
    /// <param name="topicName">The destination name used as the cache key.</param>
    /// <returns>The cached or newly resolved temporary queue.</returns>
    IQueue GetTemporaryQueue(ISession session, string topicName);

    /// <summary>Gets or creates the cached temporary topic for a destination name.</summary>
    /// <param name="session">The session used to resolve the destination.</param>
    /// <param name="topicName">The destination name used as the cache key.</param>
    /// <returns>The cached or newly resolved temporary topic.</returns>
    ITopic GetTemporaryTopic(ISession session, string topicName);

    /// <summary>Tries to retrieve a cached temporary destination by name and destination type.</summary>
    /// <param name="name">The destination name.</param>
    /// <param name="destinationType">The queue or topic destination type.</param>
    /// <param name="destination">The cached destination, when found.</param>
    /// <returns><see langword="true" /> when the destination is cached; otherwise, <see langword="false" />.</returns>
    bool TryGetTemporaryEntity(string name, DestinationType destinationType, out IDestination? destination);

    /// <summary>Tries to remove a cached temporary destination and delete it from the broker.</summary>
    /// <param name="session">The session used to delete the broker destination.</param>
    /// <param name="name">The cached destination name.</param>
    /// <param name="destinationType">The queue or topic destination type to remove.</param>
    /// <returns><see langword="true" /> when a cached destination was deleted; otherwise, <see langword="false" />.</returns>
    bool TryRemoveTemporaryEntity(ISession session, string name, DestinationType destinationType);
}
