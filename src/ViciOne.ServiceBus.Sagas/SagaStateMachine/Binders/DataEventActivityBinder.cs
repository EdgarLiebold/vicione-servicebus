using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Binds data event activity activities to the pipeline.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
/// <typeparam name="TData">The data type.</typeparam>
public class DataEventActivityBinder<TInstance, TData> :
    IEventActivityBinder<TInstance, TData>
    where TInstance : class, ISagaStateMachineInstance
    where TData : class
{
    readonly IActivityBinder<TInstance>[] _activities;
    readonly IEvent<TData> _event;
    readonly StateMachineCondition<TInstance, TData>? _filter = null!;
    readonly IStateMachine<TInstance> _machine;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="machine">The machine.</param>
    /// <param name="event">The event.</param>
    /// <param name="activities">The activities.</param>
    public DataEventActivityBinder(IStateMachine<TInstance> machine, IEvent<TData> @event, params IActivityBinder<TInstance>[] activities)
    {
        _event = @event ?? throw new ArgumentNullException(nameof(@event));
        _activities = activities ?? [];
        _machine = machine;
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="machine">The machine.</param>
    /// <param name="event">The event.</param>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <param name="activities">The activities.</param>
    public DataEventActivityBinder(IStateMachine<TInstance> machine, IEvent<TData> @event, StateMachineCondition<TInstance, TData>? filter,
        params IActivityBinder<TInstance>[] activities)
    {
        _event = @event ?? throw new ArgumentNullException(nameof(@event));
        _activities = activities ?? [];
        _machine = machine;
        _filter = filter;
    }

    DataEventActivityBinder(IStateMachine<TInstance> machine, IEvent<TData> @event, StateMachineCondition<TInstance, TData>? filter,
        IActivityBinder<TInstance>[] activities, params IActivityBinder<TInstance>[] appendActivity)
    {
        _activities = new IActivityBinder<TInstance>[activities.Length + appendActivity.Length];
        Array.Copy(activities, 0, _activities, 0, activities.Length);
        Array.Copy(appendActivity, 0, _activities, activities.Length, appendActivity.Length);

        _event = @event;
        _machine = machine;
        _filter = filter;
    }

    IEvent<TData> IEventActivityBinder<TInstance, TData>.Event => _event;

    IEventActivityBinder<TInstance, TData> IEventActivityBinder<TInstance, TData>.Add(IStateMachineActivity<TInstance> activity)
    {
        return new DataEventActivityBinder<TInstance, TData>(_machine, _event, _filter, _activities,
            CreateStateActivityBinder(new SlimActivity<TInstance, TData>(activity)));
    }

    IEventActivityBinder<TInstance, TData> IEventActivityBinder<TInstance, TData>.Add(IStateMachineActivity<TInstance, TData> activity)
    {
        return new DataEventActivityBinder<TInstance, TData>(_machine, _event, _filter, _activities, CreateStateActivityBinder(activity));
    }

    IEventActivityBinder<TInstance, TData> IEventActivityBinder<TInstance, TData>.Catch<T>(
        Func<IExceptionActivityBinder<TInstance, TData, T>, IExceptionActivityBinder<TInstance, TData, T>> activityCallback)
    {
        IExceptionActivityBinder<TInstance, TData, T> binder = new CatchExceptionActivityBinder<TInstance, TData, T>(_machine, _event);

        binder = activityCallback(binder);

        IActivityBinder<TInstance> activityBinder = new CatchActivityBinder<TInstance, T>(_event, binder);

        return new DataEventActivityBinder<TInstance, TData>(_machine, _event, _filter, _activities, activityBinder);
    }

    /// <summary>Retries the configured operation.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <param name="activityCallback">The activity callback.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public IEventActivityBinder<TInstance, TData> Retry(Action<IRetryConfigurator> configure,
        Func<IEventActivityBinder<TInstance, TData>, IEventActivityBinder<TInstance, TData>> activityCallback)
    {
        var configurator = new BehaviorContextRetryConfigurator();
        configure(configurator);

        if (configurator.PolicyFactory == null)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Saga", "unknown", "A retry policy must be specified", "Correct the named configuration before starting the host"));

        IEventActivityBinder<TInstance, TData> activityBinder = GetBinder(activityCallback);

        var retryPolicy = configurator.GetRetryPolicy();

        var binder = new RetryActivityBinder<TInstance, TData>(_event, retryPolicy, activityBinder);

        return new DataEventActivityBinder<TInstance, TData>(_machine, _event, _filter, _activities, binder);
    }

    IEventActivityBinder<TInstance, TData> IEventActivityBinder<TInstance, TData>.If(StateMachineCondition<TInstance, TData> condition,
        Func<IEventActivityBinder<TInstance, TData>, IEventActivityBinder<TInstance, TData>> activityCallback)
    {
        return IfElse(condition, activityCallback, b => b);
    }

    IEventActivityBinder<TInstance, TData> IEventActivityBinder<TInstance, TData>.IfAwaited(StateMachineAsyncCondition<TInstance, TData> condition,
        Func<IEventActivityBinder<TInstance, TData>, IEventActivityBinder<TInstance, TData>> activityCallback)
    {
        return IfElseAwaited(condition, activityCallback, b => b);
    }

    /// <summary>Adds conditional success and alternative branches.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="thenActivityCallback">The then activity callback.</param>
    /// <param name="elseActivityCallback">The else activity callback.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public IEventActivityBinder<TInstance, TData> IfElse(StateMachineCondition<TInstance, TData> condition,
        Func<IEventActivityBinder<TInstance, TData>, IEventActivityBinder<TInstance, TData>> thenActivityCallback,
        Func<IEventActivityBinder<TInstance, TData>, IEventActivityBinder<TInstance, TData>> elseActivityCallback)
    {
        IEventActivityBinder<TInstance, TData> thenBinder = GetBinder(thenActivityCallback);
        IEventActivityBinder<TInstance, TData> elseBinder = GetBinder(elseActivityCallback);

        var conditionBinder = new ConditionalActivityBinder<TInstance, TData>(_event, condition, thenBinder, elseBinder);

        return new DataEventActivityBinder<TInstance, TData>(_machine, _event, _filter, _activities, conditionBinder);
    }

    /// <summary>Adds conditional success and alternative branches.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="thenActivityCallback">The then activity callback.</param>
    /// <param name="elseActivityCallback">The else activity callback.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public IEventActivityBinder<TInstance, TData> IfElseAwaited(StateMachineAsyncCondition<TInstance, TData> condition,
        Func<IEventActivityBinder<TInstance, TData>, IEventActivityBinder<TInstance, TData>> thenActivityCallback,
        Func<IEventActivityBinder<TInstance, TData>, IEventActivityBinder<TInstance, TData>> elseActivityCallback)
    {
        IEventActivityBinder<TInstance, TData> thenBinder = GetBinder(thenActivityCallback);
        IEventActivityBinder<TInstance, TData> elseBinder = GetBinder(elseActivityCallback);

        var conditionBinder = new ConditionalActivityBinder<TInstance, TData>(_event, condition, thenBinder, elseBinder);

        return new DataEventActivityBinder<TInstance, TData>(_machine, _event, _filter, _activities, conditionBinder);
    }

    IStateMachine<TInstance> IEventActivityBinder<TInstance, TData>.StateMachine => _machine;

    /// <summary>Gets state activity binders.</summary>
    /// <returns>The state activity binders.</returns>
    public IEnumerable<IActivityBinder<TInstance>> GetStateActivityBinders()
    {
        if (_filter != null)
            return Enumerable.Repeat(CreateConditionalActivityBinder(), 1);

        return _activities;
    }

    IEventActivityBinder<TInstance, TData> GetBinder(Func<IEventActivityBinder<TInstance, TData>, IEventActivityBinder<TInstance, TData>> activityCallback)
    {
        IEventActivityBinder<TInstance, TData> binder = new DataEventActivityBinder<TInstance, TData>(_machine, _event);

        return activityCallback(binder);
    }

    IActivityBinder<TInstance> CreateStateActivityBinder(IStateMachineActivity<TInstance, TData> activity)
    {
        var converterActivity = new DataConverterActivity<TInstance, TData>(activity);

        return new ExecuteActivityBinder<TInstance>(_event, converterActivity);
    }

    IActivityBinder<TInstance> CreateConditionalActivityBinder()
    {
        IEventActivityBinder<TInstance, TData> thenBinder = new DataEventActivityBinder<TInstance, TData>(_machine, _event, _activities);
        IEventActivityBinder<TInstance, TData> elseBinder = new DataEventActivityBinder<TInstance, TData>(_machine, _event);

        var filter = _filter ?? throw new InvalidOperationException("A conditional activity requires a filter.");
        return new ConditionalActivityBinder<TInstance, TData>(_event, context => filter(context), thenBinder, elseBinder);
    }
}
