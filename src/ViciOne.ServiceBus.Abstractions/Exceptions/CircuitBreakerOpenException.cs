using System;

namespace ViciOne.ServiceBus;
/// <summary>
/// The configured circuit breaker rejected an operation before it reached the protected pipe.
/// </summary>
public sealed class CircuitBreakerOpenException : ViciOneServiceBusException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="retryAfter">The retry after value.</param>
    /// <param name="probeInProgress">The probe in progress value.</param>
    /// <param name="lastFailure">The last failure value.</param>
    public CircuitBreakerOpenException(TimeSpan retryAfter, bool probeInProgress, Exception? lastFailure)
        : base(CreateMessage(retryAfter, probeInProgress), lastFailure)
    {
        if (retryAfter < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(retryAfter), retryAfter, "The retry delay cannot be negative.");

        RetryAfter = retryAfter;
        ProbeInProgress = probeInProgress;
    }

    /// <summary>
    /// Minimum delay reported by the breaker before another recovery probe can be attempted.
    /// A zero value means that another probe may be attempted as soon as the current probe completes.
    /// </summary>
    public TimeSpan RetryAfter { get; }

    /// <summary>
    /// Indicates that the exclusive half-open probe is currently owned by another caller.
    /// </summary>
    public bool ProbeInProgress { get; }

    private static string CreateMessage(TimeSpan retryAfter, bool probeInProgress) =>
        probeInProgress
            ? "The circuit breaker is half-open and its exclusive recovery probe is already in progress."
            : $"The circuit breaker is open. Retry after {retryAfter}.";
}
