using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>
/// Provides a base retry context implementation.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
public class BaseRetryContext<TContext> :
    RetryContext
    where TContext : class, PipeContext
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="retryCount">The retry count value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    protected BaseRetryContext(TContext context, Exception exception, int retryCount, CancellationToken cancellationToken)
    {
        Context = context;
        Exception = exception;
        CancellationToken = cancellationToken;

        RetryCount = retryCount;
        RetryAttempt = retryCount + 1;
    }

    /// <summary>
    /// Gets the context value.
    /// </summary>
    public TContext Context { get; }

    /// <summary>
    /// Gets the cancellation token value.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Gets the exception value.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Gets the retry attempt value.
    /// </summary>
    public int RetryAttempt { get; }

    /// <summary>
    /// Gets the retry count value.
    /// </summary>
    public int RetryCount { get; }

    /// <summary>
    /// Gets the delay value.
    /// </summary>
    public virtual TimeSpan? Delay => null;

    Type RetryContext.ContextType => typeof(TContext);

    /// <summary>
    /// Performs the pre retry operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public virtual Task PreRetryAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
    }

    /// <summary>
    /// Performs the retry faulted operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public virtual Task RetryFaultedAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
    }
}
