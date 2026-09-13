using System;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Provides extension methods for container activity.</summary>
public static class ContainerActivityExtensions
{
    /// <summary>Adds an activity to the state machine that is resolved from the container, rather than being initialized directly.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <param name="binder">The binder.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance, TData> Activity<TInstance, TData>(this IEventActivityBinder<TInstance, TData> binder,
        Func<IStateMachineActivitySelector<TInstance, TData>, IEventActivityBinder<TInstance, TData>> configure)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
    {
        var selector = new StateMachineActivitySelector<TInstance, TData>(binder);

        return configure(selector);
    }

    /// <summary>Adds an activity to the state machine that is resolved from the container, rather than being initialized directly.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <param name="binder">The binder.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance> Activity<TInstance>(this IEventActivityBinder<TInstance> binder,
        Func<IStateMachineActivitySelector<TInstance>, IEventActivityBinder<TInstance>> configure)
        where TInstance : class, ISagaStateMachineInstance
    {
        var selector = new StateMachineActivitySelector<TInstance>(binder);

        return configure(selector);
    }

    /// <summary>Adds an activity to the state machine that is resolved from the container, but only handles Faulted behaviors.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="binder">The binder.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TException> Activity<TInstance, TException>(this IExceptionActivityBinder<TInstance, TException> binder,
        Func<IStateMachineFaultedActivitySelector<TInstance, TException>, IExceptionActivityBinder<TInstance, TException>> configure)
        where TInstance : class, ISagaStateMachineInstance
        where TException : Exception
    {
        var selector = new StateMachineFaultedActivitySelector<TInstance, TException>(binder);

        return configure(selector);
    }

    /// <summary>Adds an activity to the state machine that is resolved from the container, but only handles Faulted behaviors.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="binder">The binder.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TMessage, TException> Activity<TInstance, TMessage, TException>(
        this IExceptionActivityBinder<TInstance, TMessage, TException> binder,
        Func<IStateMachineFaultedActivitySelector<TInstance, TMessage, TException>, IExceptionActivityBinder<TInstance, TMessage, TException>> configure)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        var selector = new StateMachineFaultedActivitySelector<TInstance, TMessage, TException>(binder);

        return configure(selector);
    }
}
