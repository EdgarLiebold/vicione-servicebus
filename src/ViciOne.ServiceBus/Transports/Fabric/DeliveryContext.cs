using System;
using System.Threading;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Exposes state for delivery operations.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface DeliveryContext<T>
    where T : class
{
    /// <summary>Gets the cancellation token.</summary>
    CancellationToken CancellationToken { get; }

    /// <summary>The package being delivered.</summary>
    T Message { get; }

    /// <summary>Optional routing key, which is used by direct/topic exchanges.</summary>
    string? RoutingKey { get; }

    /// <summary>Optional enqueue time, which can be used to delay messages.</summary>
    DateTimeOffset? EnqueueTime { get; }

    /// <summary>If specified, targets a specific receiver in the message fabric.</summary>
    long? ReceiverId { get; }

    /// <summary>Should this delivery occur, or has is already been delivered.</summary>
    /// <param name="sink">The sink.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool WasAlreadyDelivered(IMessageSink<T> sink);

    /// <summary>Marks the sink as delivered for this dispatch.</summary>
    /// <param name="sink">The sink.</param>
    void Delivered(IMessageSink<T> sink);
}
