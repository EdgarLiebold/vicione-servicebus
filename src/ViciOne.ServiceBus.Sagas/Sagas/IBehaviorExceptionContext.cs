using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Provides the exception being handled by an untyped-event state-machine behavior.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
/// <typeparam name="TException">The exception type being handled.</typeparam>
public interface IBehaviorExceptionContext<TSaga, out TException> :
    IBehaviorContext<TSaga>
    where TException : Exception
    where TSaga : class, ISagaStateMachineInstance
{
    /// <summary>Gets the exception being handled.</summary>
    TException Exception { get; }

    /// <summary>Creates an exception context for a nested message event on the same saga instance.</summary>
    /// <typeparam name="TMessage">The nested event message type.</typeparam>
    /// <param name="event">The event for the new context.</param>
    /// <param name="data">The data for the event.</param>
    /// <returns>The nested message-event exception context.</returns>
    new IBehaviorExceptionContext<TSaga, TMessage, TException> CreateProxy<TMessage>(IEvent<TMessage> @event, TMessage data)
        where TMessage : class;
}


/// <summary>Provides the typed event message and exception being handled by a state-machine behavior.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
/// <typeparam name="TMessage">The event message type.</typeparam>
/// <typeparam name="TException">The exception type being handled.</typeparam>
public interface IBehaviorExceptionContext<TSaga, out TMessage, out TException> :
    IBehaviorContext<TSaga, TMessage>,
    IBehaviorExceptionContext<TSaga, TException>
    where TException : Exception
    where TSaga : class, ISagaStateMachineInstance
    where TMessage : class
{
    /// <summary>Creates an exception context for a nested message event on the same saga instance.</summary>
    /// <typeparam name="TNestedMessage">The nested event message type.</typeparam>
    /// <param name="event">The event for the new context.</param>
    /// <param name="data">The data for the event.</param>
    /// <returns>The nested message-event exception context.</returns>
    new IBehaviorExceptionContext<TSaga, TNestedMessage, TException> CreateProxy<TNestedMessage>(IEvent<TNestedMessage> @event, TNestedMessage data)
        where TNestedMessage : class;
}
