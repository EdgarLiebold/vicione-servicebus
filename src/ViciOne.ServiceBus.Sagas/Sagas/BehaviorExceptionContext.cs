using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>An exceptional behavior context.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TException">The exception handled by the member.</typeparam>
public interface BehaviorExceptionContext<TSaga, out TException> :
    BehaviorContext<TSaga>
    where TException : Exception
    where TSaga : class, SagaStateMachineInstance
{
    /// <summary>Gets the exception.</summary>
    TException Exception { get; }

    /// <summary>Return a proxy of the current behavior context with the specified event and data.</summary>
    /// <typeparam name="T">The data type.</typeparam>
    /// <param name="event">The event for the new context.</param>
    /// <param name="data">The data for the event.</param>
    /// <returns>The created proxy.</returns>
    new BehaviorExceptionContext<TSaga, T, TException> CreateProxy<T>(Event<T> @event, T data)
        where T : class;
}


/// <summary>An exceptional behavior context.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="TException">The exception handled by the member.</typeparam>
public interface BehaviorExceptionContext<TSaga, out TMessage, out TException> :
    BehaviorContext<TSaga, TMessage>,
    BehaviorExceptionContext<TSaga, TException>
    where TException : Exception
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
{
    /// <summary>Return a proxy of the current behavior context with the specified event and data.</summary>
    /// <typeparam name="T">The data type.</typeparam>
    /// <param name="event">The event for the new context.</param>
    /// <param name="data">The data for the event.</param>
    /// <returns>The created proxy.</returns>
    new BehaviorExceptionContext<TSaga, T, TException> CreateProxy<T>(Event<T> @event, T data)
        where T : class;
}
