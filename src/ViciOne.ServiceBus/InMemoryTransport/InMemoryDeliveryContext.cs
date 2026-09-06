using System;
using System.Collections.Generic;
using System.Threading;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>
/// Provides an in memory delivery context implementation.
/// </summary>
public class InMemoryDeliveryContext :
    DeliveryContext<InMemoryTransportMessage>
{
    readonly HashSet<IMessageSink<InMemoryTransportMessage>> _delivered;
    readonly DateTime? _enqueueTime;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="utcNow">The utc now value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public InMemoryDeliveryContext(InMemoryTransportMessage message, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        Message = message;
        CancellationToken = cancellationToken;
        _enqueueTime = message.Delay.HasValue ? utcNow.UtcDateTime + message.Delay.Value : null;

        _delivered = new HashSet<IMessageSink<InMemoryTransportMessage>>();
    }

    /// <summary>
    /// Gets the cancellation token value.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Gets the message value.
    /// </summary>
    public InMemoryTransportMessage Message { get; }
    /// <summary>
    /// Gets the routing key value.
    /// </summary>
    public string? RoutingKey => Message.RoutingKey;
    /// <summary>
    /// Gets the enqueue time value.
    /// </summary>
    public DateTimeOffset? EnqueueTime => _enqueueTime;
    /// <summary>
    /// Gets the receiver id value.
    /// </summary>
    public long? ReceiverId => default;

    /// <summary>
    /// Performs the was already delivered operation.
    /// </summary>
    /// <param name="sink">The sink value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool WasAlreadyDelivered(IMessageSink<InMemoryTransportMessage> sink)
    {
        return _delivered.Contains(sink);
    }

    /// <summary>
    /// Performs the delivered operation.
    /// </summary>
    /// <param name="sink">The sink value.</param>
    public void Delivered(IMessageSink<InMemoryTransportMessage> sink)
    {
        _delivered.Add(sink);
    }
}
