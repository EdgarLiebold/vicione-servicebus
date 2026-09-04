namespace ViciOne.ServiceBus.EntityFrameworkCore;


/// <summary>
/// Specifies the available outbox failure kind values.
/// </summary>
public enum OutboxFailureKind
{
    /// <summary>
    /// Indicates none.
    /// </summary>
    None = 0,
    /// <summary>
    /// Indicates transient.
    /// </summary>
    Transient = 1,
    /// <summary>
    /// Indicates permanent.
    /// </summary>
    Permanent = 2,
    /// <summary>
    /// Indicates invariant violation.
    /// </summary>
    InvariantViolation = 3,
    /// <summary>
    /// Indicates retry limit exceeded.
    /// </summary>
    RetryLimitExceeded = 4,
    /// <summary>
    /// Indicates unclassified.
    /// </summary>
    Unclassified = 5
}
