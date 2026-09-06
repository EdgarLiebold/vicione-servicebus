using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Defines the operations required by state machine visitor.</summary>
public interface StateMachineVisitor
{
    /// <summary>Visits the configured graph element.</summary>
    /// <param name="state">The state.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    void Visit(State state, Action<State> next);

    /// <summary>Visits the configured graph element.</summary>
    /// <param name="event">The event.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    void Visit(Event @event, Action<Event> next);

    /// <summary>Visits the configured graph element.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="event">The event.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    void Visit<TMessage>(Event<TMessage> @event, Action<Event<TMessage>> next)
        where TMessage : class;

    /// <summary>Visits the configured graph element.</summary>
    /// <param name="activity">The activity.</param>
    void Visit(IStateMachineActivity activity);

    /// <summary>Visits the configured graph element.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="behavior">The state-machine behavior to compose or inspect.</param>
    void Visit<T>(IBehavior<T> behavior)
        where T : class, SagaStateMachineInstance;

    /// <summary>Visits the configured graph element.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="behavior">The state-machine behavior to compose or inspect.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    void Visit<T>(IBehavior<T> behavior, Action<IBehavior<T>> next)
        where T : class, SagaStateMachineInstance;

    /// <summary>Visits the configured graph element.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="behavior">The state-machine behavior to compose or inspect.</param>
    void Visit<T, TMessage>(IBehavior<T, TMessage> behavior)
        where T : class, SagaStateMachineInstance
        where TMessage : class;

    /// <summary>Visits the configured graph element.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="behavior">The state-machine behavior to compose or inspect.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    void Visit<T, TMessage>(IBehavior<T, TMessage> behavior, Action<IBehavior<T, TMessage>> next)
        where T : class, SagaStateMachineInstance
        where TMessage : class;

    /// <summary>Visits the configured graph element.</summary>
    /// <param name="activity">The activity.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    void Visit(IStateMachineActivity activity, Action<IStateMachineActivity> next);
}
