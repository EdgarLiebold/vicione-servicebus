using System;
using System.Linq.Expressions;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Defines the operations required by state machine modifier.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface IStateMachineModifier<TSaga>
    where TSaga : class, ISagaStateMachineInstance
{
    /// <summary>Gets the initial.</summary>
    IState Initial { get; }
    /// <summary>Gets the final.</summary>
    IState Final { get; }

    /// <summary>Configures the saga instance-state property.</summary>
    /// <param name="instanceStateProperty">The instance state property.</param>
    /// <returns>The state machine modifier produced by the operation.</returns>
    IStateMachineModifier<TSaga> InstanceState(Expression<Func<TSaga, IState?>> instanceStateProperty);
    /// <summary>Configures the saga instance-state property.</summary>
    /// <param name="instanceStateProperty">The instance state property.</param>
    /// <returns>The state machine modifier produced by the operation.</returns>
    IStateMachineModifier<TSaga> InstanceState(Expression<Func<TSaga, string>> instanceStateProperty);
    /// <summary>Configures the saga instance-state property.</summary>
    /// <param name="instanceStateProperty">The instance state property.</param>
    /// <param name="states">The states.</param>
    /// <returns>The state machine modifier produced by the operation.</returns>
    IStateMachineModifier<TSaga> InstanceState(Expression<Func<TSaga, int>> instanceStateProperty, params IState[] states);
    /// <summary>Configures the entity name.</summary>
    /// <param name="machineName">The machine name.</param>
    /// <returns>The state machine modifier produced by the operation.</returns>
    IStateMachineModifier<TSaga> Name(string machineName);
    /// <summary>Applies the event configuration.</summary>
    /// <param name="name">The name.</param>
    /// <param name="event">Receives the event produced by the operation.</param>
    /// <returns>The state machine modifier produced by the operation.</returns>
    IStateMachineModifier<TSaga> Event(string name, out IEvent @event);

    /// <summary>Applies the event configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="name">The name.</param>
    /// <param name="event">Receives the event produced by the operation.</param>
    /// <returns>The state machine modifier produced by the operation.</returns>
    IStateMachineModifier<TSaga> Event<T>(string name, out IEvent<T> @event)
        where T : class;

    /// <summary>Applies the event configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="name">The name.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <param name="event">Receives the event produced by the operation.</param>
    /// <returns>The state machine modifier produced by the operation.</returns>
    IStateMachineModifier<TSaga> Event<T>(string name, Action<IEventCorrelationConfigurator<TSaga, T>> configure, out IEvent<T> @event)
        where T : class;

    /// <summary>Applies the event configuration.</summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="propertyExpression">The property expression.</param>
    /// <param name="eventPropertyExpression">The event property expression.</param>
    /// <returns>The state machine modifier produced by the operation.</returns>
    IStateMachineModifier<TSaga> Event<TProperty, T>(Expression<Func<TProperty>> propertyExpression,
        Expression<Func<TProperty, IEvent<T>>> eventPropertyExpression)
        where TProperty : class
        where T : class;

    /// <summary>Configures the composite event.</summary>
    /// <param name="name">The name.</param>
    /// <param name="event">Receives the event produced by the operation.</param>
    /// <param name="trackingPropertyExpression">The tracking property expression.</param>
    /// <param name="events">The events.</param>
    /// <returns>The state machine modifier produced by the operation.</returns>
    IStateMachineModifier<TSaga> CompositeEvent(string name, out IEvent @event,
        Expression<Func<TSaga, CompositeEventStatus>> trackingPropertyExpression,
        params IEvent[] events);

    /// <summary>Configures the composite event.</summary>
    /// <param name="name">The name.</param>
    /// <param name="event">Receives the event produced by the operation.</param>
    /// <param name="trackingPropertyExpression">The tracking property expression.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="events">The events.</param>
    /// <returns>The state machine modifier produced by the operation.</returns>
    IStateMachineModifier<TSaga> CompositeEvent(string name, out IEvent @event,
        Expression<Func<TSaga, CompositeEventStatus>> trackingPropertyExpression,
        CompositeEventOptions options,
        params IEvent[] events);

    /// <summary>Configures the composite event.</summary>
    /// <param name="name">The name.</param>
    /// <param name="event">Receives the event produced by the operation.</param>
    /// <param name="trackingPropertyExpression">The tracking property expression.</param>
    /// <param name="events">The events.</param>
    /// <returns>The state machine modifier produced by the operation.</returns>
    IStateMachineModifier<TSaga> CompositeEvent(string name, out IEvent @event,
        Expression<Func<TSaga, int>> trackingPropertyExpression,
        params IEvent[] events);

    /// <summary>Configures the composite event.</summary>
    /// <param name="name">The name.</param>
    /// <param name="event">Receives the event produced by the operation.</param>
    /// <param name="trackingPropertyExpression">The tracking property expression.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="events">The events.</param>
    /// <returns>The state machine modifier produced by the operation.</returns>
    IStateMachineModifier<TSaga> CompositeEvent(string name, out IEvent @event,
        Expression<Func<TSaga, int>> trackingPropertyExpression,
        CompositeEventOptions options,
        params IEvent[] events);

    /// <summary>Applies the state configuration.</summary>
    /// <param name="name">The name.</param>
    /// <param name="state">Receives the state produced by the operation.</param>
    /// <returns>The state machine modifier produced by the operation.</returns>
    IStateMachineModifier<TSaga> State(string name, out IState<TSaga> state);
    /// <summary>Applies the state configuration.</summary>
    /// <param name="name">The name.</param>
    /// <param name="state">Receives the state produced by the operation.</param>
    /// <returns>The state machine modifier produced by the operation.</returns>
    IStateMachineModifier<TSaga> State(string name, out IState state);

    /// <summary>Applies the state configuration.</summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertyExpression">The property expression.</param>
    /// <param name="statePropertyExpression">The state property expression.</param>
    /// <returns>The state machine modifier produced by the operation.</returns>
    IStateMachineModifier<TSaga> State<TProperty>(Expression<Func<TProperty>> propertyExpression,
        Expression<Func<TProperty, IState>> statePropertyExpression)
        where TProperty : class;

    /// <summary>Configures a state-machine substate.</summary>
    /// <param name="name">The name.</param>
    /// <param name="superState">The super state.</param>
    /// <param name="subState">Receives the sub state produced by the operation.</param>
    /// <returns>The state machine modifier produced by the operation.</returns>
    IStateMachineModifier<TSaga> SubState(string name, IState superState, out IState<TSaga> subState);

    /// <summary>Configures a state-machine substate.</summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertyExpression">The property expression.</param>
    /// <param name="statePropertyExpression">The state property expression.</param>
    /// <param name="superState">The super state.</param>
    /// <returns>The state machine modifier produced by the operation.</returns>
    IStateMachineModifier<TSaga> SubState<TProperty>(Expression<Func<TProperty>> propertyExpression,
        Expression<Func<TProperty, IState>> statePropertyExpression, IState superState)
        where TProperty : class;

    /// <summary>Adds behavior for the selected state.</summary>
    /// <param name="states">The states.</param>
    /// <returns>The state machine event activities builder produced by the operation.</returns>
    IStateMachineEventActivitiesBuilder<TSaga> During(params IState[] states);
    /// <summary>Configures the initial state-machine behavior.</summary>
    /// <returns>The state machine event activities builder produced by the operation.</returns>
    IStateMachineEventActivitiesBuilder<TSaga> Initially();
    /// <summary>Adds behavior that runs in any state.</summary>
    /// <returns>The state machine event activities builder produced by the operation.</returns>
    IStateMachineEventActivitiesBuilder<TSaga> DuringAny();
    /// <summary>Configures activities for the final state's Enter event using the machine's any-state binding rules.</summary>
    /// <param name="activityCallback">The activity callback.</param>
    /// <returns>The state machine modifier produced by the operation.</returns>
    IStateMachineModifier<TSaga> Finally(Func<IEventActivityBinder<TSaga>, IEventActivityBinder<TSaga>> activityCallback);

    /// <summary>Adds behavior that runs when entering the state.</summary>
    /// <param name="state">The state.</param>
    /// <param name="activityCallback">The activity callback.</param>
    /// <returns>The state machine modifier produced by the operation.</returns>
    IStateMachineModifier<TSaga> WhenEnter(IState state,
        Func<IEventActivityBinder<TSaga>, IEventActivityBinder<TSaga>> activityCallback);

    /// <summary>Adds behavior that runs when entering any state.</summary>
    /// <param name="activityCallback">The activity callback.</param>
    /// <returns>The state machine modifier produced by the operation.</returns>
    IStateMachineModifier<TSaga> WhenEnterAny(Func<IEventActivityBinder<TSaga>, IEventActivityBinder<TSaga>> activityCallback);
    /// <summary>Adds behavior that runs when leaving any state.</summary>
    /// <param name="activityCallback">The activity callback.</param>
    /// <returns>The state machine modifier produced by the operation.</returns>
    IStateMachineModifier<TSaga> WhenLeaveAny(Func<IEventActivityBinder<TSaga>, IEventActivityBinder<TSaga>> activityCallback);

    /// <summary>Adds behavior that runs before entering any state.</summary>
    /// <param name="activityCallback">The activity callback.</param>
    /// <returns>The state machine modifier produced by the operation.</returns>
    IStateMachineModifier<TSaga> BeforeEnterAny(Func<IEventActivityBinder<TSaga, IState>, IEventActivityBinder<TSaga, IState>> activityCallback);

    /// <summary>Adds behavior that runs after leaving any state.</summary>
    /// <param name="activityCallback">The activity callback.</param>
    /// <returns>The state machine modifier produced by the operation.</returns>
    IStateMachineModifier<TSaga> AfterLeaveAny(Func<IEventActivityBinder<TSaga, IState>, IEventActivityBinder<TSaga, IState>> activityCallback);

    /// <summary>Adds behavior that runs when leaving the state.</summary>
    /// <param name="state">The state.</param>
    /// <param name="activityCallback">The activity callback.</param>
    /// <returns>The state machine modifier produced by the operation.</returns>
    IStateMachineModifier<TSaga> WhenLeave(IState state,
        Func<IEventActivityBinder<TSaga>, IEventActivityBinder<TSaga>> activityCallback);

    /// <summary>Adds behavior that runs before entering the state.</summary>
    /// <param name="state">The state.</param>
    /// <param name="activityCallback">The activity callback.</param>
    /// <returns>The state machine modifier produced by the operation.</returns>
    IStateMachineModifier<TSaga> BeforeEnter(IState state,
        Func<IEventActivityBinder<TSaga, IState>, IEventActivityBinder<TSaga, IState>> activityCallback);

    /// <summary>Adds behavior that runs after leaving the state.</summary>
    /// <param name="state">The state.</param>
    /// <param name="activityCallback">The activity callback.</param>
    /// <returns>The state machine modifier produced by the operation.</returns>
    IStateMachineModifier<TSaga> AfterLeave(IState state,
        Func<IEventActivityBinder<TSaga, IState>, IEventActivityBinder<TSaga, IState>> activityCallback);

    /// <summary>Handles the notification for unhandled event.</summary>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The state machine modifier produced by the operation.</returns>
    IStateMachineModifier<TSaga> OnUnhandledEvent(UnhandledEventCallback<TSaga> callback);

    /// <summary>Applies this specification to the target builder.</summary>
    void Apply();
}
