namespace ViciOne.ServiceBus.Operations;

/// <summary>Persisted producer-side durable-send lifecycle state.</summary>
public enum DurableSendStatus
{
    /// <summary>The intent is immediately eligible for delivery.</summary>
    Pending = 0,
    /// <summary>The intent becomes eligible at its persisted retry time.</summary>
    RetryScheduled = 1,
    /// <summary>The intent requires an explicit operator decision.</summary>
    Quarantined = 2,
    /// <summary>A volatile transport accepted the intent and logical consumer completion is outstanding.</summary>
    AwaitingConsumerCompletion = 3,
}
