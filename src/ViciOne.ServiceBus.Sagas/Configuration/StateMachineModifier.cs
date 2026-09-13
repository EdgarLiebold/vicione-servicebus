using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ViciOne.ServiceBus.Configuration;

class StateMachineModifier<TSaga> :
    IStateMachineModifier<TSaga>
    where TSaga : class, ISagaStateMachineInstance
{
    readonly List<IStateMachineEventActivitiesBuilder<TSaga>> _activityBuilders;
    readonly ViciOneServiceBusStateMachine<TSaga> _machine;

    public StateMachineModifier(ViciOneServiceBusStateMachine<TSaga> machine)
    {
        _machine = machine ?? throw new ArgumentNullException(nameof(machine));
        _activityBuilders = new List<IStateMachineEventActivitiesBuilder<TSaga>>();
    }

    public IState Initial => _machine.Initial;
    public IState Final => _machine.Final;

    public void Apply()
    {
        IStateMachineEventActivitiesBuilder<TSaga>[] uncommittedActivities = _activityBuilders
            .Where(builder => !builder.IsCommitted)
            .ToArray();

        foreach (IStateMachineEventActivitiesBuilder<TSaga> builder in uncommittedActivities)
            builder.CommitActivities();
    }

    public IStateMachineEventActivitiesBuilder<TSaga> During(params IState[] states)
    {
        var builder = new StateMachineEventActivitiesBuilder<TSaga>(_machine, this, activities => _machine.During(states, activities));
        _activityBuilders.Add(builder);
        return builder;
    }

    public IStateMachineEventActivitiesBuilder<TSaga> DuringAny()
    {
        var builder = new StateMachineEventActivitiesBuilder<TSaga>(_machine, this, activities => _machine.DuringAny(activities));
        _activityBuilders.Add(builder);
        return builder;
    }

    public IStateMachineEventActivitiesBuilder<TSaga> Initially()
    {
        var builder = new StateMachineEventActivitiesBuilder<TSaga>(_machine, this, activities => _machine.Initially(activities));
        _activityBuilders.Add(builder);
        return builder;
    }

    public IStateMachineModifier<TSaga> AfterLeave(IState state,
        Func<IEventActivityBinder<TSaga, IState>, IEventActivityBinder<TSaga, IState>> activityCallback)
    {
        _machine.AfterLeave(state, activityCallback);
        return this;
    }

    public IStateMachineModifier<TSaga> AfterLeaveAny(Func<IEventActivityBinder<TSaga, IState>, IEventActivityBinder<TSaga, IState>> activityCallback)
    {
        _machine.AfterLeaveAny(activityCallback);
        return this;
    }

    public IStateMachineModifier<TSaga> BeforeEnter(IState state,
        Func<IEventActivityBinder<TSaga, IState>, IEventActivityBinder<TSaga, IState>> activityCallback)
    {
        _machine.BeforeEnter(state, activityCallback);
        return this;
    }

    public IStateMachineModifier<TSaga> BeforeEnterAny(Func<IEventActivityBinder<TSaga, IState>, IEventActivityBinder<TSaga, IState>> activityCallback)
    {
        _machine.BeforeEnterAny(activityCallback);
        return this;
    }

    public IStateMachineModifier<TSaga> CompositeEvent(string name, out IEvent @event,
        Expression<Func<TSaga, CompositeEventStatus>> trackingPropertyExpression, params IEvent[] events)
    {
        Event(name, out @event);
        _machine.CompositeEvent(@event, trackingPropertyExpression, events);
        return this;
    }

    public IStateMachineModifier<TSaga> CompositeEvent(string name, out IEvent @event,
        Expression<Func<TSaga, CompositeEventStatus>> trackingPropertyExpression, CompositeEventOptions options,
        params IEvent[] events)
    {
        Event(name, out @event);
        _machine.CompositeEvent(@event, trackingPropertyExpression, options, events);
        return this;
    }

    public IStateMachineModifier<TSaga> CompositeEvent(string name, out IEvent @event, Expression<Func<TSaga, int>> trackingPropertyExpression,
        params IEvent[] events)
    {
        Event(name, out @event);
        _machine.CompositeEvent(@event, trackingPropertyExpression, events);
        return this;
    }

    public IStateMachineModifier<TSaga> CompositeEvent(string name, out IEvent @event, Expression<Func<TSaga, int>> trackingPropertyExpression,
        CompositeEventOptions options, params IEvent[] events)
    {
        Event(name, out @event);
        _machine.CompositeEvent(@event, trackingPropertyExpression, options, events);
        return this;
    }

    public IStateMachineModifier<TSaga> Event(string name, out IEvent @event)
    {
        @event = _machine.Event(name);
        return this;
    }

    public IStateMachineModifier<TSaga> Event<T>(string name, out IEvent<T> @event)
        where T : class
    {
        @event = _machine.Event<T>(name);
        return this;
    }

    public IStateMachineModifier<TSaga> Event<T>(string name, Action<IEventCorrelationConfigurator<TSaga, T>> configure, out IEvent<T> @event)
        where T : class
    {
        @event = _machine.Event(name, configure);
        return this;
    }

    public IStateMachineModifier<TSaga> Event<TProperty, T>(Expression<Func<TProperty>> propertyExpression,
        Expression<Func<TProperty, IEvent<T>>> eventPropertyExpression)
        where TProperty : class
        where T : class
    {
        _machine.Event(propertyExpression, eventPropertyExpression);
        return this;
    }

    public IStateMachineModifier<TSaga> Finally(Func<IEventActivityBinder<TSaga>, IEventActivityBinder<TSaga>> activityCallback)
    {
        _machine.Finally(activityCallback);
        return this;
    }

    public IStateMachineModifier<TSaga> InstanceState(Expression<Func<TSaga, IState?>> instanceStateProperty)
    {
        _machine.InstanceState(instanceStateProperty);
        return this;
    }

    public IStateMachineModifier<TSaga> InstanceState(Expression<Func<TSaga, string>> instanceStateProperty)
    {
        _machine.InstanceState(instanceStateProperty);
        return this;
    }

    public IStateMachineModifier<TSaga> InstanceState(Expression<Func<TSaga, int>> instanceStateProperty, params IState[] states)
    {
        _machine.InstanceState(instanceStateProperty, states);
        return this;
    }

    public IStateMachineModifier<TSaga> Name(string machineName)
    {
        _machine.Name(machineName);
        return this;
    }

    public IStateMachineModifier<TSaga> OnUnhandledEvent(UnhandledEventCallback<TSaga> callback)
    {
        _machine.OnUnhandledEvent(callback);
        return this;
    }

    public IStateMachineModifier<TSaga> State(string name, out IState<TSaga> state)
    {
        state = _machine.State(name);
        return this;
    }

    public IStateMachineModifier<TSaga> State(string name, out IState state)
    {
        state = _machine.State(name);
        return this;
    }

    public IStateMachineModifier<TSaga> State<TProperty>(Expression<Func<TProperty>> propertyExpression,
        Expression<Func<TProperty, IState>> statePropertyExpression)
        where TProperty : class
    {
        _machine.State(propertyExpression, statePropertyExpression);
        return this;
    }

    public IStateMachineModifier<TSaga> SubState(string name, IState superState, out IState<TSaga> subState)
    {
        subState = _machine.SubState(name, superState);
        return this;
    }

    public IStateMachineModifier<TSaga> SubState<TProperty>(Expression<Func<TProperty>> propertyExpression,
        Expression<Func<TProperty, IState>> statePropertyExpression, IState superState)
        where TProperty : class
    {
        _machine.SubState(propertyExpression, statePropertyExpression, superState);
        return this;
    }

    public IStateMachineModifier<TSaga> WhenEnter(IState state,
        Func<IEventActivityBinder<TSaga>, IEventActivityBinder<TSaga>> activityCallback)
    {
        _machine.WhenEnter(state, activityCallback);
        return this;
    }

    public IStateMachineModifier<TSaga> WhenEnterAny(Func<IEventActivityBinder<TSaga>, IEventActivityBinder<TSaga>> activityCallback)
    {
        _machine.WhenEnterAny(activityCallback);
        return this;
    }

    public IStateMachineModifier<TSaga> WhenLeave(IState state,
        Func<IEventActivityBinder<TSaga>, IEventActivityBinder<TSaga>> activityCallback)
    {
        _machine.WhenLeave(state, activityCallback);
        return this;
    }

    public IStateMachineModifier<TSaga> WhenLeaveAny(Func<IEventActivityBinder<TSaga>, IEventActivityBinder<TSaga>> activityCallback)
    {
        _machine.WhenLeaveAny(activityCallback);
        return this;
    }

    public IStateMachineModifier<TSaga> InstanceState(Expression<Func<TSaga, int>> instanceStateProperty,
        params string[] stateNames)
    {
        // State replacement operates only on declarations already present in the machine graph.
        IState<TSaga>[] states = stateNames
            .Select(name => _machine.GetState(name))
            .ToArray();

        _machine.InstanceState(instanceStateProperty, states.Cast<IState>().ToArray());
        return this;
    }
}
