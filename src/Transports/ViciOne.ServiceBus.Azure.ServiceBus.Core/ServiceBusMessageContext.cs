using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus;

/// <summary>
/// The context of a Message from AzureServiceBus - gives access to the transport
/// message when requested.
/// </summary>
public interface ServiceBusMessageContext :
    PartitionKeyConsumeContext
{
    int DeliveryCount { get; }
    string Label { get; }
    long SequenceNumber { get; }
    long EnqueuedSequenceNumber { get; }
    string LockToken { get; }
    DateTimeOffset LockedUntil { get; }
    string SessionId { get; }
    long Size { get; }
    string To { get; }
    string ReplyToSessionId { get; }
    string ReplyTo { get; }
    DateTimeOffset EnqueuedTime { get; }
    DateTimeOffset ScheduledEnqueueTime { get; }
    IReadOnlyDictionary<string, object> Properties { get; }
    TimeSpan TimeToLive { get; }
    string CorrelationId { get; }
    string MessageId { get; }
    DateTimeOffset ExpiresAt { get; }
}
