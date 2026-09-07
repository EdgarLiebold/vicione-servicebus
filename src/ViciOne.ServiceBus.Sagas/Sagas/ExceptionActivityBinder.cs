using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Defines the operations required by exception activity binder.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TException">The exception handled by the member.</typeparam>
public interface ExceptionActivityBinder<TSaga, TException> :
    EventActivities<TSaga>
    where TSaga : class, SagaStateMachineInstance
    where TException : Exception
{
    /// <summary>Gets the state machine.</summary>
    StateMachine<TSaga> StateMachine { get; }

    /// <summary>Gets the event.</summary>
    Event Event { get; }

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="activity">The activity.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    ExceptionActivityBinder<TSaga, TException> Add(IStateMachineActivity<TSaga> activity);

    /// <summary>Catch an exception and execute the compensating activities.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="activityCallback">The activity callback.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    ExceptionActivityBinder<TSaga, TException> Catch<T>(Func<ExceptionActivityBinder<TSaga, T>, ExceptionActivityBinder<TSaga, T>> activityCallback)
        where T : Exception;

    /// <summary>Create a conditional branch of activities for processing.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="activityCallback">The activity callback.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    ExceptionActivityBinder<TSaga, TException> If(StateMachineExceptionCondition<TSaga, TException> condition,
        Func<ExceptionActivityBinder<TSaga, TException>, ExceptionActivityBinder<TSaga, TException>> activityCallback);

    /// <summary>Create a conditional branch of activities for processing.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="activityCallback">The activity callback.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    ExceptionActivityBinder<TSaga, TException> IfAwaited(StateMachineAsyncExceptionCondition<TSaga, TException> condition,
        Func<ExceptionActivityBinder<TSaga, TException>, ExceptionActivityBinder<TSaga, TException>> activityCallback);

    /// <summary>Create a conditional branch of activities for processing.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="thenActivityCallback">The then activity callback.</param>
    /// <param name="elseActivityCallback">The else activity callback.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    ExceptionActivityBinder<TSaga, TException> IfElse(StateMachineExceptionCondition<TSaga, TException> condition,
        Func<ExceptionActivityBinder<TSaga, TException>, ExceptionActivityBinder<TSaga, TException>> thenActivityCallback,
        Func<ExceptionActivityBinder<TSaga, TException>, ExceptionActivityBinder<TSaga, TException>> elseActivityCallback);

    /// <summary>Create a conditional branch of activities for processing.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="thenActivityCallback">The then activity callback.</param>
    /// <param name="elseActivityCallback">The else activity callback.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    ExceptionActivityBinder<TSaga, TException> IfElseAwaited(StateMachineAsyncExceptionCondition<TSaga, TException> condition,
        Func<ExceptionActivityBinder<TSaga, TException>, ExceptionActivityBinder<TSaga, TException>> thenActivityCallback,
        Func<ExceptionActivityBinder<TSaga, TException>, ExceptionActivityBinder<TSaga, TException>> elseActivityCallback);
}


/// <summary>Defines the operations required by exception activity binder.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="TException">The exception handled by the member.</typeparam>
public interface ExceptionActivityBinder<TSaga, TMessage, TException> :
    EventActivities<TSaga>
    where TSaga : class, SagaStateMachineInstance
    where TException : Exception
    where TMessage : class
{
    /// <summary>Gets the state machine.</summary>
    StateMachine<TSaga> StateMachine { get; }

    /// <summary>Gets the event.</summary>
    Event<TMessage> Event { get; }

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="activity">The activity.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    ExceptionActivityBinder<TSaga, TMessage, TException> Add(IStateMachineActivity<TSaga> activity);

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="activity">The activity.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    ExceptionActivityBinder<TSaga, TMessage, TException> Add(IStateMachineActivity<TSaga, TMessage> activity);

    /// <summary>Catch an exception and execute the compensating activities.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="activityCallback">The activity callback.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    ExceptionActivityBinder<TSaga, TMessage, TException> Catch<T>(
        Func<ExceptionActivityBinder<TSaga, TMessage, T>, ExceptionActivityBinder<TSaga, TMessage, T>> activityCallback)
        where T : Exception;

    /// <summary>Create a conditional branch of activities for processing.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="activityCallback">The activity callback.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    ExceptionActivityBinder<TSaga, TMessage, TException> If(StateMachineExceptionCondition<TSaga, TMessage, TException> condition,
        Func<ExceptionActivityBinder<TSaga, TMessage, TException>, ExceptionActivityBinder<TSaga, TMessage, TException>> activityCallback);

    /// <summary>Create a conditional branch of activities for processing.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="activityCallback">The activity callback.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    ExceptionActivityBinder<TSaga, TMessage, TException> IfAwaited(StateMachineAsyncExceptionCondition<TSaga, TMessage, TException> condition,
        Func<ExceptionActivityBinder<TSaga, TMessage, TException>, ExceptionActivityBinder<TSaga, TMessage, TException>> activityCallback);

    /// <summary>Create a conditional branch of activities for processing.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="thenActivityCallback">The then activity callback.</param>
    /// <param name="elseActivityCallback">The else activity callback.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    ExceptionActivityBinder<TSaga, TMessage, TException> IfElse(StateMachineExceptionCondition<TSaga, TMessage, TException> condition,
        Func<ExceptionActivityBinder<TSaga, TMessage, TException>, ExceptionActivityBinder<TSaga, TMessage, TException>> thenActivityCallback,
        Func<ExceptionActivityBinder<TSaga, TMessage, TException>, ExceptionActivityBinder<TSaga, TMessage, TException>> elseActivityCallback);

    /// <summary>Create a conditional branch of activities for processing.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="thenActivityCallback">The then activity callback.</param>
    /// <param name="elseActivityCallback">The else activity callback.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    ExceptionActivityBinder<TSaga, TMessage, TException> IfElseAwaited(StateMachineAsyncExceptionCondition<TSaga, TMessage, TException> condition,
        Func<ExceptionActivityBinder<TSaga, TMessage, TException>, ExceptionActivityBinder<TSaga, TMessage, TException>> thenActivityCallback,
        Func<ExceptionActivityBinder<TSaga, TMessage, TException>, ExceptionActivityBinder<TSaga, TMessage, TException>> elseActivityCallback);
}
