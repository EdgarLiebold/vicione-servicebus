using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides retry counters, nested retry contexts, and deferred fault notification for a consumed message.</summary>
public interface ConsumeRetryContext
{
    /// <summary>Gets the one-based retry attempt currently in progress, or zero before the first retry.</summary>
    int RetryAttempt { get; }

    /// <summary>
    /// Gets the number of retry attempts completed before the current attempt.
    /// </summary>
    int RetryCount { get; }

    /// <summary>Creates a nested consume-retry context for the next retry policy.</summary>
    /// <typeparam name="TContext">The requested consume-retry context contract.</typeparam>
    /// <param name="retryContext">The retry policy state for the nested context.</param>
    /// <returns>The nested consume-retry context.</returns>
    TContext CreateNext<TContext>(RetryContext retryContext)
        where TContext : class, ConsumeRetryContext;

    /// <summary>Notifies registered observers about pending faults.</summary>
    /// <param name="cancellationToken">The token that cancels observer notification.</param>
    /// <returns>A task that completes after every pending fault observer is notified.</returns>
    Task NotifyPendingFaultsAsync(CancellationToken cancellationToken = default);
}
