using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Binds trigger event activity activities to the pipeline.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
public class TriggerEventActivityBinder<TInstance> :
    IEventActivityBinder<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    readonly IActivityBinder<TInstance>[] _activities;
    readonly StateMachineCondition<TInstance>? _filter = null!;
    readonly IStateMachine<TInstance> _machine;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="machine">The machine.</param>
    /// <param name="event">The event.</param>
    /// <param name="activities">The activities.</param>
    public TriggerEventActivityBinder(IStateMachine<TInstance> machine, IEvent @event, params IActivityBinder<TInstance>[] activities)
    {
        Event = @event;
        _machine = machine;
        _activities = activities ?? [];
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="machine">The machine.</param>
    /// <param name="event">The event.</param>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <param name="activities">The activities.</param>
    public TriggerEventActivityBinder(IStateMachine<TInstance> machine, IEvent @event, StateMachineCondition<TInstance>? filter,
        params IActivityBinder<TInstance>[] activities)
    {
        Event = @event;
        _filter = filter;
        _machine = machine;
        _activities = activities ?? [];
    }

    TriggerEventActivityBinder(IStateMachine<TInstance> machine, IEvent @event, StateMachineCondition<TInstance>? filter,
        IActivityBinder<TInstance>[] activities,
        params IActivityBinder<TInstance>[] appendActivity)
    {
        Event = @event;
        _filter = filter;
        _machine = machine;

        _activities = new IActivityBinder<TInstance>[activities.Length + appendActivity.Length];
        Array.Copy(activities, 0, _activities, 0, activities.Length);
        Array.Copy(appendActivity, 0, _activities, activities.Length, appendActivity.Length);
    }

    /// <summary>Gets the event.</summary>
    public IEvent Event { get; }

    IEvent IEventActivityBinder<TInstance>.Event => Event;

    IEventActivityBinder<TInstance> IEventActivityBinder<TInstance>.Add(IStateMachineActivity<TInstance> activity)
    {
        IActivityBinder<TInstance> activityBinder = new ExecuteActivityBinder<TInstance>(Event, activity);

        return new TriggerEventActivityBinder<TInstance>(_machine, Event, _filter, _activities, activityBinder);
    }

    IEventActivityBinder<TInstance> IEventActivityBinder<TInstance>.Catch<T>(
        Func<IExceptionActivityBinder<TInstance, T>, IExceptionActivityBinder<TInstance, T>> activityCallback)
    {
        IExceptionActivityBinder<TInstance, T> binder = new CatchExceptionActivityBinder<TInstance, T>(_machine, Event);

        binder = activityCallback(binder);

        IActivityBinder<TInstance> activityBinder = new CatchActivityBinder<TInstance, T>(Event, binder);

        return new TriggerEventActivityBinder<TInstance>(_machine, Event, _filter, _activities, activityBinder);
    }

    /// <summary>Retries the configured operation.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <param name="activityCallback">The activity callback.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public IEventActivityBinder<TInstance> Retry(Action<IRetryConfigurator> configure,
        Func<IEventActivityBinder<TInstance>, IEventActivityBinder<TInstance>> activityCallback)
    {
        var configurator = new BehaviorContextRetryConfigurator();
        configure(configurator);

        if (configurator.PolicyFactory == null)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Saga", "unknown", "A retry policy must be specified", "Correct the named configuration before starting the host"));

        IEventActivityBinder<TInstance> activityBinder = GetBinder(activityCallback);

        var retryPolicy = configurator.GetRetryPolicy();

        var binder = new RetryActivityBinder<TInstance>(Event, retryPolicy, activityBinder);

        return new TriggerEventActivityBinder<TInstance>(_machine, Event, _filter, _activities, binder);
    }

    IEventActivityBinder<TInstance> IEventActivityBinder<TInstance>.If(StateMachineCondition<TInstance> condition,
        Func<IEventActivityBinder<TInstance>, IEventActivityBinder<TInstance>> activityCallback)
    {
        return IfElse(condition, activityCallback, b => b);
    }

    IEventActivityBinder<TInstance> IEventActivityBinder<TInstance>.IfAwaited(StateMachineAsyncCondition<TInstance> condition,
        Func<IEventActivityBinder<TInstance>, IEventActivityBinder<TInstance>> activityCallback)
    {
        return IfElseAwaited(condition, activityCallback, b => b);
    }

    /// <summary>Adds conditional success and alternative branches.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="thenActivityCallback">The then activity callback.</param>
    /// <param name="elseActivityCallback">The else activity callback.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public IEventActivityBinder<TInstance> IfElse(StateMachineCondition<TInstance> condition,
        Func<IEventActivityBinder<TInstance>, IEventActivityBinder<TInstance>> thenActivityCallback,
        Func<IEventActivityBinder<TInstance>, IEventActivityBinder<TInstance>> elseActivityCallback)
    {
        IEventActivityBinder<TInstance> thenBinder = GetBinder(thenActivityCallback);
        IEventActivityBinder<TInstance> elseBinder = GetBinder(elseActivityCallback);

        var conditionBinder = new ConditionalActivityBinder<TInstance>(Event, condition, thenBinder, elseBinder);

        return new TriggerEventActivityBinder<TInstance>(_machine, Event, _filter, _activities, conditionBinder);
    }

    /// <summary>Adds conditional success and alternative branches.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="thenActivityCallback">The then activity callback.</param>
    /// <param name="elseActivityCallback">The else activity callback.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public IEventActivityBinder<TInstance> IfElseAwaited(StateMachineAsyncCondition<TInstance> condition,
        Func<IEventActivityBinder<TInstance>, IEventActivityBinder<TInstance>> thenActivityCallback,
        Func<IEventActivityBinder<TInstance>, IEventActivityBinder<TInstance>> elseActivityCallback)
    {
        IEventActivityBinder<TInstance> thenBinder = GetBinder(thenActivityCallback);
        IEventActivityBinder<TInstance> elseBinder = GetBinder(elseActivityCallback);

        var conditionBinder = new ConditionalActivityBinder<TInstance>(Event, condition, thenBinder, elseBinder);

        return new TriggerEventActivityBinder<TInstance>(_machine, Event, _filter, _activities, conditionBinder);
    }

    IStateMachine<TInstance> IEventActivityBinder<TInstance>.StateMachine => _machine;

    /// <summary>Gets state activity binders.</summary>
    /// <returns>The state activity binders.</returns>
    public IEnumerable<IActivityBinder<TInstance>> GetStateActivityBinders()
    {
        if (_filter != null)
            return Enumerable.Repeat(CreateConditionalActivityBinder(), 1);

        return _activities;
    }

    IEventActivityBinder<TInstance> GetBinder(Func<IEventActivityBinder<TInstance>, IEventActivityBinder<TInstance>> activityCallback)
    {
        IEventActivityBinder<TInstance> binder = new TriggerEventActivityBinder<TInstance>(_machine, Event);
        return activityCallback(binder);
    }

    IActivityBinder<TInstance> CreateConditionalActivityBinder()
    {
        IEventActivityBinder<TInstance> thenBinder = new TriggerEventActivityBinder<TInstance>(_machine, Event, _activities);
        IEventActivityBinder<TInstance> elseBinder = new TriggerEventActivityBinder<TInstance>(_machine, Event);

        var filter = _filter ?? throw new InvalidOperationException("A conditional activity requires a filter.");
        var conditionBinder = new ConditionalActivityBinder<TInstance>(Event, context => filter(context), thenBinder, elseBinder);

        return conditionBinder;
    }
}
