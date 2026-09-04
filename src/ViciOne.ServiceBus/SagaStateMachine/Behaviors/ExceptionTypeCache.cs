using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

public static class ExceptionTypeCache
{
    static CachedConfigurator GetOrAdd(Type type)
    {
        return Cached.Instance.GetOrAdd(type, _ =>
            (CachedConfigurator)(Activator.CreateInstance(typeof(CachedConfigurator<>).MakeGenericType(type))
                ?? throw new InvalidOperationException($"Could not create an exception configurator for '{type}'.")));
    }

    public static Task FaultedAsync<TSaga>(IBehavior<TSaga> behavior, BehaviorContext<TSaga> context, Exception exception, CancellationToken cancellationToken = default)
        where TSaga : class, SagaStateMachineInstance
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); if (exception == null)
            throw new ArgumentNullException(nameof(exception));

        return GetOrAdd(exception.GetType()).FaultedAsync(behavior, context, exception);
    }

    public static Task FaultedAsync<TSaga, TMessage>(IBehavior<TSaga, TMessage> behavior, BehaviorContext<TSaga, TMessage> context, Exception exception, CancellationToken cancellationToken = default)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); if (exception == null)
            throw new ArgumentNullException(nameof(exception));

        return GetOrAdd(exception.GetType()).FaultedAsync(behavior, context, exception);
    }


    static class Cached
    {
        internal static readonly ConcurrentDictionary<Type, CachedConfigurator> Instance = new ConcurrentDictionary<Type, CachedConfigurator>();
    }


    interface CachedConfigurator
    {
        Task FaultedAsync<TSaga>(IBehavior<TSaga> behavior, BehaviorContext<TSaga> context, Exception exception)
            where TSaga : class, SagaStateMachineInstance;

        Task FaultedAsync<TSaga, TMessage>(IBehavior<TSaga, TMessage> behavior, BehaviorContext<TSaga, TMessage> context, Exception exception)
            where TSaga : class, SagaStateMachineInstance
            where TMessage : class;
    }


    class CachedConfigurator<TException> :
        CachedConfigurator
        where TException : Exception
    {
        Task CachedConfigurator.FaultedAsync<TInstance>(IBehavior<TInstance> behavior, BehaviorContext<TInstance> context, Exception exception)
        {
            if (exception is TException typedException)
            {
                var exceptionContext = new ViciOneServiceBusStateMachine<TInstance>.BehaviorExceptionContextProxy<TException>(context, typedException);

                return behavior.FaultedAsync(exceptionContext);
            }

            throw new ArgumentException($"The exception type {exception.GetType().Name} did not match the expected type {typeof(TException).Name}");
        }

        Task CachedConfigurator.FaultedAsync<TInstance, TData>(IBehavior<TInstance, TData> behavior, BehaviorContext<TInstance, TData> context,
            Exception exception)
        {
            if (exception is TException typedException)
            {
                var exceptionContext = new ViciOneServiceBusStateMachine<TInstance>.BehaviorExceptionContextProxy<TData, TException>(context, typedException);

                return behavior.FaultedAsync(exceptionContext);
            }

            throw new ArgumentException($"The exception type {exception.GetType().Name} did not match the expected type {typeof(TException).Name}");
        }
    }
}
