using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// Defines the contract for state machine visitor.
/// </summary>
public interface StateMachineVisitor
{
    /// <summary>
    /// Performs the visit operation.
    /// </summary>
    /// <param name="state">The state value.</param>
    /// <param name="next">The next value.</param>
    void Visit(State state, Action<State> next);

    /// <summary>
    /// Performs the visit operation.
    /// </summary>
    /// <param name="event">The event value.</param>
    /// <param name="next">The next value.</param>
    void Visit(Event @event, Action<Event> next);

    /// <summary>
    /// Performs the visit operation.
    /// </summary>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="event">The event value.</param>
    /// <param name="next">The next value.</param>
    void Visit<TMessage>(Event<TMessage> @event, Action<Event<TMessage>> next)
        where TMessage : class;

    /// <summary>
    /// Performs the visit operation.
    /// </summary>
    /// <param name="activity">The activity value.</param>
    void Visit(IStateMachineActivity activity);

    /// <summary>
    /// Performs the visit operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="behavior">The behavior value.</param>
    void Visit<T>(IBehavior<T> behavior)
        where T : class, SagaStateMachineInstance;

    /// <summary>
    /// Performs the visit operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="behavior">The behavior value.</param>
    /// <param name="next">The next value.</param>
    void Visit<T>(IBehavior<T> behavior, Action<IBehavior<T>> next)
        where T : class, SagaStateMachineInstance;

    /// <summary>
    /// Performs the visit operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="behavior">The behavior value.</param>
    void Visit<T, TMessage>(IBehavior<T, TMessage> behavior)
        where T : class, SagaStateMachineInstance
        where TMessage : class;

    /// <summary>
    /// Performs the visit operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="behavior">The behavior value.</param>
    /// <param name="next">The next value.</param>
    void Visit<T, TMessage>(IBehavior<T, TMessage> behavior, Action<IBehavior<T, TMessage>> next)
        where T : class, SagaStateMachineInstance
        where TMessage : class;

    /// <summary>
    /// Performs the visit operation.
    /// </summary>
    /// <param name="activity">The activity value.</param>
    /// <param name="next">The next value.</param>
    void Visit(IStateMachineActivity activity, Action<IStateMachineActivity> next);
}
