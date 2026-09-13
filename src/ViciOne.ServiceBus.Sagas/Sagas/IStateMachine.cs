using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Describes the states and events in a configured state machine.</summary>
public interface IStateMachine :
    IVisitable
{
    /// <summary>Gets the state-machine name.</summary>
    string Name { get; }

    /// <summary>Gets the events declared by the state machine.</summary>
    IEnumerable<IEvent> Events { get; }

    /// <summary>Gets the states declared by the state machine.</summary>
    IEnumerable<IState> States { get; }

    /// <summary>Gets the saga state type controlled by the state machine.</summary>
    Type InstanceType { get; }

    /// <summary>Gets the initial state assigned to a new saga instance.</summary>
    IState Initial { get; }

    /// <summary>Gets the terminal state after which a saga can be removed.</summary>
    IState Final { get; }

    /// <summary>Returns a declared event by name.</summary>
    /// <param name="name">The event name.</param>
    /// <returns>The matching event.</returns>
    IEvent GetEvent(string name);

    /// <summary>Returns a declared state by name.</summary>
    /// <param name="name">The state name.</param>
    /// <returns>The matching state.</returns>
    IState GetState(string name);

    /// <summary>Returns the events accepted while a state is current.</summary>
    /// <param name="state">The state to inspect.</param>
    /// <returns>The events accepted by the state.</returns>
    IEnumerable<IEvent> NextEvents(IState state);

    /// <summary>Determines whether an event participates in composite-event tracking.</summary>
    /// <param name="event">The event to inspect.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool IsCompositeEvent(IEvent @event);
}


/// <summary>Executes a configured state machine against one saga state type.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
public interface IStateMachine<TSaga> :
    IStateMachine
    where TSaga : class, ISagaStateMachineInstance
{
    /// <summary>Gets the accessor that reads and writes the current state on a saga instance.</summary>
    IStateAccessor<TSaga> Accessor { get; }

    /// <summary>Returns a typed state by name.</summary>
    /// <param name="name">The state name.</param>
    /// <returns>The matching typed state.</returns>
    new IState<TSaga> GetState(string name);

    /// <summary>Raises an untyped event on a saga instance.</summary>
    /// <param name="context">The saga and event being processed.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents completion of the configured behavior for the raised event.</returns>
    Task RaiseEventAsync(IBehaviorContext<TSaga> context, CancellationToken cancellationToken = default);

    /// <summary>Raises a message event on a saga instance.</summary>
    /// <typeparam name="TMessage">The event message type.</typeparam>
    /// <param name="context">The saga, event, and message being processed.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents completion of the configured behavior for the raised message event.</returns>
    Task RaiseEventAsync<TMessage>(IBehaviorContext<TSaga, TMessage> context, CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>Connects an observer to every event processed by this state machine.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    IDisposable ConnectEventObserver(IEventObserver<TSaga> observer);
    /// <summary>Connects an observer to one event processed by this state machine.</summary>
    /// <param name="event">The event to observe.</param>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    IDisposable ConnectEventObserver(IEvent @event, IEventObserver<TSaga> observer);
    /// <summary>Connects an observer to state transitions performed by this state machine.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    IDisposable ConnectStateObserver(IStateObserver<TSaga> observer);
}
