using System;
using System.Collections.Generic;
using System.Threading;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport.Runtime;

/// <summary>Tracks one in-memory fabric delivery and the sinks that have accepted it.</summary>
internal sealed class InMemoryDeliveryContext :
    DeliveryContext<InMemoryTransportMessage>
{
    readonly HashSet<IMessageSink<InMemoryTransportMessage>> _delivered;
    readonly object _deliveredLock;
    readonly DateTimeOffset? _enqueueTime;

    /// <summary>Creates delivery state for a transport message.</summary>
    /// <param name="message">The message delivered through the fabric.</param>
    /// <param name="utcNow">The current transport time used to resolve a relative delay.</param>
    /// <param name="cancellationToken">The token that cancels queue admission or delayed delivery.</param>
    public InMemoryDeliveryContext(InMemoryTransportMessage message, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        CancellationToken = cancellationToken;
        _enqueueTime = message.Delay.HasValue ? utcNow + message.Delay.Value : null;

        _delivered = new HashSet<IMessageSink<InMemoryTransportMessage>>();
        _deliveredLock = new object();
    }

    /// <summary>Gets the token that cancels this delivery.</summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>Gets the transport message being delivered.</summary>
    public InMemoryTransportMessage Message { get; }
    /// <summary>Gets the routing key used by direct and topic exchanges.</summary>
    public string? RoutingKey => Message.RoutingKey;
    /// <summary>Gets the absolute enqueue time for a delayed message.</summary>
    public DateTimeOffset? EnqueueTime => _enqueueTime;
    /// <summary>Gets the optional receiver identifier; in-memory deliveries are not receiver-pinned.</summary>
    public long? ReceiverId => default;

    /// <summary>Determines whether the message has already been delivered to the specified sink.</summary>
    /// <param name="sink">The candidate sink.</param>
    /// <returns><see langword="true" /> when the sink already accepted this delivery; otherwise, <see langword="false" />.</returns>
    public bool WasAlreadyDelivered(IMessageSink<InMemoryTransportMessage> sink)
    {
        ArgumentNullException.ThrowIfNull(sink);
        lock (_deliveredLock)
            return _delivered.Contains(sink);
    }

    /// <summary>Records that the message was delivered to the specified sink.</summary>
    /// <param name="sink">The sink that accepted the delivery.</param>
    public void Delivered(IMessageSink<InMemoryTransportMessage> sink)
    {
        ArgumentNullException.ThrowIfNull(sink);
        lock (_deliveredLock)
            _delivered.Add(sink);
    }
}
