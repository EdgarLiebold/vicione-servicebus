using System;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Defines the contract for receive settings.
/// </summary>
public interface ReceiveSettings
{
    /// <summary>
    /// Gets the consumer group value.
    /// </summary>
    string ConsumerGroup { get; }
    /// <summary>
    /// Gets the container name value.
    /// </summary>
    string ContainerName { get; }
    /// <summary>
    /// Gets the event hub name value.
    /// </summary>
    string EventHubName { get; }
    /// <summary>
    /// Gets the checkpoint message limit value.
    /// </summary>
    ushort CheckpointMessageLimit { get; }
    /// <summary>
    /// Gets the checkpoint message count value.
    /// </summary>
    ushort CheckpointMessageCount { get; }
    /// <summary>
    /// Gets the prefetch count value.
    /// </summary>
    int PrefetchCount { get; }
    /// <summary>
    /// Gets the checkpoint interval value.
    /// </summary>
    TimeSpan CheckpointInterval { get; }
    /// <summary>
    /// Gets the concurrent message limit value.
    /// </summary>
    int ConcurrentMessageLimit { get; }
    /// <summary>
    /// Gets the concurrent delivery limit value.
    /// </summary>
    int ConcurrentDeliveryLimit { get; }
}
