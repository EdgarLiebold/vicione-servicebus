using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ViciOne.ServiceBus.Configuration;

class StateMachineEventActivitiesBuilder<TInstance> :
    IStateMachineEventActivitiesBuilder<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    readonly List<IEventActivities<TInstance>> _activities;
    readonly Action<IEventActivities<TInstance>[]> _committer;
    readonly ViciOneServiceBusStateMachine<TInstance> _machine;
    readonly IStateMachineModifier<TInstance> _modifier;

    public StateMachineEventActivitiesBuilder(ViciOneServiceBusStateMachine<TInstance> machine,
        IStateMachineModifier<TInstance> modifier, Action<IEventActivities<TInstance>[]> committer)
    {
        _machine = machine ?? throw new ArgumentNullException(nameof(machine));
        _modifier = modifier ?? throw new ArgumentNullException(nameof(modifier));
        _committer = committer ?? throw new ArgumentNullException(nameof(committer));
        _activities = new List<IEventActivities<TInstance>>();
        IsCommitted = false;
    }

    public bool IsCommitted { get; private set; }

    public IState Initial => _modifier.Initial;
    public IState Final => _modifier.Final;

    public IStateMachineModifier<TInstance> CommitActivities()
    {
        if (IsCommitted)
            return _modifier;

        _committer(_activities.ToArray());
        IsCommitted = true;
        return _modifier;
    }

    public IStateMachineEventActivitiesBuilder<TInstance> When(IEvent @event,
        Func<IEventActivityBinder<TInstance>, IEventActivityBinder<TInstance>> configure)
    {
        EnsureNotCommitted();
        ArgumentNullException.ThrowIfNull(@event, nameof(@event));
        ArgumentNullException.ThrowIfNull(configure);

        AddActivity(configure(_machine.When(@event)));
        return this;
    }

    public IStateMachineEventActivitiesBuilder<TInstance> When(IEvent @event, StateMachineCondition<TInstance> filter,
        Func<IEventActivityBinder<TInstance>, IEventActivityBinder<TInstance>> configure)
    {
        EnsureNotCommitted();
        ArgumentNullException.ThrowIfNull(@event, nameof(@event));
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(configure);

        AddActivity(configure(_machine.When(@event, filter)));
        return this;
    }

    public IStateMachineEventActivitiesBuilder<TInstance> When<TData>(IEvent<TData> @event,
        Func<IEventActivityBinder<TInstance, TData>, IEventActivityBinder<TInstance, TData>> configure)
        where TData : class
    {
        EnsureNotCommitted();
        ArgumentNullException.ThrowIfNull(@event, nameof(@event));
        ArgumentNullException.ThrowIfNull(configure);

        AddActivity(configure(_machine.When(@event)));
        return this;
    }

    public IStateMachineEventActivitiesBuilder<TInstance> When<TData>(IEvent<TData> @event,
        StateMachineCondition<TInstance, TData> filter,
        Func<IEventActivityBinder<TInstance, TData>, IEventActivityBinder<TInstance, TData>> configure)
        where TData : class
    {
        EnsureNotCommitted();
        ArgumentNullException.ThrowIfNull(@event, nameof(@event));
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(configure);

        AddActivity(configure(_machine.When(@event, filter)));
        return this;
    }

    public IStateMachineEventActivitiesBuilder<TInstance> Ignore(IEvent @event)
    {
        EnsureNotCommitted();
        ArgumentNullException.ThrowIfNull(@event, nameof(@event));

        AddActivity(_machine.Ignore(@event));
        return this;
    }

    public IStateMachineEventActivitiesBuilder<TInstance> Ignore<TData>(IEvent<TData> @event)
        where TData : class
    {
        EnsureNotCommitted();
        ArgumentNullException.ThrowIfNull(@event, nameof(@event));

        AddActivity(_machine.Ignore(@event));
        return this;
    }

    public IStateMachineEventActivitiesBuilder<TInstance> Ignore<TData>(IEvent<TData> @event,
        StateMachineCondition<TInstance, TData> filter)
        where TData : class
    {
        EnsureNotCommitted();
        ArgumentNullException.ThrowIfNull(@event, nameof(@event));
        ArgumentNullException.ThrowIfNull(filter);

        AddActivity(_machine.Ignore(@event, filter));
        return this;
    }

    public void Apply()
    {
        CommitActivities().Apply();
    }

    public IStateMachineModifier<TInstance> AfterLeave(IState state,
        Func<IEventActivityBinder<TInstance, IState>, IEventActivityBinder<TInstance, IState>> activityCallback)
    {
        return CommitActivities().AfterLeave(state, activityCallback);
    }

    public IStateMachineModifier<TInstance> AfterLeaveAny(
        Func<IEventActivityBinder<TInstance, IState>, IEventActivityBinder<TInstance, IState>> activityCallback)
    {
        return CommitActivities().AfterLeaveAny(activityCallback);
    }

    public IStateMachineModifier<TInstance> BeforeEnter(IState state,
        Func<IEventActivityBinder<TInstance, IState>, IEventActivityBinder<TInstance, IState>> activityCallback)
    {
        return CommitActivities().BeforeEnter(state, activityCallback);
    }

    public IStateMachineModifier<TInstance> BeforeEnterAny(
        Func<IEventActivityBinder<TInstance, IState>, IEventActivityBinder<TInstance, IState>> activityCallback)
    {
        return CommitActivities().BeforeEnterAny(activityCallback);
    }

    public IStateMachineModifier<TInstance> CompositeEvent(string name, out IEvent @event,
        Expression<Func<TInstance, CompositeEventStatus>> trackingPropertyExpression, params IEvent[] events)
    {
        return CommitActivities().CompositeEvent(name, out @event, trackingPropertyExpression, events);
    }

    public IStateMachineModifier<TInstance> CompositeEvent(string name, out IEvent @event,
        Expression<Func<TInstance, CompositeEventStatus>> trackingPropertyExpression, CompositeEventOptions options,
        params IEvent[] events)
    {
        return CommitActivities().CompositeEvent(name, out @event, trackingPropertyExpression, options, events);
    }

    public IStateMachineModifier<TInstance> CompositeEvent(string name, out IEvent @event, Expression<Func<TInstance, int>> trackingPropertyExpression,
        params IEvent[] events)
    {
        return CommitActivities().CompositeEvent(name, out @event, trackingPropertyExpression, events);
    }

    public IStateMachineModifier<TInstance> CompositeEvent(string name, out IEvent @event, Expression<Func<TInstance, int>> trackingPropertyExpression,
        CompositeEventOptions options, params IEvent[] events)
    {
        return CommitActivities().CompositeEvent(name, out @event, trackingPropertyExpression, options, events);
    }

    public IStateMachineEventActivitiesBuilder<TInstance> During(params IState[] states)
    {
        return CommitActivities().During(states);
    }

    public IStateMachineEventActivitiesBuilder<TInstance> DuringAny()
    {
        return CommitActivities().DuringAny();
    }

    public IStateMachineModifier<TInstance> Event(string name, out IEvent @event)
    {
        return CommitActivities().Event(name, out @event);
    }

    public IStateMachineModifier<TInstance> Event<T>(string name, out IEvent<T> @event)
        where T : class
    {
        return CommitActivities().Event(name, out @event);
    }

    public IStateMachineModifier<TInstance> Event<T>(string name, Action<IEventCorrelationConfigurator<TInstance, T>> configure, out IEvent<T> @event)
        where T : class
    {
        return CommitActivities().Event(name, configure, out @event);
    }

    public IStateMachineModifier<TInstance> Event<TProperty, T>(Expression<Func<TProperty>> propertyExpression,
        Expression<Func<TProperty, IEvent<T>>> eventPropertyExpression)
        where TProperty : class
        where T : class
    {
        return CommitActivities().Event(propertyExpression, eventPropertyExpression);
    }

    public IStateMachineModifier<TInstance> Finally(Func<IEventActivityBinder<TInstance>, IEventActivityBinder<TInstance>> activityCallback)
    {
        return CommitActivities().Finally(activityCallback);
    }

    public IStateMachineEventActivitiesBuilder<TInstance> Initially()
    {
        return CommitActivities().Initially();
    }

    public IStateMachineModifier<TInstance> InstanceState(Expression<Func<TInstance, IState?>> instanceStateProperty)
    {
        return CommitActivities().InstanceState(instanceStateProperty);
    }

    public IStateMachineModifier<TInstance> InstanceState(Expression<Func<TInstance, string>> instanceStateProperty)
    {
        return CommitActivities().InstanceState(instanceStateProperty);
    }

    public IStateMachineModifier<TInstance> InstanceState(Expression<Func<TInstance, int>> instanceStateProperty, params IState[] states)
    {
        return CommitActivities().InstanceState(instanceStateProperty, states);
    }

    public IStateMachineModifier<TInstance> Name(string machineName)
    {
        return CommitActivities().Name(machineName);
    }

    public IStateMachineModifier<TInstance> OnUnhandledEvent(UnhandledEventCallback<TInstance> callback)
    {
        return CommitActivities().OnUnhandledEvent(callback);
    }

    public IStateMachineModifier<TInstance> State(string name, out IState<TInstance> state)
    {
        return CommitActivities().State(name, out state);
    }

    public IStateMachineModifier<TInstance> State(string name, out IState state)
    {
        return CommitActivities().State(name, out state);
    }

    public IStateMachineModifier<TInstance> State<TProperty>(Expression<Func<TProperty>> propertyExpression,
        Expression<Func<TProperty, IState>> statePropertyExpression)
        where TProperty : class
    {
        return CommitActivities().State(propertyExpression, statePropertyExpression);
    }

    public IStateMachineModifier<TInstance> SubState(string name, IState superState, out IState<TInstance> subState)
    {
        return CommitActivities().SubState(name, superState, out subState);
    }

    public IStateMachineModifier<TInstance> SubState<TProperty>(Expression<Func<TProperty>> propertyExpression,
        Expression<Func<TProperty, IState>> statePropertyExpression, IState superState)
        where TProperty : class
    {
        return CommitActivities().SubState(propertyExpression, statePropertyExpression, superState);
    }

    public IStateMachineModifier<TInstance> WhenEnter(IState state,
        Func<IEventActivityBinder<TInstance>, IEventActivityBinder<TInstance>> activityCallback)
    {
        return CommitActivities().WhenEnter(state, activityCallback);
    }

    public IStateMachineModifier<TInstance> WhenEnterAny(Func<IEventActivityBinder<TInstance>, IEventActivityBinder<TInstance>> activityCallback)
    {
        return CommitActivities().WhenEnterAny(activityCallback);
    }

    public IStateMachineModifier<TInstance> WhenLeave(IState state,
        Func<IEventActivityBinder<TInstance>, IEventActivityBinder<TInstance>> activityCallback)
    {
        return CommitActivities().WhenLeave(state, activityCallback);
    }

    public IStateMachineModifier<TInstance> WhenLeaveAny(Func<IEventActivityBinder<TInstance>, IEventActivityBinder<TInstance>> activityCallback)
    {
        return CommitActivities().WhenLeaveAny(activityCallback);
    }

    void AddActivity(IEventActivities<TInstance>? activity)
    {
        if (activity == null)
            throw new InvalidOperationException("The event activity configuration callback returned null.");

        _activities.Add(activity);
    }

    void EnsureNotCommitted()
    {
        if (IsCommitted)
            throw new InvalidOperationException("The state machine event activities have already been committed.");
    }
}
