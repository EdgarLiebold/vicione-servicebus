using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Provides immutable retry-policy state for a pipeline context.</summary>
/// <typeparam name="TContext">The pipeline context type governed by the policy.</typeparam>
internal class BaseRetryContext<TContext> :
    RetryContext
    where TContext : class, PipeContext
{
    /// <summary>Initializes retry state after a failed pipeline attempt.</summary>
    /// <param name="context">The pipeline context governed by the retry policy.</param>
    /// <param name="exception">The exception that triggered retry evaluation.</param>
    /// <param name="retryCount">The number of retry attempts already completed.</param>
    /// <param name="cancellationToken">The token that cancels retry processing.</param>
    protected BaseRetryContext(TContext context, Exception exception, int retryCount, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentOutOfRangeException.ThrowIfNegative(retryCount);

        Context = context;
        Exception = exception;
        CancellationToken = cancellationToken;

        RetryCount = retryCount;
        RetryAttempt = retryCount + 1;
    }

    /// <summary>Gets the pipeline context governed by the retry policy.</summary>
    public TContext Context { get; }

    /// <summary>Gets the token that cancels retry processing.</summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>Gets the exception that triggered retry evaluation.</summary>
    public Exception Exception { get; }

    /// <summary>Gets the one-based retry attempt that follows the failed operation.</summary>
    public int RetryAttempt { get; }

    /// <summary>Gets the number of retry attempts completed before this decision.</summary>
    public int RetryCount { get; }

    /// <summary>Gets the delay before retry; the base policy retries immediately.</summary>
    public virtual TimeSpan? Delay => null;

    Type RetryContext.ContextType => typeof(TContext);

    /// <summary>Completes immediately unless pre-retry work has already been canceled.</summary>
    /// <param name="cancellationToken">The token that cancels pre-retry work.</param>
    /// <returns>A completed or canceled task.</returns>
    public virtual Task PreRetryAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken);

        return Task.CompletedTask;
    }

    /// <summary>Completes fault notification immediately unless notification has already been canceled.</summary>
    /// <param name="exception">The exception raised by the retry attempt.</param>
    /// <param name="cancellationToken">The token that cancels fault notification.</param>
    /// <returns>A completed or canceled task.</returns>
    public virtual Task RetryFaultedAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken);

        return Task.CompletedTask;
    }
}
