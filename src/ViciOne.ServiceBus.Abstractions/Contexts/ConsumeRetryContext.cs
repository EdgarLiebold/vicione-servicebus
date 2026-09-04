using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Defines the contract for consume retry context.
/// </summary>
public interface ConsumeRetryContext
{
    /// <summary>
    /// The retry attempt in progress, or zero if this is the first time through
    /// </summary>
    int RetryAttempt { get; }

    /// <summary>
    /// The number of retries that have already been attempted, note that this is zero
    /// on the first retry attempt
    /// </summary>
    int RetryCount { get; }

    /// <summary>
    /// Creates next.
    /// </summary>
    /// <typeparam name="TContext">The t context type.</typeparam>
    /// <param name="retryContext">The retry context value.</param>
    /// <returns>The result of the operation.</returns>
    TContext CreateNext<TContext>(RetryContext retryContext)
        where TContext : class, ConsumeRetryContext;

    /// <summary>
    /// Performs the notify pending faults operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task NotifyPendingFaultsAsync(CancellationToken cancellationToken = default);
}
