using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// The context of a Message from AzureServiceBus - gives access to the transport
/// message when requested.
/// </summary>
public interface ServiceBusMessageContext :
    PartitionKeyConsumeContext
{
    /// <summary>
    /// Gets the delivery count value.
    /// </summary>
    int DeliveryCount { get; }
    /// <summary>
    /// Gets the label value.
    /// </summary>
    string Label { get; }
    /// <summary>
    /// Gets the sequence number value.
    /// </summary>
    long SequenceNumber { get; }
    /// <summary>
    /// Gets the enqueued sequence number value.
    /// </summary>
    long EnqueuedSequenceNumber { get; }
    /// <summary>
    /// Gets the lock token value.
    /// </summary>
    string LockToken { get; }
    /// <summary>
    /// Gets the locked until value.
    /// </summary>
    DateTimeOffset LockedUntil { get; }
    /// <summary>
    /// Gets the session id value.
    /// </summary>
    string SessionId { get; }
    /// <summary>
    /// Gets the size value.
    /// </summary>
    long Size { get; }
    /// <summary>
    /// Gets the to value.
    /// </summary>
    string To { get; }
    /// <summary>
    /// Gets the reply to session id value.
    /// </summary>
    string ReplyToSessionId { get; }
    /// <summary>
    /// Gets the reply to value.
    /// </summary>
    string ReplyTo { get; }
    /// <summary>
    /// Gets the enqueued time value.
    /// </summary>
    DateTimeOffset EnqueuedTime { get; }
    /// <summary>
    /// Gets the scheduled enqueue time value.
    /// </summary>
    DateTimeOffset ScheduledEnqueueTime { get; }
    /// <summary>
    /// Gets the properties value.
    /// </summary>
    IReadOnlyDictionary<string, object> Properties { get; }
    /// <summary>
    /// Gets the time to live value.
    /// </summary>
    TimeSpan TimeToLive { get; }
    /// <summary>
    /// Gets the correlation id value.
    /// </summary>
    string CorrelationId { get; }
    /// <summary>
    /// Gets the message id value.
    /// </summary>
    string MessageId { get; }
    /// <summary>
    /// Gets the expires at value.
    /// </summary>
    DateTimeOffset ExpiresAt { get; }
}
