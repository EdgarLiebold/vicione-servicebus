namespace ViciOne.ServiceBus;

/// <summary>Persisted reason category for the last durable-send delivery failure.</summary>
public enum DurableSendFailureKind
{
    None = 0,
    Transient = 1,
    NonRetryable = 2,
    Unclassified = 3,
    RetryLimitExceeded = 4,
    InvariantViolation = 5,
    ConsumerCompletionTimeout = 6,
}
