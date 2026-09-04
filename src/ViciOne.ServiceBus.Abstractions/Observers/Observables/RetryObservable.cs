using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Observables;

/// <summary>
/// Provides a retry observable implementation.
/// </summary>
public class RetryObservable :
    Connectable<IRetryObserver>,
    IRetryObserver
{
    /// <summary>
    /// Performs the post create operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task PostCreateAsync<T>(RetryPolicyContext<T> context)
        where T : class, PipeContext
    {
        return ForEachAsync(x => x.PostCreateAsync(context));
    }

    /// <summary>
    /// Performs the post fault operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task PostFaultAsync<T>(RetryContext<T> context)
        where T : class, PipeContext
    {
        return ForEachAsync(x => x.PostFaultAsync(context));
    }

    /// <summary>
    /// Performs the pre retry operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task PreRetryAsync<T>(RetryContext<T> context)
        where T : class, PipeContext
    {
        return ForEachAsync(x => x.PreRetryAsync(context));
    }

    /// <summary>
    /// Performs the retry fault operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task RetryFaultAsync<T>(RetryContext<T> context)
        where T : class, PipeContext
    {
        return ForEachAsync(x => x.RetryFaultAsync(context));
    }

    /// <summary>
    /// Performs the retry complete operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task RetryCompleteAsync<T>(RetryContext<T> context)
        where T : class, PipeContext
    {
        return ForEachAsync(x => x.RetryCompleteAsync(context));
    }

    /// <summary>
    /// Performs the retry fault operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task RetryFaultAsync(RetryContext context, CancellationToken cancellationToken = default)
    {
        return ForEachAsync(x => RetryFaultObserverCache.RetryFaultAsync(x, context, context.ContextType, cancellationToken: cancellationToken), cancellationToken: cancellationToken);
    }
}
