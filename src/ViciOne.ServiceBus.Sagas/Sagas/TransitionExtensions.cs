using System;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Provides extension methods for transition.</summary>
public static class TransitionExtensions
{
    /// <summary>Transition the state machine to the specified state.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="toState">The to state.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TSaga> TransitionTo<TSaga>(this IEventActivityBinder<TSaga> source, IState toState)
        where TSaga : class, ISagaStateMachineInstance
    {
        IState<TSaga> state = source.StateMachine.GetState(toState.Name);

        var activity = new TransitionActivity<TSaga>(state, source.StateMachine.Accessor);

        return source.Add(activity);
    }

    /// <summary>Transition the state machine to the specified state in response to an exception.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="toState">The to state.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TSaga, TException> TransitionTo<TSaga, TException>(this IExceptionActivityBinder<TSaga, TException> source,
        IState toState)
        where TSaga : class, ISagaStateMachineInstance
        where TException : Exception
    {
        IState<TSaga> state = source.StateMachine.GetState(toState.Name);

        var activity = new TransitionActivity<TSaga>(state, source.StateMachine.Accessor);

        var compensateActivity = new ExecuteOnFaultedActivity<TSaga>(activity);

        return source.Add(compensateActivity);
    }

    /// <summary>Transition the state machine to the specified state.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="toState">The to state.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TSaga, TMessage> TransitionTo<TSaga, TMessage>(this IEventActivityBinder<TSaga, TMessage> source, IState toState)
        where TSaga : class, ISagaStateMachineInstance
        where TMessage : class
    {
        IState<TSaga> state = source.StateMachine.GetState(toState.Name);

        var activity = new TransitionActivity<TSaga>(state, source.StateMachine.Accessor);

        return source.Add(activity);
    }

    /// <summary>Transition the state machine to the specified state in response to an exception.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="toState">The to state.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TSaga, TMessage, TException> TransitionTo<TSaga, TMessage, TException>(
        this IExceptionActivityBinder<TSaga, TMessage, TException> source, IState toState)
        where TSaga : class, ISagaStateMachineInstance
        where TException : Exception
        where TMessage : class
    {
        IState<TSaga> state = source.StateMachine.GetState(toState.Name);

        var activity = new TransitionActivity<TSaga>(state, source.StateMachine.Accessor);

        var compensateActivity = new ExecuteOnFaultedActivity<TSaga>(activity);

        return source.Add(compensateActivity);
    }

    /// <summary>Transition the state machine to the Final state.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TSaga, TMessage> Finalize<TSaga, TMessage>(this IEventActivityBinder<TSaga, TMessage> source)
        where TSaga : class, ISagaStateMachineInstance
        where TMessage : class
    {
        IState<TSaga> state = source.StateMachine.GetState(source.StateMachine.Final.Name);

        var activity = new TransitionActivity<TSaga>(state, source.StateMachine.Accessor);

        return source.Add(activity);
    }

    /// <summary>Transition the state machine to the Final state.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TSaga> Finalize<TSaga>(this IEventActivityBinder<TSaga> source)
        where TSaga : class, ISagaStateMachineInstance
    {
        IState<TSaga> state = source.StateMachine.GetState(source.StateMachine.Final.Name);

        var activity = new TransitionActivity<TSaga>(state, source.StateMachine.Accessor);

        return source.Add(activity);
    }

    /// <summary>Transition the state machine to the Final state.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TSaga, TMessage, TException> Finalize<TSaga, TMessage, TException>(
        this IExceptionActivityBinder<TSaga, TMessage, TException> source)
        where TSaga : class, ISagaStateMachineInstance
        where TException : Exception
        where TMessage : class
    {
        IState<TSaga> state = source.StateMachine.GetState(source.StateMachine.Final.Name);

        var activity = new TransitionActivity<TSaga>(state, source.StateMachine.Accessor);

        var compensateActivity = new ExecuteOnFaultedActivity<TSaga>(activity);

        return source.Add(compensateActivity);
    }

    /// <summary>Transition the state machine to the Final state.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TSaga, TException> Finalize<TSaga, TException>(this IExceptionActivityBinder<TSaga, TException> source)
        where TSaga : class, ISagaStateMachineInstance
        where TException : Exception
    {
        IState<TSaga> state = source.StateMachine.GetState(source.StateMachine.Final.Name);

        var activity = new TransitionActivity<TSaga>(state, source.StateMachine.Accessor);

        var compensateActivity = new ExecuteOnFaultedActivity<TSaga>(activity);

        return source.Add(compensateActivity);
    }
}
