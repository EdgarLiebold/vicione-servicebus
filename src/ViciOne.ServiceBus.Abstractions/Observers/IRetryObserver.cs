using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Receives notifications about retry events.</summary>
public interface IRetryObserver
{
    /// <summary>Called before a message is dispatched to any consumers.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PostCreateAsync<T>(RetryPolicyContext<T> context)
        where T : class, PipeContext;

    /// <summary>Called after a fault has occurred, but will be retried.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PostFaultAsync<T>(RetryContext<T> context)
        where T : class, PipeContext;

    /// <summary>Called immediately before an exception will be retried.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PreRetryAsync<T>(RetryContext<T> context)
        where T : class, PipeContext;

    /// <summary>Called when the retry filter is no longer going to retry, and the context is faulted.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RetryFaultAsync<T>(RetryContext<T> context)
        where T : class, PipeContext;

    /// <summary>Called when the retry filter retried at least once, and the context completed successfully.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RetryCompleteAsync<T>(RetryContext<T> context)
        where T : class, PipeContext;
}
