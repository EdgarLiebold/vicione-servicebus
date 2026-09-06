using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Carries state for base retry operations.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public class BaseRetryContext<TContext> :
    RetryContext
    where TContext : class, PipeContext
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="retryCount">The retry count.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    protected BaseRetryContext(TContext context, Exception exception, int retryCount, CancellationToken cancellationToken)
    {
        Context = context;
        Exception = exception;
        CancellationToken = cancellationToken;

        RetryCount = retryCount;
        RetryAttempt = retryCount + 1;
    }

    /// <summary>Gets the context.</summary>
    public TContext Context { get; }

    /// <summary>Gets the cancellation token.</summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>Gets the exception.</summary>
    public Exception Exception { get; }

    /// <summary>Gets the retry attempt.</summary>
    public int RetryAttempt { get; }

    /// <summary>Gets the retry count.</summary>
    public int RetryCount { get; }

    /// <summary>Gets the delay.</summary>
    public virtual TimeSpan? Delay => null;

    Type RetryContext.ContextType => typeof(TContext);

    /// <summary>Runs before retry.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual Task PreRetryAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
    }

    /// <summary>Reports that retry has faulted.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual Task RetryFaultedAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
    }
}
