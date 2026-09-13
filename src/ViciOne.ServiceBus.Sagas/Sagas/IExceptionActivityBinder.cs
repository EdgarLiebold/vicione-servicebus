using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Builds the compensation activity chain for an event failure without a message payload.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
/// <typeparam name="TException">The exception type being handled.</typeparam>
public interface IExceptionActivityBinder<TSaga, TException> :
    IEventActivities<TSaga>
    where TSaga : class, ISagaStateMachineInstance
    where TException : Exception
{
    /// <summary>Gets the state machine that owns the failed event.</summary>
    IStateMachine<TSaga> StateMachine { get; }

    /// <summary>Gets the event whose failure is being handled.</summary>
    IEvent Event { get; }

    /// <summary>Appends an activity to the compensation behavior.</summary>
    /// <param name="activity">The activity to append.</param>
    /// <returns>A binder containing the appended activity.</returns>
    IExceptionActivityBinder<TSaga, TException> Add(IStateMachineActivity<TSaga> activity);

    /// <summary>Appends a nested compensation branch for a specified exception type.</summary>
    /// <typeparam name="TNestedException">The exception type handled by the nested branch.</typeparam>
    /// <param name="activityCallback">A callback that configures the nested compensation activities.</param>
    /// <returns>A binder containing the nested exception branch.</returns>
    IExceptionActivityBinder<TSaga, TException> Catch<TNestedException>(
        Func<IExceptionActivityBinder<TSaga, TNestedException>, IExceptionActivityBinder<TSaga, TNestedException>> activityCallback)
        where TNestedException : Exception;

    /// <summary>Appends compensation activities when a synchronous exception condition is satisfied.</summary>
    /// <param name="condition">The condition evaluated for the saga and exception.</param>
    /// <param name="activityCallback">A callback that configures the conditional activities.</param>
    /// <returns>A binder containing the conditional branch.</returns>
    IExceptionActivityBinder<TSaga, TException> If(StateMachineExceptionCondition<TSaga, TException> condition,
        Func<IExceptionActivityBinder<TSaga, TException>, IExceptionActivityBinder<TSaga, TException>> activityCallback);

    /// <summary>Appends compensation activities when an asynchronously evaluated exception condition is satisfied.</summary>
    /// <param name="condition">The asynchronous condition evaluated for the saga and exception.</param>
    /// <param name="activityCallback">A callback that configures the conditional activities.</param>
    /// <returns>A binder containing the conditional branch.</returns>
    IExceptionActivityBinder<TSaga, TException> IfAwaited(StateMachineAsyncExceptionCondition<TSaga, TException> condition,
        Func<IExceptionActivityBinder<TSaga, TException>, IExceptionActivityBinder<TSaga, TException>> activityCallback);

    /// <summary>Appends compensation branches selected by a synchronous exception condition.</summary>
    /// <param name="condition">The condition evaluated for the saga and exception.</param>
    /// <param name="thenActivityCallback">A callback that configures activities for a satisfied condition.</param>
    /// <param name="elseActivityCallback">A callback that configures activities for an unsatisfied condition.</param>
    /// <returns>A binder containing both conditional branches.</returns>
    IExceptionActivityBinder<TSaga, TException> IfElse(StateMachineExceptionCondition<TSaga, TException> condition,
        Func<IExceptionActivityBinder<TSaga, TException>, IExceptionActivityBinder<TSaga, TException>> thenActivityCallback,
        Func<IExceptionActivityBinder<TSaga, TException>, IExceptionActivityBinder<TSaga, TException>> elseActivityCallback);

    /// <summary>Appends compensation branches selected by an asynchronously evaluated exception condition.</summary>
    /// <param name="condition">The asynchronous condition evaluated for the saga and exception.</param>
    /// <param name="thenActivityCallback">A callback that configures activities for a satisfied condition.</param>
    /// <param name="elseActivityCallback">A callback that configures activities for an unsatisfied condition.</param>
    /// <returns>A binder containing both conditional branches.</returns>
    IExceptionActivityBinder<TSaga, TException> IfElseAwaited(StateMachineAsyncExceptionCondition<TSaga, TException> condition,
        Func<IExceptionActivityBinder<TSaga, TException>, IExceptionActivityBinder<TSaga, TException>> thenActivityCallback,
        Func<IExceptionActivityBinder<TSaga, TException>, IExceptionActivityBinder<TSaga, TException>> elseActivityCallback);
}


/// <summary>Builds the message-aware compensation activity chain for an event failure.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
/// <typeparam name="TMessage">The failed event's message type.</typeparam>
/// <typeparam name="TException">The exception type being handled.</typeparam>
public interface IExceptionActivityBinder<TSaga, TMessage, TException> :
    IEventActivities<TSaga>
    where TSaga : class, ISagaStateMachineInstance
    where TException : Exception
    where TMessage : class
{
    /// <summary>Gets the state machine that owns the failed event.</summary>
    IStateMachine<TSaga> StateMachine { get; }

    /// <summary>Gets the event whose failure is being handled.</summary>
    IEvent<TMessage> Event { get; }

    /// <summary>Appends an untyped activity to the compensation behavior.</summary>
    /// <param name="activity">The activity to append.</param>
    /// <returns>A binder containing the appended activity.</returns>
    IExceptionActivityBinder<TSaga, TMessage, TException> Add(IStateMachineActivity<TSaga> activity);

    /// <summary>Appends a message-aware activity to the compensation behavior.</summary>
    /// <param name="activity">The activity to append.</param>
    /// <returns>A binder containing the appended activity.</returns>
    IExceptionActivityBinder<TSaga, TMessage, TException> Add(IStateMachineActivity<TSaga, TMessage> activity);

    /// <summary>Appends a nested message-aware compensation branch for a specified exception type.</summary>
    /// <typeparam name="TNestedException">The exception type handled by the nested branch.</typeparam>
    /// <param name="activityCallback">A callback that configures the nested compensation activities.</param>
    /// <returns>A binder containing the nested exception branch.</returns>
    IExceptionActivityBinder<TSaga, TMessage, TException> Catch<TNestedException>(
        Func<IExceptionActivityBinder<TSaga, TMessage, TNestedException>, IExceptionActivityBinder<TSaga, TMessage, TNestedException>> activityCallback)
        where TNestedException : Exception;

    /// <summary>Appends message-aware compensation activities when a synchronous exception condition is satisfied.</summary>
    /// <param name="condition">The condition evaluated for the saga, event message, and exception.</param>
    /// <param name="activityCallback">A callback that configures the conditional activities.</param>
    /// <returns>A binder containing the conditional branch.</returns>
    IExceptionActivityBinder<TSaga, TMessage, TException> If(StateMachineExceptionCondition<TSaga, TMessage, TException> condition,
        Func<IExceptionActivityBinder<TSaga, TMessage, TException>, IExceptionActivityBinder<TSaga, TMessage, TException>> activityCallback);

    /// <summary>Appends message-aware compensation activities when an asynchronously evaluated exception condition is satisfied.</summary>
    /// <param name="condition">The asynchronous condition evaluated for the saga, event message, and exception.</param>
    /// <param name="activityCallback">A callback that configures the conditional activities.</param>
    /// <returns>A binder containing the conditional branch.</returns>
    IExceptionActivityBinder<TSaga, TMessage, TException> IfAwaited(StateMachineAsyncExceptionCondition<TSaga, TMessage, TException> condition,
        Func<IExceptionActivityBinder<TSaga, TMessage, TException>, IExceptionActivityBinder<TSaga, TMessage, TException>> activityCallback);

    /// <summary>Appends message-aware compensation branches selected by a synchronous exception condition.</summary>
    /// <param name="condition">The condition evaluated for the saga, event message, and exception.</param>
    /// <param name="thenActivityCallback">A callback that configures activities for a satisfied condition.</param>
    /// <param name="elseActivityCallback">A callback that configures activities for an unsatisfied condition.</param>
    /// <returns>A binder containing both conditional branches.</returns>
    IExceptionActivityBinder<TSaga, TMessage, TException> IfElse(StateMachineExceptionCondition<TSaga, TMessage, TException> condition,
        Func<IExceptionActivityBinder<TSaga, TMessage, TException>, IExceptionActivityBinder<TSaga, TMessage, TException>> thenActivityCallback,
        Func<IExceptionActivityBinder<TSaga, TMessage, TException>, IExceptionActivityBinder<TSaga, TMessage, TException>> elseActivityCallback);

    /// <summary>Appends message-aware compensation branches selected by an asynchronously evaluated exception condition.</summary>
    /// <param name="condition">The asynchronous condition evaluated for the saga, event message, and exception.</param>
    /// <param name="thenActivityCallback">A callback that configures activities for a satisfied condition.</param>
    /// <param name="elseActivityCallback">A callback that configures activities for an unsatisfied condition.</param>
    /// <returns>A binder containing both conditional branches.</returns>
    IExceptionActivityBinder<TSaga, TMessage, TException> IfElseAwaited(StateMachineAsyncExceptionCondition<TSaga, TMessage, TException> condition,
        Func<IExceptionActivityBinder<TSaga, TMessage, TException>, IExceptionActivityBinder<TSaga, TMessage, TException>> thenActivityCallback,
        Func<IExceptionActivityBinder<TSaga, TMessage, TException>, IExceptionActivityBinder<TSaga, TMessage, TException>> elseActivityCallback);
}
