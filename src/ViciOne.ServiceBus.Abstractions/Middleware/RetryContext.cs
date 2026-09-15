using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Describes one retry-policy decision and its execution state.</summary>
public interface RetryContext
{
    /// <summary>Gets the token that cancels retry processing without canceling the underlying operation.</summary>
    CancellationToken CancellationToken { get; }

    /// <summary>Gets the exception that triggered retry evaluation.</summary>
    Exception Exception { get; }

    /// <summary>Gets the one-based retry attempt that follows the failed operation.</summary>
    int RetryAttempt { get; }

    /// <summary>Gets the number of retry attempts completed before this decision.</summary>
    int RetryCount { get; }

    /// <summary>
    /// Gets the delay before the represented retry, or <see langword="null" /> when the retry is immediate
    /// or the decision is terminal.
    /// </summary>
    TimeSpan? Delay { get; }

    /// <summary>Gets the pipeline context type governed by the retry policy.</summary>
    Type ContextType { get; }

    /// <summary>Notifies the policy that a retry attempt failed.</summary>
    /// <param name="exception">The exception raised by the retry attempt.</param>
    /// <param name="cancellationToken">The token that cancels notification.</param>
    /// <returns>A task that completes after fault notification.</returns>
    Task RetryFaultedAsync(Exception exception, CancellationToken cancellationToken = default);

    /// <summary>Performs policy work required immediately before the retry attempt.</summary>
    /// <param name="cancellationToken">The token that cancels the pre-retry work.</param>
    /// <returns>A task that completes when the retry may begin.</returns>
    Task PreRetryAsync(CancellationToken cancellationToken = default);
}


/// <summary>Describes retry-policy state for a strongly typed pipeline context.</summary>
/// <typeparam name="TContext">The pipeline context type.</typeparam>
public interface RetryContext<TContext> :
    RetryContext
    where TContext : class
{
    /// <summary>Gets the pipeline context governed by the retry policy.</summary>
    TContext Context { get; }

    /// <summary>Determines whether an exception permits another retry attempt.</summary>
    /// <param name="exception">The exception raised by the failed attempt.</param>
    /// <param name="retryContext">The state for the resulting retry or terminal decision.</param>
    /// <returns><see langword="true" /> when another attempt is permitted; otherwise, <see langword="false" />.</returns>
    bool CanRetry(Exception exception, out RetryContext<TContext> retryContext);
}
