using System;
using System.Threading;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Provides a message and tracks its destinations during an in-memory dispatch.</summary>
/// <typeparam name="TMessage">The message type.</typeparam>
internal interface IMessageDeliveryContext<TMessage>
    where TMessage : class
{
    /// <summary>Gets the token that cancels the message's delivery lifetime.</summary>
    CancellationToken CancellationToken { get; }

    /// <summary>Gets the message being delivered.</summary>
    TMessage Message { get; }

    /// <summary>Gets the optional routing key used by direct and topic exchanges.</summary>
    string? RoutingKey { get; }

    /// <summary>Gets the optional time at which the queue may admit the message.</summary>
    DateTimeOffset? EnqueueTime { get; }

    /// <summary>Atomically reserves a destination so that it receives this dispatch at most once.</summary>
    /// <param name="sink">The destination to reserve.</param>
    /// <returns><see langword="true" /> when the destination was reserved; otherwise, <see langword="false" />.</returns>
    bool TryReserveDelivery(IMessageSink<TMessage> sink);
}
