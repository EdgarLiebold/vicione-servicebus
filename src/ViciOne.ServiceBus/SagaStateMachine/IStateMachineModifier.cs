using System;
using System.Linq.Expressions;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// Defines the contract for state machine modifier.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public interface IStateMachineModifier<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    /// <summary>
    /// Gets the initial value.
    /// </summary>
    State Initial { get; }
    /// <summary>
    /// Gets the final value.
    /// </summary>
    State Final { get; }

    /// <summary>
    /// Performs the instance state operation.
    /// </summary>
    /// <param name="instanceStateProperty">The instance state property value.</param>
    /// <returns>The result of the operation.</returns>
    IStateMachineModifier<TSaga> InstanceState(Expression<Func<TSaga, State?>> instanceStateProperty);
    /// <summary>
    /// Performs the instance state operation.
    /// </summary>
    /// <param name="instanceStateProperty">The instance state property value.</param>
    /// <returns>The result of the operation.</returns>
    IStateMachineModifier<TSaga> InstanceState(Expression<Func<TSaga, string>> instanceStateProperty);
    /// <summary>
    /// Performs the instance state operation.
    /// </summary>
    /// <param name="instanceStateProperty">The instance state property value.</param>
    /// <param name="states">The states value.</param>
    /// <returns>The result of the operation.</returns>
    IStateMachineModifier<TSaga> InstanceState(Expression<Func<TSaga, int>> instanceStateProperty, params State[] states);
    /// <summary>
    /// Performs the name operation.
    /// </summary>
    /// <param name="machineName">The machine name value.</param>
    /// <returns>The result of the operation.</returns>
    IStateMachineModifier<TSaga> Name(string machineName);
    /// <summary>
    /// Performs the event operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="event">The event value.</param>
    /// <returns>The result of the operation.</returns>
    IStateMachineModifier<TSaga> Event(string name, out Event @event);

    /// <summary>
    /// Performs the event operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="name">The name value.</param>
    /// <param name="event">The event value.</param>
    /// <returns>The result of the operation.</returns>
    IStateMachineModifier<TSaga> Event<T>(string name, out Event<T> @event)
        where T : class;

    /// <summary>
    /// Performs the event operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="name">The name value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <param name="event">The event value.</param>
    /// <returns>The result of the operation.</returns>
    IStateMachineModifier<TSaga> Event<T>(string name, Action<IEventCorrelationConfigurator<TSaga, T>> configure, out Event<T> @event)
        where T : class;

    /// <summary>
    /// Performs the event operation.
    /// </summary>
    /// <typeparam name="TProperty">The t property type.</typeparam>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="propertyExpression">The property expression value.</param>
    /// <param name="eventPropertyExpression">The event property expression value.</param>
    /// <returns>The result of the operation.</returns>
    IStateMachineModifier<TSaga> Event<TProperty, T>(Expression<Func<TProperty>> propertyExpression,
        Expression<Func<TProperty, Event<T>>> eventPropertyExpression)
        where TProperty : class
        where T : class;

    /// <summary>
    /// Performs the composite event operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="event">The event value.</param>
    /// <param name="trackingPropertyExpression">The tracking property expression value.</param>
    /// <param name="events">The events value.</param>
    /// <returns>The result of the operation.</returns>
    IStateMachineModifier<TSaga> CompositeEvent(string name, out Event @event,
        Expression<Func<TSaga, CompositeEventStatus>> trackingPropertyExpression,
        params Event[] events);

    /// <summary>
    /// Performs the composite event operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="event">The event value.</param>
    /// <param name="trackingPropertyExpression">The tracking property expression value.</param>
    /// <param name="options">The options value.</param>
    /// <param name="events">The events value.</param>
    /// <returns>The result of the operation.</returns>
    IStateMachineModifier<TSaga> CompositeEvent(string name, out Event @event,
        Expression<Func<TSaga, CompositeEventStatus>> trackingPropertyExpression,
        CompositeEventOptions options,
        params Event[] events);

    /// <summary>
    /// Performs the composite event operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="event">The event value.</param>
    /// <param name="trackingPropertyExpression">The tracking property expression value.</param>
    /// <param name="events">The events value.</param>
    /// <returns>The result of the operation.</returns>
    IStateMachineModifier<TSaga> CompositeEvent(string name, out Event @event,
        Expression<Func<TSaga, int>> trackingPropertyExpression,
        params Event[] events);

    /// <summary>
    /// Performs the composite event operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="event">The event value.</param>
    /// <param name="trackingPropertyExpression">The tracking property expression value.</param>
    /// <param name="options">The options value.</param>
    /// <param name="events">The events value.</param>
    /// <returns>The result of the operation.</returns>
    IStateMachineModifier<TSaga> CompositeEvent(string name, out Event @event,
        Expression<Func<TSaga, int>> trackingPropertyExpression,
        CompositeEventOptions options,
        params Event[] events);

    /// <summary>
    /// Performs the state operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="state">The state value.</param>
    /// <returns>The result of the operation.</returns>
    IStateMachineModifier<TSaga> State(string name, out State<TSaga> state);
    /// <summary>
    /// Performs the state operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="state">The state value.</param>
    /// <returns>The result of the operation.</returns>
    IStateMachineModifier<TSaga> State(string name, out State state);

    /// <summary>
    /// Performs the state operation.
    /// </summary>
    /// <typeparam name="TProperty">The t property type.</typeparam>
    /// <param name="propertyExpression">The property expression value.</param>
    /// <param name="statePropertyExpression">The state property expression value.</param>
    /// <returns>The result of the operation.</returns>
    IStateMachineModifier<TSaga> State<TProperty>(Expression<Func<TProperty>> propertyExpression,
        Expression<Func<TProperty, State>> statePropertyExpression)
        where TProperty : class;

    /// <summary>
    /// Performs the sub state operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="superState">The super state value.</param>
    /// <param name="subState">The sub state value.</param>
    /// <returns>The result of the operation.</returns>
    IStateMachineModifier<TSaga> SubState(string name, State superState, out State<TSaga> subState);

    /// <summary>
    /// Performs the sub state operation.
    /// </summary>
    /// <typeparam name="TProperty">The t property type.</typeparam>
    /// <param name="propertyExpression">The property expression value.</param>
    /// <param name="statePropertyExpression">The state property expression value.</param>
    /// <param name="superState">The super state value.</param>
    /// <returns>The result of the operation.</returns>
    IStateMachineModifier<TSaga> SubState<TProperty>(Expression<Func<TProperty>> propertyExpression,
        Expression<Func<TProperty, State>> statePropertyExpression, State superState)
        where TProperty : class;

    /// <summary>
    /// Performs the during operation.
    /// </summary>
    /// <param name="states">The states value.</param>
    /// <returns>The result of the operation.</returns>
    IStateMachineEventActivitiesBuilder<TSaga> During(params State[] states);
    /// <summary>
    /// Performs the initially operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    IStateMachineEventActivitiesBuilder<TSaga> Initially();
    /// <summary>
    /// Performs the during any operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    IStateMachineEventActivitiesBuilder<TSaga> DuringAny();
    /// <summary>
    /// Performs the finally operation.
    /// </summary>
    /// <param name="activityCallback">The activity callback value.</param>
    /// <returns>The result of the operation.</returns>
    IStateMachineModifier<TSaga> Finally(Func<EventActivityBinder<TSaga>, EventActivityBinder<TSaga>> activityCallback);

    /// <summary>
    /// Performs the when enter operation.
    /// </summary>
    /// <param name="state">The state value.</param>
    /// <param name="activityCallback">The activity callback value.</param>
    /// <returns>The result of the operation.</returns>
    IStateMachineModifier<TSaga> WhenEnter(State state,
        Func<EventActivityBinder<TSaga>, EventActivityBinder<TSaga>> activityCallback);

    /// <summary>
    /// Performs the when enter any operation.
    /// </summary>
    /// <param name="activityCallback">The activity callback value.</param>
    /// <returns>The result of the operation.</returns>
    IStateMachineModifier<TSaga> WhenEnterAny(Func<EventActivityBinder<TSaga>, EventActivityBinder<TSaga>> activityCallback);
    /// <summary>
    /// Performs the when leave any operation.
    /// </summary>
    /// <param name="activityCallback">The activity callback value.</param>
    /// <returns>The result of the operation.</returns>
    IStateMachineModifier<TSaga> WhenLeaveAny(Func<EventActivityBinder<TSaga>, EventActivityBinder<TSaga>> activityCallback);

    /// <summary>
    /// Performs the before enter any operation.
    /// </summary>
    /// <param name="activityCallback">The activity callback value.</param>
    /// <returns>The result of the operation.</returns>
    IStateMachineModifier<TSaga> BeforeEnterAny(Func<EventActivityBinder<TSaga, State>, EventActivityBinder<TSaga, State>> activityCallback);

    /// <summary>
    /// Performs the after leave any operation.
    /// </summary>
    /// <param name="activityCallback">The activity callback value.</param>
    /// <returns>The result of the operation.</returns>
    IStateMachineModifier<TSaga> AfterLeaveAny(Func<EventActivityBinder<TSaga, State>, EventActivityBinder<TSaga, State>> activityCallback);

    /// <summary>
    /// Performs the when leave operation.
    /// </summary>
    /// <param name="state">The state value.</param>
    /// <param name="activityCallback">The activity callback value.</param>
    /// <returns>The result of the operation.</returns>
    IStateMachineModifier<TSaga> WhenLeave(State state,
        Func<EventActivityBinder<TSaga>, EventActivityBinder<TSaga>> activityCallback);

    /// <summary>
    /// Performs the before enter operation.
    /// </summary>
    /// <param name="state">The state value.</param>
    /// <param name="activityCallback">The activity callback value.</param>
    /// <returns>The result of the operation.</returns>
    IStateMachineModifier<TSaga> BeforeEnter(State state,
        Func<EventActivityBinder<TSaga, State>, EventActivityBinder<TSaga, State>> activityCallback);

    /// <summary>
    /// Performs the after leave operation.
    /// </summary>
    /// <param name="state">The state value.</param>
    /// <param name="activityCallback">The activity callback value.</param>
    /// <returns>The result of the operation.</returns>
    IStateMachineModifier<TSaga> AfterLeave(State state,
        Func<EventActivityBinder<TSaga, State>, EventActivityBinder<TSaga, State>> activityCallback);

    /// <summary>
    /// Performs the on unhandled event operation.
    /// </summary>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    IStateMachineModifier<TSaga> OnUnhandledEvent(UnhandledEventCallback<TSaga> callback);

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    void Apply();
}
