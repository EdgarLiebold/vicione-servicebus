using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Caches strongly typed fault dispatchers for runtime exception types.</summary>
internal static class ExceptionTypeCache
{
    static CachedConfigurator GetOrAdd(Type type)
    {
        return Cached.Instance.GetOrAdd(type, _ =>
            (CachedConfigurator)(Activator.CreateInstance(typeof(CachedConfigurator<>).MakeGenericType(type))
                ?? throw new InvalidOperationException($"Could not create an exception configurator for '{type}'.")));
    }

    /// <summary>Dispatches an exception through an untyped-event fault behavior.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <param name="behavior">The state-machine behavior to compose or inspect.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes after the typed fault behavior.</returns>
    public static Task FaultedAsync<TSaga>(IBehavior<TSaga> behavior, BehaviorContext<TSaga> context, Exception exception, CancellationToken cancellationToken = default)
        where TSaga : class, SagaStateMachineInstance
    {
        ArgumentNullException.ThrowIfNull(behavior);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(exception);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        return GetOrAdd(exception.GetType()).FaultedAsync(behavior, context, exception);
    }

    /// <summary>Dispatches an exception through a data-event fault behavior.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="behavior">The state-machine behavior to compose or inspect.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes after the typed fault behavior.</returns>
    public static Task FaultedAsync<TSaga, TMessage>(IBehavior<TSaga, TMessage> behavior, BehaviorContext<TSaga, TMessage> context, Exception exception, CancellationToken cancellationToken = default)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(behavior);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(exception);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

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
