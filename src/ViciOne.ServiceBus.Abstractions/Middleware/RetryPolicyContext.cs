using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Owns the retry state and cancellation lifetime for one pipeline operation.</summary>
/// <typeparam name="TContext">The governed pipeline context type.</typeparam>
public interface RetryPolicyContext<TContext> :
    IDisposable
    where TContext : class
{
    /// <summary>Gets the pipeline context governed by the policy.</summary>
    TContext Context { get; }

    /// <summary>Evaluates the initial failure and returns its retry decision.</summary>
    /// <param name="exception">The exception raised by the initial attempt.</param>
    /// <param name="retryContext">The state for the resulting decision.</param>
    /// <returns><see langword="true" /> when another attempt is permitted; otherwise, <see langword="false" />.</returns>
    bool CanRetry(Exception exception, out RetryContext<TContext> retryContext);

    /// <summary>Notifies the policy that a retry attempt failed.</summary>
    /// <param name="exception">The exception raised by the retry attempt.</param>
    /// <param name="cancellationToken">The token that cancels notification.</param>
    /// <returns>A task that completes after fault notification.</returns>
    Task RetryFaultedAsync(Exception exception, CancellationToken cancellationToken = default);

    /// <summary>Cancels pending and subsequent retries for this operation.</summary>
    void Cancel();
}
