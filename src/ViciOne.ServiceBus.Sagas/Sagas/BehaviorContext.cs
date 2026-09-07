using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>A behavior context is an event context delivered to a behavior, including the state instance.</summary>
/// <typeparam name="TSaga">The state instance type.</typeparam>
public interface BehaviorContext<TSaga> :
    SagaConsumeContext<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    /// <summary>Gets the state machine.</summary>
    StateMachine<TSaga> StateMachine { get; }

    /// <summary>Gets the event.</summary>
    Event Event { get; }

    /// <summary>Raise an event on the current instance, pushing the current event on the stack.</summary>
    /// <param name="event">The event to raise.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>An awaitable Task.</returns>
    Task RaiseAsync(Event @event, CancellationToken cancellationToken = default);

    /// <summary>Raise an event on the current instance, pushing the current event on the stack.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="event">The event to raise.</param>
    /// <param name="data">The event data.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>An awaitable Task.</returns>
    Task RaiseAsync<T>(Event<T> @event, T data, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Initializes the target component.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the init outcome.</returns>
    Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> InitAsync<T>(object values, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Return a proxy of the current behavior context with the specified event.</summary>
    /// <param name="event">The event for the new context.</param>
    /// <returns>The created proxy.</returns>
    BehaviorContext<TSaga> CreateProxy(Event @event);

    /// <summary>Return a proxy of the current behavior context with the specified event and data.</summary>
    /// <typeparam name="T">The data type.</typeparam>
    /// <param name="event">The event for the new context.</param>
    /// <param name="data">The data for the event.</param>
    /// <returns>The created proxy.</returns>
    BehaviorContext<TSaga, T> CreateProxy<T>(Event<T> @event, T data)
        where T : class;
}


/// <summary>A behavior context include an event context, along with the behavior for a state instance.</summary>
/// <typeparam name="TSaga">The instance type.</typeparam>
/// <typeparam name="TMessage">The event type.</typeparam>
public interface BehaviorContext<TSaga, out TMessage> :
    SagaConsumeContext<TSaga, TMessage>,
    BehaviorContext<TSaga>
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
{
    /// <summary>Gets the event.</summary>
    new Event<TMessage> Event { get; }

    /// <summary>Initializes the target component.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the init outcome.</returns>
    new Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> InitAsync<T>(object values, CancellationToken cancellationToken = default)
        where T : class;
}
