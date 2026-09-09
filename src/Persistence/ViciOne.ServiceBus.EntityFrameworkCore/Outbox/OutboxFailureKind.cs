namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Classifies the most recent transactional-outbox delivery failure.</summary>
public enum OutboxFailureKind
{
    /// <summary>No failure is recorded.</summary>
    None = 0,
    /// <summary>The transport classified the failure as retryable.</summary>
    Transient = 1,
    /// <summary>The transport classified the failure as non-retryable.</summary>
    Permanent = 2,
    /// <summary>Persisted data violated a delivery invariant.</summary>
    InvariantViolation = 3,
    /// <summary>The configured maximum number of attempts was reached.</summary>
    RetryLimitExceeded = 4,
    /// <summary>No registered classifier recognized the failure.</summary>
    Unclassified = 5,
}
