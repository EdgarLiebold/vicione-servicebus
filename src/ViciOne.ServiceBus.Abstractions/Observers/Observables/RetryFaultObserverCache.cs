using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Observables;

/// <summary>
/// Provides a retry fault observer cache implementation.
/// </summary>
public class RetryFaultObserverCache
{
    readonly ConcurrentDictionary<Type, Lazy<IRetryFaultObserver>> _types = new ConcurrentDictionary<Type, Lazy<IRetryFaultObserver>>();

    IRetryFaultObserver this[Type type] => _types.GetOrAdd(type, CreateTypeConverter).Value;

    /// <summary>
    /// Performs the retry fault operation.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="contextType">The context type value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task RetryFaultAsync(IRetryObserver observer, RetryContext context, Type contextType, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Cached.Converters.Value[contextType].RetryFaultAsync(observer, context);
    }

    static Lazy<IRetryFaultObserver> CreateTypeConverter(Type type)
    {
        return new Lazy<IRetryFaultObserver>(() => CreateConverter(type));
    }

    static IRetryFaultObserver CreateConverter(Type type)
    {
        var converterType = typeof(RetryFaultObserver<>).MakeGenericType(type);

        return Activator.CreateInstance(converterType) as IRetryFaultObserver
            ?? throw new InvalidOperationException("Failed to create Retry Fault Observer");
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
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            return observer.RetryFaultAsync((RetryContext<T>)context);
        }
    }


    static class Cached
    {
        internal static readonly Lazy<RetryFaultObserverCache> Converters = new Lazy<RetryFaultObserverCache>(() => new RetryFaultObserverCache());
    }
}
