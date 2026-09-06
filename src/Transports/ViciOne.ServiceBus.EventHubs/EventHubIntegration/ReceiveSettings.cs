using System;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Provides immutable Event Hubs receive, concurrency, and checkpoint settings to the processor pipeline.</summary>
public interface ReceiveSettings
{
    /// <summary>Gets the consumer group used to coordinate partition ownership.</summary>
    string ConsumerGroup { get; }
    /// <summary>Gets the Blob container name used for ownership and checkpoints.</summary>
    string ContainerName { get; }
    /// <summary>Gets the Event Hub entity name.</summary>
    string EventHubName { get; }
    /// <summary>Gets the maximum number of uncheckpointed events retained for one partition.</summary>
    ushort CheckpointMessageLimit { get; }
    /// <summary>Gets the number of completed events that triggers a checkpoint.</summary>
    ushort CheckpointMessageCount { get; }
    /// <summary>Gets the receive-side admission limit used before partitioned dispatch.</summary>
    int PrefetchCount { get; }
    /// <summary>Gets the maximum time between completed partition checkpoints.</summary>
    TimeSpan CheckpointInterval { get; }
    /// <summary>Gets the endpoint-wide concurrent message limit.</summary>
    int ConcurrentMessageLimit { get; }
    /// <summary>Gets the concurrent delivery limit for a partition key.</summary>
    int ConcurrentDeliveryLimit { get; }
}
