using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Observables;

/// <summary>Dispatches untyped terminal retry notifications through cached typed adapters.</summary>
internal sealed class RetryFaultObserverCache
{
    readonly ConcurrentDictionary<Type, Lazy<IRetryFaultObserver>> _types = new ConcurrentDictionary<Type, Lazy<IRetryFaultObserver>>();

    IRetryFaultObserver this[Type type] => _types.GetOrAdd(type, CreateTypeConverter).Value;

    /// <summary>Notifies an observer through the adapter for the context's runtime pipeline type.</summary>
    /// <param name="observer">The observer to notify.</param>
    /// <param name="context">The terminal retry state.</param>
    /// <param name="contextType">The runtime pipeline context type.</param>
    /// <param name="cancellationToken">The token that cancels notification.</param>
    /// <returns>A task that completes after the observer callback.</returns>
    internal static Task RetryFaultAsync(IRetryObserver observer, RetryContext context, Type contextType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(observer);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(contextType);

        return cancellationToken.IsCancellationRequested
            ? Task.FromCanceled(cancellationToken)
            : Cached.Converters.Value[contextType].RetryFaultAsync(observer, context);
    }

    static Lazy<IRetryFaultObserver> CreateTypeConverter(Type type)
    {
        return new Lazy<IRetryFaultObserver>(() => CreateConverter(type));
    }

    static IRetryFaultObserver CreateConverter(Type type)
    {
        var converterType = typeof(RetryFaultObserver<>).MakeGenericType(type);

        return Activator.CreateInstance(converterType) as IRetryFaultObserver
            ?? throw new InvalidOperationException($"A retry-fault observer adapter could not be created for {type}.");
    }


    interface IRetryFaultObserver
    {
        Task RetryFaultAsync(IRetryObserver observer, RetryContext context);
    }


    class RetryFaultObserver<T> :
        IRetryFaultObserver
        where T : class, PipeContext
    {
        public Task RetryFaultAsync(IRetryObserver observer, RetryContext context)
        {
            ArgumentNullException.ThrowIfNull(observer);
            ArgumentNullException.ThrowIfNull(context);

            if (context is not RetryContext<T> typedContext)
            {
                throw new ArgumentException(
                    $"The retry context does not implement RetryContext<{typeof(T).Name}>.", nameof(context));
            }

            return observer.RetryFaultAsync(typedContext)
                ?? Task.FromException(new InvalidOperationException("The retry observer returned a null task."));
        }
    }


    static class Cached
    {
        internal static readonly Lazy<RetryFaultObserverCache> Converters = new(() => new RetryFaultObserverCache());
    }
}
