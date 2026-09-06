using System;
using ViciOne.ServiceBus.SqlTransport;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Exposes state for sql message operations.</summary>
public interface SqlMessageContext :
    RoutingKeyConsumeContext,
    PartitionKeyConsumeContext
{
    /// <summary>Gets the transport message.</summary>
    SqlTransportMessage TransportMessage { get; }

    /// <summary>Gets the transport message id.</summary>
    Guid TransportMessageId { get; }
    /// <summary>Gets the delivery message id.</summary>
    long DeliveryMessageId { get; }

    /// <summary>Gets the queue name.</summary>
    string QueueName { get; }

    /// <summary>Gets the consumer id.</summary>
    Guid? ConsumerId { get; }
    /// <summary>Gets the lock id.</summary>
    Guid? LockId { get; }

    /// <summary>Gets the priority.</summary>
    short Priority { get; }
    /// <summary>Gets the enqueue time.</summary>
    DateTimeOffset EnqueueTime { get; }
    /// <summary>Gets the delivery count.</summary>
    int DeliveryCount { get; }
}
