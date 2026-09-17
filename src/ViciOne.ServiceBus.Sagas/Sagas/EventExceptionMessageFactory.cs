using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Creates a message synchronously from an exception behavior context.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TException">The exception handled by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <returns>The message created from the exception behavior context.</returns>
public delegate TMessage EventExceptionMessageFactory<TSaga, in TException, out TMessage>(IBehaviorExceptionContext<TSaga, TException> context)
    where TSaga : class, ISagaStateMachineInstance
    where TException : Exception
    where TMessage : class;


/// <summary>Creates a message synchronously from a message-specific exception behavior context.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="TException">The exception handled by the member.</typeparam>
/// <typeparam name="T">The produced message type.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <returns>The message created from the exception behavior context.</returns>
public delegate T EventExceptionMessageFactory<TSaga, in TMessage, in TException, out T>(IBehaviorExceptionContext<TSaga, TMessage, TException> context)
    where TSaga : class, ISagaStateMachineInstance
    where TMessage : class
    where TException : Exception
    where T : class;
