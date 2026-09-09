namespace ViciOne.ServiceBus.Operations;

/// <summary>Persisted reason category for the last durable-send delivery failure.</summary>
public enum DurableSendFailureKind
{
    /// <summary>No delivery failure has been recorded.</summary>
    None = 0,
    /// <summary>The transport failure is eligible for a bounded retry.</summary>
    Transient = 1,
    /// <summary>The transport rejected the delivery with a permanent failure.</summary>
    NonRetryable = 2,
    /// <summary>No registered classifier could establish that the failure is transient.</summary>
    Unclassified = 3,
    /// <summary>The configured delivery-attempt budget has been exhausted.</summary>
    RetryLimitExceeded = 4,
    /// <summary>The configured dispatcher returned a result that violates the durable-delivery contract.</summary>
    InvariantViolation = 5,
    /// <summary>A volatile delivery did not report logical consumer completion before its deadline.</summary>
    ConsumerCompletionTimeout = 6,
}
