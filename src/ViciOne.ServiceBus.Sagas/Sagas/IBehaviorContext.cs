using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Provides the saga, state machine, and event being processed by a state-machine behavior.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
public interface IBehaviorContext<TSaga> :
    SagaConsumeContext<TSaga>
    where TSaga : class, ISagaStateMachineInstance
{
    /// <summary>Gets the state machine executing the behavior.</summary>
    IStateMachine<TSaga> StateMachine { get; }

    /// <summary>Gets the event being processed.</summary>
    IEvent Event { get; }

    /// <summary>Raises an untyped event on the current saga instance before resuming the current event.</summary>
    /// <param name="event">The event to raise.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the nested event has been processed.</returns>
    Task RaiseAsync(IEvent @event, CancellationToken cancellationToken = default);

    /// <summary>Raises a message event on the current saga instance before resuming the current event.</summary>
    /// <typeparam name="TMessage">The nested event message type.</typeparam>
    /// <param name="event">The event to raise.</param>
    /// <param name="data">The event data.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the nested event has been processed.</returns>
    Task RaiseAsync<TMessage>(IEvent<TMessage> @event, TMessage data, CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>Initializes a message using this behavior context as the source context.</summary>
    /// <typeparam name="TMessage">The message type to initialize.</typeparam>
    /// <param name="values">The values projected onto the message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that returns the initialized message and send context.</returns>
    Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>> InitAsync<TMessage>(object values,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>Creates a context for a nested untyped event on the same saga instance.</summary>
    /// <param name="event">The event for the new context.</param>
    /// <returns>The nested event context.</returns>
    IBehaviorContext<TSaga> CreateProxy(IEvent @event);

    /// <summary>Creates a context for a nested message event on the same saga instance.</summary>
    /// <typeparam name="TMessage">The nested event message type.</typeparam>
    /// <param name="event">The event for the new context.</param>
    /// <param name="data">The data for the event.</param>
    /// <returns>The nested message-event context.</returns>
    IBehaviorContext<TSaga, TMessage> CreateProxy<TMessage>(IEvent<TMessage> @event, TMessage data)
        where TMessage : class;
}


/// <summary>Provides the typed event message in addition to the saga and state-machine behavior context.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
/// <typeparam name="TMessage">The event message type.</typeparam>
public interface IBehaviorContext<TSaga, out TMessage> :
    SagaConsumeContext<TSaga, TMessage>,
    IBehaviorContext<TSaga>
    where TSaga : class, ISagaStateMachineInstance
    where TMessage : class
{
    /// <summary>Gets the typed event being processed.</summary>
    new IEvent<TMessage> Event { get; }

    /// <summary>Initializes a message using this typed behavior context as the source context.</summary>
    /// <typeparam name="TResult">The message type to initialize.</typeparam>
    /// <param name="values">The values projected onto the message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that returns the initialized message and send context.</returns>
    new Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TResult>> InitAsync<TResult>(object values,
        CancellationToken cancellationToken = default)
        where TResult : class;
}
