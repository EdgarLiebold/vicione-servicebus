using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>
/// An initial context acquired to begin a retry filter
/// </summary>
/// <typeparam name="TContext"></typeparam>
public interface RetryPolicyContext<TContext> :
    IDisposable
    where TContext : class
{
    /// <summary>
    /// The context being managed by the retry policy
    /// </summary>
    TContext Context { get; }

    /// <summary>
    /// Determines if the exception can be retried
    /// </summary>
    /// <param name="exception">The exception that occurred</param>
    /// <param name="retryContext">The retry context for the retry</param>
    /// <returns>True if the task should be retried</returns>
    bool CanRetry(Exception exception, out RetryContext<TContext> retryContext);

    /// <summary>
    /// Called after the retry attempt has failed
    /// </summary>
    /// <param name="exception"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task RetryFaultedAsync(Exception exception, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel any pending or subsequent retries
    /// </summary>
    void Cancel();
}
