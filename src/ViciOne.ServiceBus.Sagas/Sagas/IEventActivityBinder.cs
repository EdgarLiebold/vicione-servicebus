using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Builds the activity chain executed for a state-machine event without a message payload.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
public interface IEventActivityBinder<TSaga> :
    IEventActivities<TSaga>
    where TSaga : class, ISagaStateMachineInstance
{
    /// <summary>Gets the state machine that owns the event.</summary>
    IStateMachine<TSaga> StateMachine { get; }

    /// <summary>Gets the event whose activity chain is being configured.</summary>
    IEvent Event { get; }

    /// <summary>Appends an activity to the event behavior.</summary>
    /// <param name="activity">The activity to append.</param>
    /// <returns>A binder containing the appended activity.</returns>
    IEventActivityBinder<TSaga> Add(IStateMachineActivity<TSaga> activity);

    /// <summary>Appends a compensation branch for a specified exception type.</summary>
    /// <typeparam name="TException">The exception type handled by the branch.</typeparam>
    /// <param name="activityCallback">A callback that configures the compensation activities.</param>
    /// <returns>A binder containing the exception branch.</returns>
    IEventActivityBinder<TSaga> Catch<TException>(
        Func<IExceptionActivityBinder<TSaga, TException>, IExceptionActivityBinder<TSaga, TException>> activityCallback)
        where TException : Exception;

    /// <summary>Appends an activity branch protected by a retry policy.</summary>
    /// <param name="configure">A callback that configures the retry policy.</param>
    /// <param name="activityCallback">A callback that configures the activities to retry.</param>
    /// <returns>A binder containing the retry branch.</returns>
    IEventActivityBinder<TSaga> Retry(Action<IRetryConfigurator> configure,
        Func<IEventActivityBinder<TSaga>, IEventActivityBinder<TSaga>> activityCallback);

    /// <summary>Appends activities that execute when a synchronous condition is satisfied.</summary>
    /// <param name="condition">The condition evaluated for the event context.</param>
    /// <param name="activityCallback">A callback that configures the conditional activities.</param>
    /// <returns>A binder containing the conditional branch.</returns>
    IEventActivityBinder<TSaga> If(StateMachineCondition<TSaga> condition,
        Func<IEventActivityBinder<TSaga>, IEventActivityBinder<TSaga>> activityCallback);

    /// <summary>Appends activities that execute when an asynchronously evaluated condition is satisfied.</summary>
    /// <param name="condition">The asynchronous condition evaluated for the event context.</param>
    /// <param name="activityCallback">A callback that configures the conditional activities.</param>
    /// <returns>A binder containing the conditional branch.</returns>
    IEventActivityBinder<TSaga> IfAwaited(StateMachineAsyncCondition<TSaga> condition,
        Func<IEventActivityBinder<TSaga>, IEventActivityBinder<TSaga>> activityCallback);

    /// <summary>Appends mutually exclusive activity branches selected by a synchronous condition.</summary>
    /// <param name="condition">The condition evaluated for the event context.</param>
    /// <param name="thenActivityCallback">A callback that configures activities for a satisfied condition.</param>
    /// <param name="elseActivityCallback">A callback that configures activities for an unsatisfied condition.</param>
    /// <returns>A binder containing both conditional branches.</returns>
    IEventActivityBinder<TSaga> IfElse(StateMachineCondition<TSaga> condition,
        Func<IEventActivityBinder<TSaga>, IEventActivityBinder<TSaga>> thenActivityCallback,
        Func<IEventActivityBinder<TSaga>, IEventActivityBinder<TSaga>> elseActivityCallback);

    /// <summary>Appends mutually exclusive activity branches selected by an asynchronously evaluated condition.</summary>
    /// <param name="condition">The asynchronous condition evaluated for the event context.</param>
    /// <param name="thenActivityCallback">A callback that configures activities for a satisfied condition.</param>
    /// <param name="elseActivityCallback">A callback that configures activities for an unsatisfied condition.</param>
    /// <returns>A binder containing both conditional branches.</returns>
    IEventActivityBinder<TSaga> IfElseAwaited(StateMachineAsyncCondition<TSaga> condition,
        Func<IEventActivityBinder<TSaga>, IEventActivityBinder<TSaga>> thenActivityCallback,
        Func<IEventActivityBinder<TSaga>, IEventActivityBinder<TSaga>> elseActivityCallback);
}


/// <summary>Builds the activity chain executed for a state-machine event carrying a message.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
/// <typeparam name="TMessage">The event message type.</typeparam>
public interface IEventActivityBinder<TSaga, TMessage> :
    IEventActivities<TSaga>
    where TSaga : class, ISagaStateMachineInstance
    where TMessage : class
{
    /// <summary>Gets the state machine that owns the event.</summary>
    IStateMachine<TSaga> StateMachine { get; }

    /// <summary>Gets the event whose activity chain is being configured.</summary>
    IEvent<TMessage> Event { get; }

    /// <summary>Appends an untyped activity to the event behavior.</summary>
    /// <param name="activity">The activity to append.</param>
    /// <returns>A binder containing the appended activity.</returns>
    IEventActivityBinder<TSaga, TMessage> Add(IStateMachineActivity<TSaga> activity);

    /// <summary>Appends a message-aware activity to the event behavior.</summary>
    /// <param name="activity">The activity to append.</param>
    /// <returns>A binder containing the appended activity.</returns>
    IEventActivityBinder<TSaga, TMessage> Add(IStateMachineActivity<TSaga, TMessage> activity);

    /// <summary>Appends a message-aware compensation branch for a specified exception type.</summary>
    /// <typeparam name="TException">The exception type handled by the branch.</typeparam>
    /// <param name="activityCallback">A callback that configures the compensation activities.</param>
    /// <returns>A binder containing the exception branch.</returns>
    IEventActivityBinder<TSaga, TMessage> Catch<TException>(
        Func<IExceptionActivityBinder<TSaga, TMessage, TException>, IExceptionActivityBinder<TSaga, TMessage, TException>> activityCallback)
        where TException : Exception;

    /// <summary>Appends a message-aware activity branch protected by a retry policy.</summary>
    /// <param name="configure">A callback that configures the retry policy.</param>
    /// <param name="activityCallback">A callback that configures the activities to retry.</param>
    /// <returns>A binder containing the retry branch.</returns>
    IEventActivityBinder<TSaga, TMessage> Retry(Action<IRetryConfigurator> configure,
        Func<IEventActivityBinder<TSaga, TMessage>, IEventActivityBinder<TSaga, TMessage>> activityCallback);

    /// <summary>Appends activities that execute when a synchronous message condition is satisfied.</summary>
    /// <param name="condition">The condition evaluated for the saga and event message.</param>
    /// <param name="activityCallback">A callback that configures the conditional activities.</param>
    /// <returns>A binder containing the conditional branch.</returns>
    IEventActivityBinder<TSaga, TMessage> If(StateMachineCondition<TSaga, TMessage> condition,
        Func<IEventActivityBinder<TSaga, TMessage>, IEventActivityBinder<TSaga, TMessage>> activityCallback);

    /// <summary>Appends activities that execute when an asynchronously evaluated message condition is satisfied.</summary>
    /// <param name="condition">The asynchronous condition evaluated for the saga and event message.</param>
    /// <param name="activityCallback">A callback that configures the conditional activities.</param>
    /// <returns>A binder containing the conditional branch.</returns>
    IEventActivityBinder<TSaga, TMessage> IfAwaited(StateMachineAsyncCondition<TSaga, TMessage> condition,
        Func<IEventActivityBinder<TSaga, TMessage>, IEventActivityBinder<TSaga, TMessage>> activityCallback);

    /// <summary>Appends message-aware branches selected by a synchronous condition.</summary>
    /// <param name="condition">The condition evaluated for the saga and event message.</param>
    /// <param name="thenActivityCallback">A callback that configures activities for a satisfied condition.</param>
    /// <param name="elseActivityCallback">A callback that configures activities for an unsatisfied condition.</param>
    /// <returns>A binder containing both conditional branches.</returns>
    IEventActivityBinder<TSaga, TMessage> IfElse(StateMachineCondition<TSaga, TMessage> condition,
        Func<IEventActivityBinder<TSaga, TMessage>, IEventActivityBinder<TSaga, TMessage>> thenActivityCallback,
        Func<IEventActivityBinder<TSaga, TMessage>, IEventActivityBinder<TSaga, TMessage>> elseActivityCallback);

    /// <summary>Appends message-aware branches selected by an asynchronously evaluated condition.</summary>
    /// <param name="condition">The asynchronous condition evaluated for the saga and event message.</param>
    /// <param name="thenActivityCallback">A callback that configures activities for a satisfied condition.</param>
    /// <param name="elseActivityCallback">A callback that configures activities for an unsatisfied condition.</param>
    /// <returns>A binder containing both conditional branches.</returns>
    IEventActivityBinder<TSaga, TMessage> IfElseAwaited(StateMachineAsyncCondition<TSaga, TMessage> condition,
        Func<IEventActivityBinder<TSaga, TMessage>, IEventActivityBinder<TSaga, TMessage>> thenActivityCallback,
        Func<IEventActivityBinder<TSaga, TMessage>, IEventActivityBinder<TSaga, TMessage>> elseActivityCallback);
}
