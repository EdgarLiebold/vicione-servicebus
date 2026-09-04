namespace ViciOne.ServiceBus.Operations;

/// <summary>Persisted producer-side durable-send lifecycle state.</summary>
public enum DurableSendStatus
{
    /// <summary>
    /// Indicates pending.
    /// </summary>
    Pending = 0,
    /// <summary>
    /// Indicates retry scheduled.
    /// </summary>
    RetryScheduled = 1,
    /// <summary>
    /// Indicates quarantined.
    /// </summary>
    Quarantined = 2,
    /// <summary>
    /// Indicates awaiting consumer completion.
    /// </summary>
    AwaitingConsumerCompletion = 3,
}
