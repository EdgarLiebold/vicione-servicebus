using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Provides Event Hubs metadata and application properties for the event being consumed.</summary>
public interface EventHubConsumeContext :
    PartitionKeyConsumeContext
{
    /// <summary>Gets the UTC instant at which Event Hubs accepted the event.</summary>
    DateTimeOffset EnqueuedTime { get; }

    /// <summary>Gets the event's provider-defined partition offset.</summary>
    string OffsetString { get; }
    /// <summary>Gets the identifier of the partition from which the event was received.</summary>
    string PartitionId { get; }
    /// <summary>Gets the event's sequence number within its partition.</summary>
    long SequenceNumber { get; }
    /// <summary>Gets the system-managed Event Hubs properties.</summary>
    IReadOnlyDictionary<string, object> SystemProperties { get; }
    /// <summary>Gets the application properties attached to the event.</summary>
    IDictionary<string, object> Properties { get; }
}
