using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>An initial context acquired to begin a retry filter.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public interface RetryPolicyContext<TContext> :
    IDisposable
    where TContext : class
{
    /// <summary>The context being managed by the retry policy.</summary>
    TContext Context { get; }

    /// <summary>Determines if the exception can be retried.</summary>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="retryContext">The retry context for the retry.</param>
    /// <returns>True if the task should be retried.</returns>
    bool CanRetry(Exception exception, out RetryContext<TContext> retryContext);

    /// <summary>Called after the retry attempt has failed.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RetryFaultedAsync(Exception exception, CancellationToken cancellationToken = default);

    /// <summary>Cancel any pending or subsequent retries.</summary>
    void Cancel();
}
