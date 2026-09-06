namespace ViciOne.ServiceBus.Operations;

/// <summary>Persisted reason category for the last durable-send delivery failure.</summary>
public enum DurableSendFailureKind
{
    /// <summary>Indicates none.</summary>
    None = 0,
    /// <summary>Indicates transient.</summary>
    Transient = 1,
    /// <summary>Indicates non retryable.</summary>
    NonRetryable = 2,
    /// <summary>Indicates unclassified.</summary>
    Unclassified = 3,
    /// <summary>Indicates retry limit exceeded.</summary>
    RetryLimitExceeded = 4,
    /// <summary>Indicates invariant violation.</summary>
    InvariantViolation = 5,
    /// <summary>Indicates consumer completion timeout.</summary>
    ConsumerCompletionTimeout = 6,
}
