using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Observables;

/// <summary>Publishes retry lifecycle notifications to a stable observer snapshot.</summary>
internal sealed class RetryObservable :
    Connectable<IRetryObserver>,
    IRetryObserver
{
    /// <summary>Notifies observers after an operation acquires retry-policy state.</summary>
    /// <typeparam name="T">The pipeline context type.</typeparam>
    /// <param name="context">The operation's retry-policy state.</param>
    /// <returns>A task that completes after every observer callback.</returns>
    public Task PostCreateAsync<T>(RetryPolicyContext<T> context)
        where T : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(context);
        return ForEachAsync(observer => observer.PostCreateAsync(context));
    }

    /// <summary>Notifies observers that a handled failure will be retried.</summary>
    /// <typeparam name="T">The pipeline context type.</typeparam>
    /// <param name="context">The scheduled retry state.</param>
    /// <returns>A task that completes after every observer callback.</returns>
    public Task PostFaultAsync<T>(RetryContext<T> context)
        where T : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(context);
        return ForEachAsync(observer => observer.PostFaultAsync(context));
    }

    /// <summary>Notifies observers immediately before a retry attempt begins.</summary>
    /// <typeparam name="T">The pipeline context type.</typeparam>
    /// <param name="context">The retry state for the attempt.</param>
    /// <returns>A task that completes after every observer callback.</returns>
    public Task PreRetryAsync<T>(RetryContext<T> context)
        where T : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(context);
        return ForEachAsync(observer => observer.PreRetryAsync(context));
    }

    /// <summary>Notifies observers that the retry policy reached a terminal failure.</summary>
    /// <typeparam name="T">The pipeline context type.</typeparam>
    /// <param name="context">The terminal retry state.</param>
    /// <returns>A task that completes after every observer callback.</returns>
    public Task RetryFaultAsync<T>(RetryContext<T> context)
        where T : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(context);
        return ForEachAsync(observer => observer.RetryFaultAsync(context));
    }

    /// <summary>Notifies observers that a retry attempt completed successfully.</summary>
    /// <typeparam name="T">The pipeline context type.</typeparam>
    /// <param name="context">The successful retry state.</param>
    /// <returns>A task that completes after every observer callback.</returns>
    public Task RetryCompleteAsync<T>(RetryContext<T> context)
        where T : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(context);
        return ForEachAsync(observer => observer.RetryCompleteAsync(context));
    }

    /// <summary>Notifies observers of terminal state when only its runtime context type is known.</summary>
    /// <param name="context">The terminal retry state.</param>
    /// <param name="cancellationToken">The token that cancels notification.</param>
    /// <returns>A task that completes after every observer callback.</returns>
    public Task RetryFaultAsync(RetryContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return ForEachAsync(
            observer => RetryFaultObserverCache.RetryFaultAsync(observer, context, context.ContextType, cancellationToken),
            cancellationToken);
    }
}
