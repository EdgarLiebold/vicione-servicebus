using System;
using System.Collections.Generic;
using System.Threading;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>Carries state for in memory delivery operations.</summary>
public class InMemoryDeliveryContext :
    DeliveryContext<InMemoryTransportMessage>
{
    readonly HashSet<IMessageSink<InMemoryTransportMessage>> _delivered;
    readonly DateTime? _enqueueTime;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="utcNow">The utc now.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public InMemoryDeliveryContext(InMemoryTransportMessage message, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        Message = message;
        CancellationToken = cancellationToken;
        _enqueueTime = message.Delay.HasValue ? utcNow.UtcDateTime + message.Delay.Value : null;

        _delivered = new HashSet<IMessageSink<InMemoryTransportMessage>>();
    }

    /// <summary>Gets the cancellation token.</summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>Gets the message.</summary>
    public InMemoryTransportMessage Message { get; }
    /// <summary>Gets the routing key.</summary>
    public string? RoutingKey => Message.RoutingKey;
    /// <summary>Gets the enqueue time.</summary>
    public DateTimeOffset? EnqueueTime => _enqueueTime;
    /// <summary>Gets the receiver id.</summary>
    public long? ReceiverId => default;

    /// <summary>Determines whether the message has already been delivered to the specified sink.</summary>
    /// <param name="sink">The sink.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool WasAlreadyDelivered(IMessageSink<InMemoryTransportMessage> sink)
    {
        return _delivered.Contains(sink);
    }

    /// <summary>Records that the message was delivered to the specified sink.</summary>
    /// <param name="sink">The sink.</param>
    public void Delivered(IMessageSink<InMemoryTransportMessage> sink)
    {
        _delivered.Add(sink);
    }
}
