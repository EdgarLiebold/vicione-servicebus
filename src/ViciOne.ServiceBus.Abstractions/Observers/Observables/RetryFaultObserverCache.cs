using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Observables;

public class RetryFaultObserverCache
{
    readonly ConcurrentDictionary<Type, Lazy<IRetryFaultObserver>> _types = new ConcurrentDictionary<Type, Lazy<IRetryFaultObserver>>();

    IRetryFaultObserver this[Type type] => _types.GetOrAdd(type, CreateTypeConverter).Value;

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
