using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Defines the contract for event hub consume context.
/// </summary>
public interface EventHubConsumeContext :
    PartitionKeyConsumeContext
{
    /// <summary>
    /// Gets the enqueued time value.
    /// </summary>
    DateTimeOffset EnqueuedTime { get; }

    /// <summary>
    /// Gets the offset string value.
    /// </summary>
    string OffsetString { get; }
    /// <summary>
    /// Gets the partition id value.
    /// </summary>
    string PartitionId { get; }
    /// <summary>
    /// Gets the sequence number value.
    /// </summary>
    long SequenceNumber { get; }
    /// <summary>
    /// Gets the system properties value.
    /// </summary>
    IReadOnlyDictionary<string, object> SystemProperties { get; }
    /// <summary>
    /// Gets the properties value.
    /// </summary>
    IDictionary<string, object> Properties { get; }
}
