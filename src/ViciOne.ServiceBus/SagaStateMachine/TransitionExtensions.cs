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
    public static EventActivityBinder<TSaga> TransitionTo<TSaga>(this EventActivityBinder<TSaga> source, State toState)
        where TSaga : class, SagaStateMachineInstance
    {
        State<TSaga> state = source.StateMachine.GetState(toState.Name);

        var activity = new TransitionActivity<TSaga>(state, source.StateMachine.Accessor);

        return source.Add(activity);
    }

    /// <summary>Transition the state machine to the specified state in response to an exception.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="toState">The to state.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static ExceptionActivityBinder<TSaga, TException> TransitionTo<TSaga, TException>(this ExceptionActivityBinder<TSaga, TException> source,
        State toState)
        where TSaga : class, SagaStateMachineInstance
        where TException : Exception
    {
        State<TSaga> state = source.StateMachine.GetState(toState.Name);

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
    public static EventActivityBinder<TSaga, TMessage> TransitionTo<TSaga, TMessage>(this EventActivityBinder<TSaga, TMessage> source, State toState)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
    {
        State<TSaga> state = source.StateMachine.GetState(toState.Name);

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
    public static ExceptionActivityBinder<TSaga, TMessage, TException> TransitionTo<TSaga, TMessage, TException>(
        this ExceptionActivityBinder<TSaga, TMessage, TException> source, State toState)
        where TSaga : class, SagaStateMachineInstance
        where TException : Exception
        where TMessage : class
    {
        State<TSaga> state = source.StateMachine.GetState(toState.Name);

        var activity = new TransitionActivity<TSaga>(state, source.StateMachine.Accessor);

        var compensateActivity = new ExecuteOnFaultedActivity<TSaga>(activity);

        return source.Add(compensateActivity);
    }

    /// <summary>Transition the state machine to the Final state.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<TSaga, TMessage> Finalize<TSaga, TMessage>(this EventActivityBinder<TSaga, TMessage> source)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
    {
        State<TSaga> state = source.StateMachine.GetState(source.StateMachine.Final.Name);

        var activity = new TransitionActivity<TSaga>(state, source.StateMachine.Accessor);

        return source.Add(activity);
    }

    /// <summary>Transition the state machine to the Final state.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<TSaga> Finalize<TSaga>(this EventActivityBinder<TSaga> source)
        where TSaga : class, SagaStateMachineInstance
    {
        State<TSaga> state = source.StateMachine.GetState(source.StateMachine.Final.Name);

        var activity = new TransitionActivity<TSaga>(state, source.StateMachine.Accessor);

        return source.Add(activity);
    }

    /// <summary>Transition the state machine to the Final state.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static ExceptionActivityBinder<TSaga, TMessage, TException> Finalize<TSaga, TMessage, TException>(
        this ExceptionActivityBinder<TSaga, TMessage, TException> source)
        where TSaga : class, SagaStateMachineInstance
        where TException : Exception
        where TMessage : class
    {
        State<TSaga> state = source.StateMachine.GetState(source.StateMachine.Final.Name);

        var activity = new TransitionActivity<TSaga>(state, source.StateMachine.Accessor);

        var compensateActivity = new ExecuteOnFaultedActivity<TSaga>(activity);

        return source.Add(compensateActivity);
    }

    /// <summary>Transition the state machine to the Final state.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static ExceptionActivityBinder<TSaga, TException> Finalize<TSaga, TException>(this ExceptionActivityBinder<TSaga, TException> source)
        where TSaga : class, SagaStateMachineInstance
        where TException : Exception
    {
        State<TSaga> state = source.StateMachine.GetState(source.StateMachine.Final.Name);

        var activity = new TransitionActivity<TSaga>(state, source.StateMachine.Accessor);

        var compensateActivity = new ExecuteOnFaultedActivity<TSaga>(activity);

        return source.Add(compensateActivity);
    }
}
