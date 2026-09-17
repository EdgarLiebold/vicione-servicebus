using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Describes a named state and the events raised around its lifecycle.</summary>
public interface IState :
    IVisitable,
    global::ViciOne.ServiceBus.Advanced.Initializers.INamedInitializerValue,
    IComparable<IState>
{
    /// <summary>Gets the state name.</summary>
    new string Name { get; }

    /// <summary>Gets the event raised after the state becomes current.</summary>
    IEvent Enter { get; }

    /// <summary>Gets the event raised before the current state is left.</summary>
    IEvent Leave { get; }

    /// <summary>Gets the event raised before this state becomes current.</summary>
    IEvent<IState> BeforeEnter { get; }

    /// <summary>Gets the event raised after this state has been left.</summary>
    IEvent<IState> AfterLeave { get; }
}


/// <summary>Defines the event behavior and hierarchy of a state for one saga type.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
public interface IState<TSaga> :
    IState,
    global::ViciOne.ServiceBus.Advanced.Initializers.INamedInitializerValue<TSaga>
    where TSaga : class, ISagaStateMachineInstance
{
    /// <summary>Gets the non-lifecycle events effective in this state, including events inherited from its superstate.</summary>
    IEnumerable<IEvent> Events { get; }

    /// <summary>Gets the event bindings explicitly configured on this state, including lifecycle-event handlers.</summary>
    IEnumerable<IEvent> DeclaredEvents { get; }

    /// <summary>Gets the state whose behavior this state inherits, or <see langword="null" /> for a root state.</summary>
    IState<TSaga>? SuperState { get; }

    /// <summary>Executes the activities bound to the context event in this state.</summary>
    /// <param name="context">The saga and event being processed.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RaiseAsync(IBehaviorContext<TSaga> context, CancellationToken cancellationToken = default);

    /// <summary>Executes the activities bound to the typed context event in this state.</summary>
    /// <typeparam name="TMessage">The event message type.</typeparam>
    /// <param name="context">The saga, event, and message being processed.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RaiseAsync<TMessage>(IBehaviorContext<TSaga, TMessage> context, CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>Binds an activity to an event handled directly by this state.</summary>
    /// <param name="event">The event to handle.</param>
    /// <param name="activity">The activity executed for the event.</param>
    void Bind(IEvent @event, IStateMachineActivity<TSaga> activity);

    /// <summary>
    /// Marks an event as intentionally ignored while this state is current.
    /// </summary>
    /// <param name="event">The event to ignore.</param>
    void Ignore(IEvent @event);

    /// <summary>
    /// Ignores an event while this state is current when its message satisfies a condition.
    /// </summary>
    /// <typeparam name="TMessage">The message contract carried by the event.</typeparam>
    /// <param name="event">The event to conditionally ignore.</param>
    /// <param name="filter">The condition evaluated for the event message.</param>
    void Ignore<TMessage>(IEvent<TMessage> @event, StateMachineCondition<TSaga, TMessage> filter)
        where TMessage : class;

    /// <summary>Adds a direct substate.</summary>
    /// <param name="subState">The child state to add.</param>
    void AddSubstate(IState<TSaga> subState);

    /// <summary>Determines whether this state equals or contains a descendant state.</summary>
    /// <param name="state">The candidate state.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool HasState(IState<TSaga> state);

    /// <summary>Determines whether this state is equal to or nested beneath the specified state.</summary>
    /// <param name="state">The candidate ancestor state.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool IsStateOf(IState<TSaga> state);
}
