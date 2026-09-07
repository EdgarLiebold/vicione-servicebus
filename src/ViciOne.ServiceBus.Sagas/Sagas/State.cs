using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Defines the operations required by state.</summary>
public interface State :
    IVisitable,
    global::ViciOne.ServiceBus.Advanced.Initializers.INamedInitializerValue,
    IComparable<State>
{
    /// <summary>Gets the name.</summary>
    new string Name { get; }

    /// <summary>Raised when the state is entered.</summary>
    Event Enter { get; }

    /// <summary>Raised when the state is about to be left.</summary>
    Event Leave { get; }

    /// <summary>Raised just before the state is about to change to a new state.</summary>
    Event<State> BeforeEnter { get; }

    /// <summary>Raised just after the state has been left and a new state is selected.</summary>
    Event<State> AfterLeave { get; }
}


/// <summary>A state within a state machine that can be targeted with events.</summary>
/// <typeparam name="TSaga">The instance type to which the state applies.</typeparam>
public interface State<TSaga> :
    State,
    global::ViciOne.ServiceBus.Advanced.Initializers.INamedInitializerValue<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    /// <summary>Gets the events.</summary>
    IEnumerable<Event> Events { get; }

    /// <summary>Returns the superState of the state, if there is one.</summary>
    State<TSaga>? SuperState { get; }

    /// <summary>Raises the configured event.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RaiseAsync(BehaviorContext<TSaga> context, CancellationToken cancellationToken = default);

    /// <summary>Raise an event to the state, passing the instance.</summary>
    /// <typeparam name="T">The event data type.</typeparam>
    /// <param name="context">The event context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RaiseAsync<T>(BehaviorContext<TSaga, T> context, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Bind an activity to an event.</summary>
    /// <param name="event">The event.</param>
    /// <param name="activity">The activity.</param>
    void Bind(Event @event, IStateMachineActivity<TSaga> activity);

    /// <summary>
    /// Ignore the specified event in this state. Prevents an exception from being thrown if
    /// the event is raised during this state.
    /// </summary>
    /// <param name="event">The event.</param>
    void Ignore(Event @event);

    /// <summary>
    /// Ignore the specified event in this state if the filter condition passed. Prevents exceptions
    /// from being thrown if the event is raised during this state.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="event">The event.</param>
    /// <param name="filter">The filter to add to the pipeline.</param>
    void Ignore<T>(Event<T> @event, StateMachineCondition<TSaga, T> filter)
        where T : class;

    /// <summary>Adds a substate to the state.</summary>
    /// <param name="subState">The sub state.</param>
    void AddSubstate(State<TSaga> subState);

    /// <summary>True if the specified state is included in the state.</summary>
    /// <param name="state">The state.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool HasState(State<TSaga> state);

    /// <summary>True if the specified state is a substate of the current state.</summary>
    /// <param name="state">The state.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool IsStateOf(State<TSaga> state);
}
