using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// Defines the contract for event activity binder.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public interface EventActivityBinder<TSaga> :
    EventActivities<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    /// <summary>
    /// Gets the state machine value.
    /// </summary>
    StateMachine<TSaga> StateMachine { get; }

    /// <summary>
    /// Gets the event value.
    /// </summary>
    Event Event { get; }

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="activity">The activity value.</param>
    /// <returns>The result of the operation.</returns>
    EventActivityBinder<TSaga> Add(IStateMachineActivity<TSaga> activity);

    /// <summary>
    /// Catch the exception of type T, and execute the compensation chain
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="activityCallback"></param>
    /// <returns></returns>
    EventActivityBinder<TSaga> Catch<T>(Func<ExceptionActivityBinder<TSaga, T>, ExceptionActivityBinder<TSaga, T>> activityCallback)
        where T : Exception;

    /// <summary>
    /// Retry the behavior, using the specified retry policy
    /// </summary>
    /// <param name="configure">Configures the retry</param>
    /// <param name="activityCallback"></param>
    /// <returns></returns>
    EventActivityBinder<TSaga> Retry(Action<IRetryConfigurator> configure,
        Func<EventActivityBinder<TSaga>, EventActivityBinder<TSaga>> activityCallback);

    /// <summary>
    /// Create a conditional branch of activities for processing
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="activityCallback"></param>
    /// <returns></returns>
    EventActivityBinder<TSaga> If(StateMachineCondition<TSaga> condition,
        Func<EventActivityBinder<TSaga>, EventActivityBinder<TSaga>> activityCallback);

    /// <summary>
    /// Create a conditional branch of activities for processing
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="activityCallback"></param>
    /// <returns></returns>
    EventActivityBinder<TSaga> IfAsync(StateMachineAsyncCondition<TSaga> condition,
        Func<EventActivityBinder<TSaga>, EventActivityBinder<TSaga>> activityCallback);

    /// <summary>
    /// Create a conditional branch of activities for processing
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="thenActivityCallback"></param>
    /// <param name="elseActivityCallback"></param>
    /// <returns></returns>
    EventActivityBinder<TSaga> IfElse(StateMachineCondition<TSaga> condition,
        Func<EventActivityBinder<TSaga>, EventActivityBinder<TSaga>> thenActivityCallback,
        Func<EventActivityBinder<TSaga>, EventActivityBinder<TSaga>> elseActivityCallback);

    /// <summary>
    /// Create a conditional branch of activities for processing
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="thenActivityCallback"></param>
    /// <param name="elseActivityCallback"></param>
    /// <returns></returns>
    EventActivityBinder<TSaga> IfElseAsync(StateMachineAsyncCondition<TSaga> condition,
        Func<EventActivityBinder<TSaga>, EventActivityBinder<TSaga>> thenActivityCallback,
        Func<EventActivityBinder<TSaga>, EventActivityBinder<TSaga>> elseActivityCallback);
}


/// <summary>
/// Defines the contract for event activity binder.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface EventActivityBinder<TSaga, TMessage> :
    EventActivities<TSaga>
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
{
    /// <summary>
    /// Gets the state machine value.
    /// </summary>
    StateMachine<TSaga> StateMachine { get; }

    /// <summary>
    /// Gets the event value.
    /// </summary>
    Event<TMessage> Event { get; }

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="activity">The activity value.</param>
    /// <returns>The result of the operation.</returns>
    EventActivityBinder<TSaga, TMessage> Add(IStateMachineActivity<TSaga> activity);

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="activity">The activity value.</param>
    /// <returns>The result of the operation.</returns>
    EventActivityBinder<TSaga, TMessage> Add(IStateMachineActivity<TSaga, TMessage> activity);

    /// <summary>
    /// Catch the exception of type T, and execute the compensation chain
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="activityCallback"></param>
    /// <returns></returns>
    EventActivityBinder<TSaga, TMessage> Catch<T>(
        Func<ExceptionActivityBinder<TSaga, TMessage, T>, ExceptionActivityBinder<TSaga, TMessage, T>> activityCallback)
        where T : Exception;

    /// <summary>
    /// Retry the behavior, using the specified retry policy
    /// </summary>
    /// <param name="configure">Configures the retry</param>
    /// <param name="activityCallback"></param>
    /// <returns></returns>
    EventActivityBinder<TSaga, TMessage> Retry(Action<IRetryConfigurator> configure,
        Func<EventActivityBinder<TSaga, TMessage>, EventActivityBinder<TSaga, TMessage>> activityCallback);

    /// <summary>
    /// Create a conditional branch of activities for processing
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="activityCallback"></param>
    /// <returns></returns>
    EventActivityBinder<TSaga, TMessage> If(StateMachineCondition<TSaga, TMessage> condition,
        Func<EventActivityBinder<TSaga, TMessage>, EventActivityBinder<TSaga, TMessage>> activityCallback);

    /// <summary>
    /// Create a conditional branch of activities for processing
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="activityCallback"></param>
    /// <returns></returns>
    EventActivityBinder<TSaga, TMessage> IfAsync(StateMachineAsyncCondition<TSaga, TMessage> condition,
        Func<EventActivityBinder<TSaga, TMessage>, EventActivityBinder<TSaga, TMessage>> activityCallback);

    /// <summary>
    /// Create a conditional branch of activities for processing
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="thenActivityCallback"></param>
    /// <param name="elseActivityCallback"></param>
    /// <returns></returns>
    EventActivityBinder<TSaga, TMessage> IfElse(StateMachineCondition<TSaga, TMessage> condition,
        Func<EventActivityBinder<TSaga, TMessage>, EventActivityBinder<TSaga, TMessage>> thenActivityCallback,
        Func<EventActivityBinder<TSaga, TMessage>, EventActivityBinder<TSaga, TMessage>> elseActivityCallback);

    /// <summary>
    /// Create a conditional branch of activities for processing
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="thenActivityCallback"></param>
    /// <param name="elseActivityCallback"></param>
    /// <returns></returns>
    EventActivityBinder<TSaga, TMessage> IfElseAsync(StateMachineAsyncCondition<TSaga, TMessage> condition,
        Func<EventActivityBinder<TSaga, TMessage>, EventActivityBinder<TSaga, TMessage>> thenActivityCallback,
        Func<EventActivityBinder<TSaga, TMessage>, EventActivityBinder<TSaga, TMessage>> elseActivityCallback);
}
