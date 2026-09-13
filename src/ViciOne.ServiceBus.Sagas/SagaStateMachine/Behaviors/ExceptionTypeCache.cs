using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Caches strongly typed fault dispatchers for runtime exception types.</summary>
internal static class ExceptionTypeCache
{
    static ICachedConfigurator GetOrAdd(Type type)
    {
        return Cached.Instance.GetOrAdd(type, _ =>
            (ICachedConfigurator)(Activator.CreateInstance(typeof(CachedConfigurator<>).MakeGenericType(type))
                ?? throw new InvalidOperationException($"Could not create an exception configurator for '{type}'.")));
    }

    /// <summary>Dispatches an exception through an untyped-event fault behavior.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <param name="behavior">The state-machine behavior to compose or inspect.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes after the typed fault behavior.</returns>
    public static Task FaultedAsync<TSaga>(IBehavior<TSaga> behavior, IBehaviorContext<TSaga> context, Exception exception, CancellationToken cancellationToken = default)
        where TSaga : class, ISagaStateMachineInstance
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
    public static Task FaultedAsync<TSaga, TMessage>(IBehavior<TSaga, TMessage> behavior, IBehaviorContext<TSaga, TMessage> context, Exception exception, CancellationToken cancellationToken = default)
        where TSaga : class, ISagaStateMachineInstance
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
        internal static readonly ConcurrentDictionary<Type, ICachedConfigurator> Instance = new ConcurrentDictionary<Type, ICachedConfigurator>();
    }


    interface ICachedConfigurator
    {
        Task FaultedAsync<TSaga>(IBehavior<TSaga> behavior, IBehaviorContext<TSaga> context, Exception exception)
            where TSaga : class, ISagaStateMachineInstance;

        Task FaultedAsync<TSaga, TMessage>(IBehavior<TSaga, TMessage> behavior, IBehaviorContext<TSaga, TMessage> context, Exception exception)
            where TSaga : class, ISagaStateMachineInstance
            where TMessage : class;
    }


    class CachedConfigurator<TException> :
        ICachedConfigurator
        where TException : Exception
    {
        Task ICachedConfigurator.FaultedAsync<TInstance>(IBehavior<TInstance> behavior, IBehaviorContext<TInstance> context, Exception exception)
        {
            if (exception is TException typedException)
            {
                var exceptionContext = new ViciOneServiceBusStateMachine<TInstance>.BehaviorExceptionContextProxy<TException>(context, typedException);

                return behavior.FaultedAsync(exceptionContext);
            }

            throw new ArgumentException($"The exception type {exception.GetType().Name} did not match the expected type {typeof(TException).Name}");
        }

        Task ICachedConfigurator.FaultedAsync<TInstance, TData>(IBehavior<TInstance, TData> behavior, IBehaviorContext<TInstance, TData> context,
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
