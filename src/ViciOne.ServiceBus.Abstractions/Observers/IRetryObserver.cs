using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Receives notifications about retry events.</summary>
public interface IRetryObserver
{
    /// <summary>Runs after an operation acquires retry-policy state.</summary>
    /// <typeparam name="T">The pipeline context type.</typeparam>
    /// <param name="context">The operation's retry-policy state.</param>
    /// <returns>A task that completes after observation.</returns>
    Task PostCreateAsync<T>(RetryPolicyContext<T> context)
        where T : class, PipeContext;

    /// <summary>Runs after a handled failure is scheduled for retry.</summary>
    /// <typeparam name="T">The pipeline context type.</typeparam>
    /// <param name="context">The scheduled retry state.</param>
    /// <returns>A task that completes after observation.</returns>
    Task PostFaultAsync<T>(RetryContext<T> context)
        where T : class, PipeContext;

    /// <summary>Runs immediately before a retry attempt begins.</summary>
    /// <typeparam name="T">The pipeline context type.</typeparam>
    /// <param name="context">The retry state for the attempt.</param>
    /// <returns>A task that completes after observation.</returns>
    Task PreRetryAsync<T>(RetryContext<T> context)
        where T : class, PipeContext;

    /// <summary>Runs when a handled failure reaches a terminal retry decision.</summary>
    /// <typeparam name="T">The pipeline context type.</typeparam>
    /// <param name="context">The terminal retry state.</param>
    /// <returns>A task that completes after observation.</returns>
    Task RetryFaultAsync<T>(RetryContext<T> context)
        where T : class, PipeContext;

    /// <summary>Runs when an operation succeeds after at least one retry attempt.</summary>
    /// <typeparam name="T">The pipeline context type.</typeparam>
    /// <param name="context">The successful retry state.</param>
    /// <returns>A task that completes after observation.</returns>
    Task RetryCompleteAsync<T>(RetryContext<T> context)
        where T : class, PipeContext;
}
