using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Receives callbacks while the states, events, behaviors, and activities of a state machine are traversed.</summary>
public interface StateMachineVisitor
{
    /// <summary>Visits a state and delegates traversal of its configured elements.</summary>
    /// <param name="state">The state being visited.</param>
    /// <param name="next">The callback that continues traversal from the state.</param>
    void Visit(State state, Action<State> next);

    /// <summary>Visits an untyped event and invokes the event continuation.</summary>
    /// <param name="event">The event being visited.</param>
    /// <param name="next">The callback that continues traversal from the event.</param>
    void Visit(Event @event, Action<Event> next);

    /// <summary>Visits a message event and invokes the event continuation.</summary>
    /// <typeparam name="TMessage">The message contract carried by the event.</typeparam>
    /// <param name="event">The event being visited.</param>
    /// <param name="next">The callback that continues traversal from the event.</param>
    void Visit<TMessage>(Event<TMessage> @event, Action<Event<TMessage>> next)
        where TMessage : class;

    /// <summary>Visits a state-machine activity as a leaf element.</summary>
    /// <param name="activity">The activity being visited.</param>
    void Visit(IStateMachineActivity activity);

    /// <summary>Visits an exception-handling activity and delegates traversal of its compensation behavior.</summary>
    /// <param name="activity">The exception-handling activity being visited.</param>
    /// <param name="next">The callback that continues traversal through the compensation behavior.</param>
    void Visit(IStateMachineExceptionActivity activity, Action<IStateMachineExceptionActivity> next);

    /// <summary>Visits an untyped-event behavior as a leaf element.</summary>
    /// <typeparam name="T">The saga instance managed by the behavior.</typeparam>
    /// <param name="behavior">The behavior being visited.</param>
    void Visit<T>(IBehavior<T> behavior)
        where T : class, SagaStateMachineInstance;

    /// <summary>Visits an untyped-event behavior and delegates traversal of its activities.</summary>
    /// <typeparam name="T">The saga instance managed by the behavior.</typeparam>
    /// <param name="behavior">The behavior being visited.</param>
    /// <param name="next">The callback that continues traversal through the behavior.</param>
    void Visit<T>(IBehavior<T> behavior, Action<IBehavior<T>> next)
        where T : class, SagaStateMachineInstance;

    /// <summary>Visits a message-event behavior as a leaf element.</summary>
    /// <typeparam name="T">The saga instance managed by the behavior.</typeparam>
    /// <typeparam name="TMessage">The message contract carried by the event.</typeparam>
    /// <param name="behavior">The behavior being visited.</param>
    void Visit<T, TMessage>(IBehavior<T, TMessage> behavior)
        where T : class, SagaStateMachineInstance
        where TMessage : class;

    /// <summary>Visits a message-event behavior and delegates traversal of its activities.</summary>
    /// <typeparam name="T">The saga instance managed by the behavior.</typeparam>
    /// <typeparam name="TMessage">The message contract carried by the event.</typeparam>
    /// <param name="behavior">The behavior being visited.</param>
    /// <param name="next">The callback that continues traversal through the behavior.</param>
    void Visit<T, TMessage>(IBehavior<T, TMessage> behavior, Action<IBehavior<T, TMessage>> next)
        where T : class, SagaStateMachineInstance
        where TMessage : class;

    /// <summary>Visits a state-machine activity and delegates traversal of nested elements.</summary>
    /// <param name="activity">The activity being visited.</param>
    /// <param name="next">The callback that continues traversal through the activity.</param>
    void Visit(IStateMachineActivity activity, Action<IStateMachineActivity> next);
}
