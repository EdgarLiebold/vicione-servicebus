namespace ViciOne.ServiceBus;

/// <summary>
/// Classifies a failure for technical message retry.
/// </summary>
public enum RetryFailureKind
{
    /// <summary>
    /// No owner has classified the failure. Unclassified failures do not enter the standard technical
    /// retry policy.
    /// </summary>
    Unclassified = 0,

    /// <summary>
    /// The failure is explicitly recoverable and may use the bounded technical retry policy.
    /// </summary>
    Transient = 1,

    /// <summary>
    /// Retrying cannot correct the failure and must not consume the technical retry budget.
    /// </summary>
    NonRetryable = 2,
}
