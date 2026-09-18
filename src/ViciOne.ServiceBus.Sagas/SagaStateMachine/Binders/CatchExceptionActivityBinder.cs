using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Binds catch exception activity activities to the pipeline.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
/// <typeparam name="TException">The exception handled by the member.</typeparam>
public class CatchExceptionActivityBinder<TInstance, TException> :
    IExceptionActivityBinder<TInstance, TException>
    where TInstance : class, ISagaStateMachineInstance
    where TException : Exception
{
    readonly IActivityBinder<TInstance>[] _activities;
    readonly IStateMachine<TInstance> _machine;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="machine">The machine.</param>
    /// <param name="event">The event.</param>
    /// <exception cref="ArgumentNullException"><paramref name="machine"/> or <paramref name="event"/> is <see langword="null"/>.</exception>
    public CatchExceptionActivityBinder(IStateMachine<TInstance> machine, IEvent @event)
    {
        ArgumentNullException.ThrowIfNull(machine);
        ArgumentNullException.ThrowIfNull(@event, nameof(@event));

        _activities = [];
        _machine = machine;
        Event = @event;
    }

    CatchExceptionActivityBinder(IStateMachine<TInstance> machine, IEvent @event,
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
    public IEvent Event { get; }

    /// <summary>Gets state activity binders.</summary>
    /// <returns>The state activity binders.</returns>
    public IEnumerable<IActivityBinder<TInstance>> GetStateActivityBinders()
    {
        return (IActivityBinder<TInstance>[])_activities.Clone();
    }

    /// <summary>Gets the state machine.</summary>
    public IStateMachine<TInstance> StateMachine => _machine;

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="activity">The activity.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="activity"/> is <see langword="null"/>.</exception>
    public IExceptionActivityBinder<TInstance, TException> Add(IStateMachineActivity<TInstance> activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        IActivityBinder<TInstance> activityBinder = new ExecuteActivityBinder<TInstance>(Event, activity);

        return new CatchExceptionActivityBinder<TInstance, TException>(_machine, Event, _activities, activityBinder);
    }

    /// <summary>Adds a fault-handling branch.</summary>
    /// <typeparam name="T">The exception type.</typeparam>
    /// <param name="activityCallback">The activity callback.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="activityCallback"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="activityCallback"/> returns <see langword="null"/>.</exception>
    public IExceptionActivityBinder<TInstance, TException> Catch<T>(
        Func<IExceptionActivityBinder<TInstance, T>, IExceptionActivityBinder<TInstance, T>> activityCallback)
        where T : Exception
    {
        ArgumentNullException.ThrowIfNull(activityCallback);

        IExceptionActivityBinder<TInstance, T> binder = new CatchExceptionActivityBinder<TInstance, T>(_machine, Event);

        binder = activityCallback(binder)
            ?? throw new InvalidOperationException("The exception activity configuration callback returned null.");

        IActivityBinder<TInstance> activityBinder = new CatchActivityBinder<TInstance, T>(Event, binder);

        return new CatchExceptionActivityBinder<TInstance, TException>(_machine, Event, _activities, activityBinder);
    }

    /// <summary>Adds a conditional branch.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="activityCallback">The activity callback.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="condition"/> or <paramref name="activityCallback"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="activityCallback"/> returns <see langword="null"/>.</exception>
    public IExceptionActivityBinder<TInstance, TException> If(StateMachineExceptionCondition<TInstance, TException> condition,
        Func<IExceptionActivityBinder<TInstance, TException>, IExceptionActivityBinder<TInstance, TException>> activityCallback)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(activityCallback);

        return IfElse(condition, activityCallback, b => b);
    }

    /// <summary>Adds a conditional branch.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="activityCallback">The activity callback.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="condition"/> or <paramref name="activityCallback"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="activityCallback"/> returns <see langword="null"/>.</exception>
    public IExceptionActivityBinder<TInstance, TException> IfAwaited(StateMachineAsyncExceptionCondition<TInstance, TException> condition,
        Func<IExceptionActivityBinder<TInstance, TException>, IExceptionActivityBinder<TInstance, TException>> activityCallback)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(activityCallback);

        return IfElseAwaited(condition, activityCallback, b => b);
    }

    /// <summary>Adds conditional success and alternative branches.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="thenActivityCallback">The then activity callback.</param>
    /// <param name="elseActivityCallback">The else activity callback.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="condition"/>, <paramref name="thenActivityCallback"/>, or <paramref name="elseActivityCallback"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="thenActivityCallback"/> or <paramref name="elseActivityCallback"/> returns <see langword="null"/>.</exception>
    public IExceptionActivityBinder<TInstance, TException> IfElse(StateMachineExceptionCondition<TInstance, TException> condition,
        Func<IExceptionActivityBinder<TInstance, TException>, IExceptionActivityBinder<TInstance, TException>> thenActivityCallback,
        Func<IExceptionActivityBinder<TInstance, TException>, IExceptionActivityBinder<TInstance, TException>> elseActivityCallback)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(thenActivityCallback);
        ArgumentNullException.ThrowIfNull(elseActivityCallback);

        IExceptionActivityBinder<TInstance, TException> thenBinder = GetBinder(thenActivityCallback);
        IExceptionActivityBinder<TInstance, TException> elseBinder = GetBinder(elseActivityCallback);

        var conditionBinder = new ConditionalExceptionActivityBinder<TInstance, TException>(Event, condition, thenBinder, elseBinder);

        return new CatchExceptionActivityBinder<TInstance, TException>(_machine, Event, _activities, conditionBinder);
    }

    /// <summary>Adds conditional success and alternative branches.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="thenActivityCallback">The then activity callback.</param>
    /// <param name="elseActivityCallback">The else activity callback.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="condition"/>, <paramref name="thenActivityCallback"/>, or <paramref name="elseActivityCallback"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="thenActivityCallback"/> or <paramref name="elseActivityCallback"/> returns <see langword="null"/>.</exception>
    public IExceptionActivityBinder<TInstance, TException> IfElseAwaited(StateMachineAsyncExceptionCondition<TInstance, TException> condition,
        Func<IExceptionActivityBinder<TInstance, TException>, IExceptionActivityBinder<TInstance, TException>> thenActivityCallback,
        Func<IExceptionActivityBinder<TInstance, TException>, IExceptionActivityBinder<TInstance, TException>> elseActivityCallback)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(thenActivityCallback);
        ArgumentNullException.ThrowIfNull(elseActivityCallback);

        IExceptionActivityBinder<TInstance, TException> thenBinder = GetBinder(thenActivityCallback);
        IExceptionActivityBinder<TInstance, TException> elseBinder = GetBinder(elseActivityCallback);

        var conditionBinder = new ConditionalExceptionActivityBinder<TInstance, TException>(Event, condition, thenBinder, elseBinder);

        return new CatchExceptionActivityBinder<TInstance, TException>(_machine, Event, _activities, conditionBinder);
    }

    IExceptionActivityBinder<TInstance, TException> GetBinder(
        Func<IExceptionActivityBinder<TInstance, TException>, IExceptionActivityBinder<TInstance, TException>> callback)
    {
        IExceptionActivityBinder<TInstance, TException> binder = new CatchExceptionActivityBinder<TInstance, TException>(_machine, Event);
        return callback(binder)
            ?? throw new InvalidOperationException("The exception activity configuration callback returned null.");
    }
}


/// <summary>Binds catch exception activity activities to the pipeline.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
/// <typeparam name="TData">The data type.</typeparam>
/// <typeparam name="TException">The exception handled by the member.</typeparam>
public class CatchExceptionActivityBinder<TInstance, TData, TException> :
    IExceptionActivityBinder<TInstance, TData, TException>
    where TInstance : class, ISagaStateMachineInstance
    where TException : Exception
    where TData : class
{
    readonly IActivityBinder<TInstance>[] _activities;
    readonly IStateMachine<TInstance> _machine;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="machine">The machine.</param>
    /// <param name="event">The event.</param>
    /// <exception cref="ArgumentNullException"><paramref name="machine"/> or <paramref name="event"/> is <see langword="null"/>.</exception>
    public CatchExceptionActivityBinder(IStateMachine<TInstance> machine, IEvent<TData> @event)
    {
        ArgumentNullException.ThrowIfNull(machine);
        ArgumentNullException.ThrowIfNull(@event, nameof(@event));

        _activities = [];
        _machine = machine;
        Event = @event;
    }

    CatchExceptionActivityBinder(IStateMachine<TInstance> machine, IEvent<TData> @event,
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
    public IEvent<TData> Event { get; }

    /// <summary>Gets state activity binders.</summary>
    /// <returns>The state activity binders.</returns>
    public IEnumerable<IActivityBinder<TInstance>> GetStateActivityBinders()
    {
        return (IActivityBinder<TInstance>[])_activities.Clone();
    }

    /// <summary>Gets the state machine.</summary>
    public IStateMachine<TInstance> StateMachine => _machine;

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="activity">The activity.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="activity"/> is <see langword="null"/>.</exception>
    public IExceptionActivityBinder<TInstance, TData, TException> Add(IStateMachineActivity<TInstance> activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        IActivityBinder<TInstance> activityBinder = new ExecuteActivityBinder<TInstance>(Event, activity);

        return new CatchExceptionActivityBinder<TInstance, TData, TException>(_machine, Event, _activities, activityBinder);
    }

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="activity">The activity.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="activity"/> is <see langword="null"/>.</exception>
    public IExceptionActivityBinder<TInstance, TData, TException> Add(IStateMachineActivity<TInstance, TData> activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        var converterActivity = new DataConverterActivity<TInstance, TData>(activity);

        IActivityBinder<TInstance> activityBinder = new ExecuteActivityBinder<TInstance>(Event, converterActivity);

        return new CatchExceptionActivityBinder<TInstance, TData, TException>(_machine, Event, _activities, activityBinder);
    }

    /// <summary>Adds a fault-handling branch.</summary>
    /// <typeparam name="T">The exception type.</typeparam>
    /// <param name="activityCallback">The activity callback.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="activityCallback"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="activityCallback"/> returns <see langword="null"/>.</exception>
    public IExceptionActivityBinder<TInstance, TData, TException> Catch<T>(
        Func<IExceptionActivityBinder<TInstance, TData, T>, IExceptionActivityBinder<TInstance, TData, T>> activityCallback)
        where T : Exception
    {
        ArgumentNullException.ThrowIfNull(activityCallback);

        IExceptionActivityBinder<TInstance, TData, T> binder = new CatchExceptionActivityBinder<TInstance, TData, T>(_machine, Event);

        binder = activityCallback(binder)
            ?? throw new InvalidOperationException("The exception activity configuration callback returned null.");

        IActivityBinder<TInstance> activityBinder = new CatchActivityBinder<TInstance, T>(Event, binder);

        return new CatchExceptionActivityBinder<TInstance, TData, TException>(_machine, Event, _activities, activityBinder);
    }

    /// <summary>Adds a conditional branch.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="activityCallback">The activity callback.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="condition"/> or <paramref name="activityCallback"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="activityCallback"/> returns <see langword="null"/>.</exception>
    public IExceptionActivityBinder<TInstance, TData, TException> If(StateMachineExceptionCondition<TInstance, TData, TException> condition,
        Func<IExceptionActivityBinder<TInstance, TData, TException>, IExceptionActivityBinder<TInstance, TData, TException>> activityCallback)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(activityCallback);

        return IfElse(condition, activityCallback, b => b);
    }

    /// <summary>Adds a conditional branch.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="activityCallback">The activity callback.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="condition"/> or <paramref name="activityCallback"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="activityCallback"/> returns <see langword="null"/>.</exception>
    public IExceptionActivityBinder<TInstance, TData, TException> IfAwaited(StateMachineAsyncExceptionCondition<TInstance, TData, TException> condition,
        Func<IExceptionActivityBinder<TInstance, TData, TException>, IExceptionActivityBinder<TInstance, TData, TException>> activityCallback)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(activityCallback);

        return IfElseAwaited(condition, activityCallback, b => b);
    }

    /// <summary>Adds conditional success and alternative branches.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="thenActivityCallback">The then activity callback.</param>
    /// <param name="elseActivityCallback">The else activity callback.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="condition"/>, <paramref name="thenActivityCallback"/>, or <paramref name="elseActivityCallback"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="thenActivityCallback"/> or <paramref name="elseActivityCallback"/> returns <see langword="null"/>.</exception>
    public IExceptionActivityBinder<TInstance, TData, TException> IfElse(StateMachineExceptionCondition<TInstance, TData, TException> condition,
        Func<IExceptionActivityBinder<TInstance, TData, TException>, IExceptionActivityBinder<TInstance, TData, TException>> thenActivityCallback,
        Func<IExceptionActivityBinder<TInstance, TData, TException>, IExceptionActivityBinder<TInstance, TData, TException>> elseActivityCallback)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(thenActivityCallback);
        ArgumentNullException.ThrowIfNull(elseActivityCallback);

        IExceptionActivityBinder<TInstance, TData, TException> thenBinder = GetBinder(thenActivityCallback);
        IExceptionActivityBinder<TInstance, TData, TException> elseBinder = GetBinder(elseActivityCallback);

        var conditionBinder = new ConditionalExceptionActivityBinder<TInstance, TData, TException>(Event, condition, thenBinder, elseBinder);

        return new CatchExceptionActivityBinder<TInstance, TData, TException>(_machine, Event, _activities, conditionBinder);
    }

    /// <summary>Adds conditional success and alternative branches.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="thenActivityCallback">The then activity callback.</param>
    /// <param name="elseActivityCallback">The else activity callback.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="condition"/>, <paramref name="thenActivityCallback"/>, or <paramref name="elseActivityCallback"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="thenActivityCallback"/> or <paramref name="elseActivityCallback"/> returns <see langword="null"/>.</exception>
    public IExceptionActivityBinder<TInstance, TData, TException> IfElseAwaited(StateMachineAsyncExceptionCondition<TInstance, TData, TException> condition,
        Func<IExceptionActivityBinder<TInstance, TData, TException>, IExceptionActivityBinder<TInstance, TData, TException>> thenActivityCallback,
        Func<IExceptionActivityBinder<TInstance, TData, TException>, IExceptionActivityBinder<TInstance, TData, TException>> elseActivityCallback)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(thenActivityCallback);
        ArgumentNullException.ThrowIfNull(elseActivityCallback);

        IExceptionActivityBinder<TInstance, TData, TException> thenBinder = GetBinder(thenActivityCallback);
        IExceptionActivityBinder<TInstance, TData, TException> elseBinder = GetBinder(elseActivityCallback);

        var conditionBinder = new ConditionalExceptionActivityBinder<TInstance, TData, TException>(Event, condition, thenBinder, elseBinder);

        return new CatchExceptionActivityBinder<TInstance, TData, TException>(_machine, Event, _activities, conditionBinder);
    }

    IExceptionActivityBinder<TInstance, TData, TException> GetBinder(
        Func<IExceptionActivityBinder<TInstance, TData, TException>, IExceptionActivityBinder<TInstance, TData, TException>> callback)
    {
        IExceptionActivityBinder<TInstance, TData, TException> binder = new CatchExceptionActivityBinder<TInstance, TData, TException>(_machine, Event);
        return callback(binder)
            ?? throw new InvalidOperationException("The exception activity configuration callback returned null.");
    }
}
