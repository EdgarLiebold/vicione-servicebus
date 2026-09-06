using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Exposes Azure Service Bus delivery metadata attached to a consume context.
/// </summary>
public interface ServiceBusMessageContext :
    PartitionKeyConsumeContext
{
    /// <summary>Gets the number of times Azure Service Bus has delivered the message.</summary>
    int DeliveryCount { get; }
    /// <summary>Gets the application subject label.</summary>
    string Label { get; }
    /// <summary>Gets the broker-assigned sequence number.</summary>
    long SequenceNumber { get; }
    /// <summary>Gets the original sequence number for a message forwarded from another entity.</summary>
    long EnqueuedSequenceNumber { get; }
    /// <summary>Gets the lock token for the current delivery.</summary>
    string LockToken { get; }
    /// <summary>Gets the UTC instant at which the current message lock expires.</summary>
    DateTimeOffset LockedUntil { get; }
    /// <summary>Gets the session identifier.</summary>
    string SessionId { get; }
    /// <summary>Gets the message size in bytes.</summary>
    long Size { get; }
    /// <summary>Gets the logical destination stored on the message.</summary>
    string To { get; }
    /// <summary>Gets the session identifier expected on replies.</summary>
    string ReplyToSessionId { get; }
    /// <summary>Gets the reply destination entity path.</summary>
    string ReplyTo { get; }
    /// <summary>Gets the UTC instant at which the broker enqueued the message.</summary>
    DateTimeOffset EnqueuedTime { get; }
    /// <summary>Gets the requested scheduled enqueue time.</summary>
    DateTimeOffset ScheduledEnqueueTime { get; }
    /// <summary>Gets the Azure Service Bus application properties.</summary>
    IReadOnlyDictionary<string, object> Properties { get; }
    /// <summary>Gets the message time-to-live interval.</summary>
    TimeSpan TimeToLive { get; }
    /// <summary>Gets the application correlation identifier.</summary>
    string CorrelationId { get; }
    /// <summary>Gets the application message identifier.</summary>
    string MessageId { get; }
    /// <summary>Gets the UTC instant at which the broker considers the message expired.</summary>
    DateTimeOffset ExpiresAt { get; }
}
