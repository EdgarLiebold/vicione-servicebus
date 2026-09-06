using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Exposes state for consume retry operations.</summary>
public interface ConsumeRetryContext
{
    /// <summary>The retry attempt in progress, or zero if this is the first time through.</summary>
    int RetryAttempt { get; }

    /// <summary>
    /// The number of retries that have already been attempted, note that this is zero
    /// on the first retry attempt.
    /// </summary>
    int RetryCount { get; }

    /// <summary>Creates next.</summary>
    /// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
    /// <param name="retryContext">The retry context.</param>
    /// <returns>The created next.</returns>
    TContext CreateNext<TContext>(RetryContext retryContext)
        where TContext : class, ConsumeRetryContext;

    /// <summary>Notifies registered observers about pending faults.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task NotifyPendingFaultsAsync(CancellationToken cancellationToken = default);
}
