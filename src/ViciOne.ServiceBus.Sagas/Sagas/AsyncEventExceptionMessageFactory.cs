using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Creates a message asynchronously from an exception behavior context.</summary>
/// <typeparam name="TSaga">The saga state-machine instance type.</typeparam>
/// <typeparam name="TException">The exception type available to the factory.</typeparam>
/// <typeparam name="T">The produced message type.</typeparam>
/// <param name="context">The exception behavior context used to create the message.</param>
/// <returns>A task whose result is the message created from the exception behavior context.</returns>
public delegate Task<T> AsyncEventExceptionMessageFactory<TSaga, in TException, T>(BehaviorExceptionContext<TSaga, TException> context)
    where TSaga : class, SagaStateMachineInstance
    where TException : Exception;


/// <summary>Creates a message asynchronously from a message-specific exception behavior context.</summary>
/// <typeparam name="TSaga">The saga state-machine instance type.</typeparam>
/// <typeparam name="TMessage">The message contract associated with the exception.</typeparam>
/// <typeparam name="TException">The exception type available to the factory.</typeparam>
/// <typeparam name="T">The produced message type.</typeparam>
/// <param name="context">The exception behavior context used to create the message.</param>
/// <returns>A task whose result is the message created from the exception behavior context.</returns>
public delegate Task<T> AsyncEventExceptionMessageFactory<TSaga, in TMessage, in TException, T>(
    BehaviorExceptionContext<TSaga, TMessage, TException> context)
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
    where TException : Exception
    where T : class;
