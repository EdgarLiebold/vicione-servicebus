using System;
using ViciOne.ServiceBus.SqlTransport;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>
/// Defines the contract for sql message context.
/// </summary>
public interface SqlMessageContext :
    RoutingKeyConsumeContext,
    PartitionKeyConsumeContext
{
    /// <summary>
    /// Gets the transport message value.
    /// </summary>
    SqlTransportMessage TransportMessage { get; }

    /// <summary>
    /// Gets the transport message id value.
    /// </summary>
    Guid TransportMessageId { get; }
    /// <summary>
    /// Gets the delivery message id value.
    /// </summary>
    long DeliveryMessageId { get; }

    /// <summary>
    /// Gets the queue name value.
    /// </summary>
    string QueueName { get; }

    /// <summary>
    /// Gets the consumer id value.
    /// </summary>
    Guid? ConsumerId { get; }
    /// <summary>
    /// Gets the lock id value.
    /// </summary>
    Guid? LockId { get; }

    /// <summary>
    /// Gets the priority value.
    /// </summary>
    short Priority { get; }
    /// <summary>
    /// Gets the enqueue time value.
    /// </summary>
    DateTimeOffset EnqueueTime { get; }
    /// <summary>
    /// Gets the delivery count value.
    /// </summary>
    int DeliveryCount { get; }
}
