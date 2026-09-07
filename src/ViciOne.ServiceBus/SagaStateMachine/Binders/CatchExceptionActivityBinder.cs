using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Binds catch exception activity activities to the pipeline.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
/// <typeparam name="TException">The exception handled by the member.</typeparam>
public class CatchExceptionActivityBinder<TInstance, TException> :
    ExceptionActivityBinder<TInstance, TException>
    where TInstance : class, SagaStateMachineInstance
    where TException : Exception
{
    readonly IActivityBinder<TInstance>[] _activities;
    readonly StateMachine<TInstance> _machine;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="machine">The machine.</param>
    /// <param name="event">The event.</param>
    public CatchExceptionActivityBinder(StateMachine<TInstance> machine, Event @event)
    {
        _activities = [];
        _machine = machine;
        Event = @event;
    }

    CatchExceptionActivityBinder(StateMachine<TInstance> machine, Event @event,
        IActivityBinder<TInstance>[] activities,
        params IActivityBinder<TInstance>[] appendActivity)
    {
        _activities = new IActivityBinder<TInstance>[activities.Length + appendActivity.Length];
        Array.Copy(activities, 0, _activities, 0, activities.Length);
        Array.Copy(appendActivity, 0, _activities, activities.Length, appendActivity.Length);

        _machine = machine;
        Event = @event;
    }

    /// <summary>Gets the event.</summary>
    public Event Event { get; }

    /// <summary>Gets state activity binders.</summary>
    /// <returns>The state activity binders.</returns>
    public IEnumerable<IActivityBinder<TInstance>> GetStateActivityBinders()
    {
        return _activities;
    }

    /// <summary>Gets the state machine.</summary>
    public StateMachine<TInstance> StateMachine => _machine;

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="activity">The activity.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public ExceptionActivityBinder<TInstance, TException> Add(IStateMachineActivity<TInstance> activity)
    {
        IActivityBinder<TInstance> activityBinder = new ExecuteActivityBinder<TInstance>(Event, activity);

        return new CatchExceptionActivityBinder<TInstance, TException>(_machine, Event, _activities, activityBinder);
    }

    /// <summary>Adds a fault-handling branch.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="activityCallback">The activity callback.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public ExceptionActivityBinder<TInstance, TException> Catch<T>(
        Func<ExceptionActivityBinder<TInstance, T>, ExceptionActivityBinder<TInstance, T>> activityCallback)
        where T : Exception
    {
        ExceptionActivityBinder<TInstance, T> binder = new CatchExceptionActivityBinder<TInstance, T>(_machine, Event);

        binder = activityCallback(binder);

        IActivityBinder<TInstance> activityBinder = new CatchActivityBinder<TInstance, T>(Event, binder);

        return new CatchExceptionActivityBinder<TInstance, TException>(_machine, Event, _activities, activityBinder);
    }

    /// <summary>Adds a conditional branch.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="activityCallback">The activity callback.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public ExceptionActivityBinder<TInstance, TException> If(StateMachineExceptionCondition<TInstance, TException> condition,
        Func<ExceptionActivityBinder<TInstance, TException>, ExceptionActivityBinder<TInstance, TException>> activityCallback)
    {
        return IfElse(condition, activityCallback, b => b);
    }

    /// <summary>Adds a conditional branch.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="activityCallback">The activity callback.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public ExceptionActivityBinder<TInstance, TException> IfAwaited(StateMachineAsyncExceptionCondition<TInstance, TException> condition,
        Func<ExceptionActivityBinder<TInstance, TException>, ExceptionActivityBinder<TInstance, TException>> activityCallback)
    {
        return IfElseAwaited(condition, activityCallback, b => b);
    }

    /// <summary>Adds conditional success and alternative branches.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="thenActivityCallback">The then activity callback.</param>
    /// <param name="elseActivityCallback">The else activity callback.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public ExceptionActivityBinder<TInstance, TException> IfElse(StateMachineExceptionCondition<TInstance, TException> condition,
        Func<ExceptionActivityBinder<TInstance, TException>, ExceptionActivityBinder<TInstance, TException>> thenActivityCallback,
        Func<ExceptionActivityBinder<TInstance, TException>, ExceptionActivityBinder<TInstance, TException>> elseActivityCallback)
    {
        ExceptionActivityBinder<TInstance, TException> thenBinder = GetBinder(thenActivityCallback);
        ExceptionActivityBinder<TInstance, TException> elseBinder = GetBinder(elseActivityCallback);

        var conditionBinder = new ConditionalExceptionActivityBinder<TInstance, TException>(Event, condition, thenBinder, elseBinder);

        return new CatchExceptionActivityBinder<TInstance, TException>(_machine, Event, _activities, conditionBinder);
    }

    /// <summary>Adds conditional success and alternative branches.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="thenActivityCallback">The then activity callback.</param>
    /// <param name="elseActivityCallback">The else activity callback.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public ExceptionActivityBinder<TInstance, TException> IfElseAwaited(StateMachineAsyncExceptionCondition<TInstance, TException> condition,
        Func<ExceptionActivityBinder<TInstance, TException>, ExceptionActivityBinder<TInstance, TException>> thenActivityCallback,
        Func<ExceptionActivityBinder<TInstance, TException>, ExceptionActivityBinder<TInstance, TException>> elseActivityCallback)
    {
        ExceptionActivityBinder<TInstance, TException> thenBinder = GetBinder(thenActivityCallback);
        ExceptionActivityBinder<TInstance, TException> elseBinder = GetBinder(elseActivityCallback);

        var conditionBinder = new ConditionalExceptionActivityBinder<TInstance, TException>(Event, condition, thenBinder, elseBinder);

        return new CatchExceptionActivityBinder<TInstance, TException>(_machine, Event, _activities, conditionBinder);
    }

    ExceptionActivityBinder<TInstance, TException> GetBinder(
        Func<ExceptionActivityBinder<TInstance, TException>, ExceptionActivityBinder<TInstance, TException>> callback)
    {
        ExceptionActivityBinder<TInstance, TException> thenBinder = new CatchExceptionActivityBinder<TInstance, TException>(_machine, Event);
        return callback(thenBinder);
    }
}


/// <summary>Binds catch exception activity activities to the pipeline.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
/// <typeparam name="TData">The data type.</typeparam>
/// <typeparam name="TException">The exception handled by the member.</typeparam>
public class CatchExceptionActivityBinder<TInstance, TData, TException> :
    ExceptionActivityBinder<TInstance, TData, TException>
    where TInstance : class, SagaStateMachineInstance
    where TException : Exception
    where TData : class
{
    readonly IActivityBinder<TInstance>[] _activities;
    readonly StateMachine<TInstance> _machine;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="machine">The machine.</param>
    /// <param name="event">The event.</param>
    public CatchExceptionActivityBinder(StateMachine<TInstance> machine, Event<TData> @event)
    {
        _activities = [];
        _machine = machine;
        Event = @event;
    }

    CatchExceptionActivityBinder(StateMachine<TInstance> machine, Event<TData> @event,
        IActivityBinder<TInstance>[] activities,
        params IActivityBinder<TInstance>[] appendActivity)
    {
        _activities = new IActivityBinder<TInstance>[activities.Length + appendActivity.Length];
        Array.Copy(activities, 0, _activities, 0, activities.Length);
        Array.Copy(appendActivity, 0, _activities, activities.Length, appendActivity.Length);

        _machine = machine;
        Event = @event;
    }

    /// <summary>Gets the event.</summary>
    public Event<TData> Event { get; }

    /// <summary>Gets state activity binders.</summary>
    /// <returns>The state activity binders.</returns>
    public IEnumerable<IActivityBinder<TInstance>> GetStateActivityBinders()
    {
        return _activities;
    }

    /// <summary>Gets the state machine.</summary>
    public StateMachine<TInstance> StateMachine => _machine;

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="activity">The activity.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public ExceptionActivityBinder<TInstance, TData, TException> Add(IStateMachineActivity<TInstance> activity)
    {
        IActivityBinder<TInstance> activityBinder = new ExecuteActivityBinder<TInstance>(Event, activity);

        return new CatchExceptionActivityBinder<TInstance, TData, TException>(_machine, Event, _activities, activityBinder);
    }

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="activity">The activity.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public ExceptionActivityBinder<TInstance, TData, TException> Add(IStateMachineActivity<TInstance, TData> activity)
    {
        var converterActivity = new DataConverterActivity<TInstance, TData>(activity);

        IActivityBinder<TInstance> activityBinder = new ExecuteActivityBinder<TInstance>(Event, converterActivity);

        return new CatchExceptionActivityBinder<TInstance, TData, TException>(_machine, Event, _activities, activityBinder);
    }

    /// <summary>Adds a fault-handling branch.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="activityCallback">The activity callback.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public ExceptionActivityBinder<TInstance, TData, TException> Catch<T>(
        Func<ExceptionActivityBinder<TInstance, TData, T>, ExceptionActivityBinder<TInstance, TData, T>> activityCallback)
        where T : Exception
    {
        ExceptionActivityBinder<TInstance, TData, T> binder = new CatchExceptionActivityBinder<TInstance, TData, T>(_machine, Event);

        binder = activityCallback(binder);

        IActivityBinder<TInstance> activityBinder = new CatchActivityBinder<TInstance, T>(Event, binder);

        return new CatchExceptionActivityBinder<TInstance, TData, TException>(_machine, Event, _activities, activityBinder);
    }

    /// <summary>Adds a conditional branch.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="activityCallback">The activity callback.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public ExceptionActivityBinder<TInstance, TData, TException> If(StateMachineExceptionCondition<TInstance, TData, TException> condition,
        Func<ExceptionActivityBinder<TInstance, TData, TException>, ExceptionActivityBinder<TInstance, TData, TException>> activityCallback)
    {
        return IfElse(condition, activityCallback, b => b);
    }

    /// <summary>Adds a conditional branch.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="activityCallback">The activity callback.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public ExceptionActivityBinder<TInstance, TData, TException> IfAwaited(StateMachineAsyncExceptionCondition<TInstance, TData, TException> condition,
        Func<ExceptionActivityBinder<TInstance, TData, TException>, ExceptionActivityBinder<TInstance, TData, TException>> activityCallback)
    {
        return IfElseAwaited(condition, activityCallback, b => b);
    }

    /// <summary>Adds conditional success and alternative branches.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="thenActivityCallback">The then activity callback.</param>
    /// <param name="elseActivityCallback">The else activity callback.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public ExceptionActivityBinder<TInstance, TData, TException> IfElse(StateMachineExceptionCondition<TInstance, TData, TException> condition,
        Func<ExceptionActivityBinder<TInstance, TData, TException>, ExceptionActivityBinder<TInstance, TData, TException>> thenActivityCallback,
        Func<ExceptionActivityBinder<TInstance, TData, TException>, ExceptionActivityBinder<TInstance, TData, TException>> elseActivityCallback)
    {
        ExceptionActivityBinder<TInstance, TData, TException> thenBinder = GetBinder(thenActivityCallback);
        ExceptionActivityBinder<TInstance, TData, TException> elseBinder = GetBinder(elseActivityCallback);

        var conditionBinder = new ConditionalExceptionActivityBinder<TInstance, TData, TException>(Event, condition, thenBinder, elseBinder);

        return new CatchExceptionActivityBinder<TInstance, TData, TException>(_machine, Event, _activities, conditionBinder);
    }

    /// <summary>Adds conditional success and alternative branches.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="thenActivityCallback">The then activity callback.</param>
    /// <param name="elseActivityCallback">The else activity callback.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public ExceptionActivityBinder<TInstance, TData, TException> IfElseAwaited(StateMachineAsyncExceptionCondition<TInstance, TData, TException> condition,
        Func<ExceptionActivityBinder<TInstance, TData, TException>, ExceptionActivityBinder<TInstance, TData, TException>> thenActivityCallback,
        Func<ExceptionActivityBinder<TInstance, TData, TException>, ExceptionActivityBinder<TInstance, TData, TException>> elseActivityCallback)
    {
        ExceptionActivityBinder<TInstance, TData, TException> thenBinder = GetBinder(thenActivityCallback);
        ExceptionActivityBinder<TInstance, TData, TException> elseBinder = GetBinder(elseActivityCallback);

        var conditionBinder = new ConditionalExceptionActivityBinder<TInstance, TData, TException>(Event, condition, thenBinder, elseBinder);

        return new CatchExceptionActivityBinder<TInstance, TData, TException>(_machine, Event, _activities, conditionBinder);
    }

    ExceptionActivityBinder<TInstance, TData, TException> GetBinder(
        Func<ExceptionActivityBinder<TInstance, TData, TException>, ExceptionActivityBinder<TInstance, TData, TException>> callback)
    {
        ExceptionActivityBinder<TInstance, TData, TException> binder = new CatchExceptionActivityBinder<TInstance, TData, TException>(_machine, Event);
        return callback(binder);
    }
}
