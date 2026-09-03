namespace ViciOne.ServiceBus;

/// <summary>Persisted producer-side durable-send lifecycle state.</summary>
public enum DurableSendStatus
{
    Pending = 0,
    RetryScheduled = 1,
    Quarantined = 2,
    AwaitingConsumerCompletion = 3,
}
