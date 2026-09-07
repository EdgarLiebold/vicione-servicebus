using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Defines the operations required by event activity binder.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface EventActivityBinder<TSaga> :
    EventActivities<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    /// <summary>Gets the state machine.</summary>
    StateMachine<TSaga> StateMachine { get; }

    /// <summary>Gets the event.</summary>
    Event Event { get; }

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="activity">The activity.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    EventActivityBinder<TSaga> Add(IStateMachineActivity<TSaga> activity);

    /// <summary>Catch the exception of type T, and execute the compensation chain.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="activityCallback">The activity callback.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    EventActivityBinder<TSaga> Catch<T>(Func<ExceptionActivityBinder<TSaga, T>, ExceptionActivityBinder<TSaga, T>> activityCallback)
        where T : Exception;

    /// <summary>Retry the behavior, using the specified retry policy.</summary>
    /// <param name="configure">Configures the retry.</param>
    /// <param name="activityCallback">The activity callback.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    EventActivityBinder<TSaga> Retry(Action<IRetryConfigurator> configure,
        Func<EventActivityBinder<TSaga>, EventActivityBinder<TSaga>> activityCallback);

    /// <summary>Create a conditional branch of activities for processing.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="activityCallback">The activity callback.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    EventActivityBinder<TSaga> If(StateMachineCondition<TSaga> condition,
        Func<EventActivityBinder<TSaga>, EventActivityBinder<TSaga>> activityCallback);

    /// <summary>Create a conditional branch of activities for processing.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="activityCallback">The activity callback.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    EventActivityBinder<TSaga> IfAwaited(StateMachineAsyncCondition<TSaga> condition,
        Func<EventActivityBinder<TSaga>, EventActivityBinder<TSaga>> activityCallback);

    /// <summary>Create a conditional branch of activities for processing.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="thenActivityCallback">The then activity callback.</param>
    /// <param name="elseActivityCallback">The else activity callback.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    EventActivityBinder<TSaga> IfElse(StateMachineCondition<TSaga> condition,
        Func<EventActivityBinder<TSaga>, EventActivityBinder<TSaga>> thenActivityCallback,
        Func<EventActivityBinder<TSaga>, EventActivityBinder<TSaga>> elseActivityCallback);

    /// <summary>Create a conditional branch of activities for processing.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="thenActivityCallback">The then activity callback.</param>
    /// <param name="elseActivityCallback">The else activity callback.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    EventActivityBinder<TSaga> IfElseAwaited(StateMachineAsyncCondition<TSaga> condition,
        Func<EventActivityBinder<TSaga>, EventActivityBinder<TSaga>> thenActivityCallback,
        Func<EventActivityBinder<TSaga>, EventActivityBinder<TSaga>> elseActivityCallback);
}


/// <summary>Defines the operations required by event activity binder.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface EventActivityBinder<TSaga, TMessage> :
    EventActivities<TSaga>
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
{
    /// <summary>Gets the state machine.</summary>
    StateMachine<TSaga> StateMachine { get; }

    /// <summary>Gets the event.</summary>
    Event<TMessage> Event { get; }

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="activity">The activity.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    EventActivityBinder<TSaga, TMessage> Add(IStateMachineActivity<TSaga> activity);

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="activity">The activity.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    EventActivityBinder<TSaga, TMessage> Add(IStateMachineActivity<TSaga, TMessage> activity);

    /// <summary>Catch the exception of type T, and execute the compensation chain.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="activityCallback">The activity callback.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    EventActivityBinder<TSaga, TMessage> Catch<T>(
        Func<ExceptionActivityBinder<TSaga, TMessage, T>, ExceptionActivityBinder<TSaga, TMessage, T>> activityCallback)
        where T : Exception;

    /// <summary>Retry the behavior, using the specified retry policy.</summary>
    /// <param name="configure">Configures the retry.</param>
    /// <param name="activityCallback">The activity callback.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    EventActivityBinder<TSaga, TMessage> Retry(Action<IRetryConfigurator> configure,
        Func<EventActivityBinder<TSaga, TMessage>, EventActivityBinder<TSaga, TMessage>> activityCallback);

    /// <summary>Create a conditional branch of activities for processing.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="activityCallback">The activity callback.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    EventActivityBinder<TSaga, TMessage> If(StateMachineCondition<TSaga, TMessage> condition,
        Func<EventActivityBinder<TSaga, TMessage>, EventActivityBinder<TSaga, TMessage>> activityCallback);

    /// <summary>Create a conditional branch of activities for processing.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="activityCallback">The activity callback.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    EventActivityBinder<TSaga, TMessage> IfAwaited(StateMachineAsyncCondition<TSaga, TMessage> condition,
        Func<EventActivityBinder<TSaga, TMessage>, EventActivityBinder<TSaga, TMessage>> activityCallback);

    /// <summary>Create a conditional branch of activities for processing.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="thenActivityCallback">The then activity callback.</param>
    /// <param name="elseActivityCallback">The else activity callback.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    EventActivityBinder<TSaga, TMessage> IfElse(StateMachineCondition<TSaga, TMessage> condition,
        Func<EventActivityBinder<TSaga, TMessage>, EventActivityBinder<TSaga, TMessage>> thenActivityCallback,
        Func<EventActivityBinder<TSaga, TMessage>, EventActivityBinder<TSaga, TMessage>> elseActivityCallback);

    /// <summary>Create a conditional branch of activities for processing.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="thenActivityCallback">The then activity callback.</param>
    /// <param name="elseActivityCallback">The else activity callback.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    EventActivityBinder<TSaga, TMessage> IfElseAwaited(StateMachineAsyncCondition<TSaga, TMessage> condition,
        Func<EventActivityBinder<TSaga, TMessage>, EventActivityBinder<TSaga, TMessage>> thenActivityCallback,
        Func<EventActivityBinder<TSaga, TMessage>, EventActivityBinder<TSaga, TMessage>> elseActivityCallback);
}
