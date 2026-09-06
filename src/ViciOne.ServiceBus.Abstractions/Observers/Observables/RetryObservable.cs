using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Observables;

/// <summary>Publishes observations for retry.</summary>
public class RetryObservable :
    Connectable<IRetryObserver>,
    IRetryObserver
{
    /// <summary>Runs after create.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PostCreateAsync<T>(RetryPolicyContext<T> context)
        where T : class, PipeContext
    {
        return ForEachAsync(x => x.PostCreateAsync(context));
    }

    /// <summary>Runs after fault.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PostFaultAsync<T>(RetryContext<T> context)
        where T : class, PipeContext
    {
        return ForEachAsync(x => x.PostFaultAsync(context));
    }

    /// <summary>Runs before retry.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PreRetryAsync<T>(RetryContext<T> context)
        where T : class, PipeContext
    {
        return ForEachAsync(x => x.PreRetryAsync(context));
    }

    /// <summary>Retries fault.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task RetryFaultAsync<T>(RetryContext<T> context)
        where T : class, PipeContext
    {
        return ForEachAsync(x => x.RetryFaultAsync(context));
    }

    /// <summary>Retries complete.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task RetryCompleteAsync<T>(RetryContext<T> context)
        where T : class, PipeContext
    {
        return ForEachAsync(x => x.RetryCompleteAsync(context));
    }

    /// <summary>Retries fault.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task RetryFaultAsync(RetryContext context, CancellationToken cancellationToken = default)
    {
        return ForEachAsync(x => RetryFaultObserverCache.RetryFaultAsync(x, context, context.ContextType, cancellationToken: cancellationToken), cancellationToken: cancellationToken);
    }
}
