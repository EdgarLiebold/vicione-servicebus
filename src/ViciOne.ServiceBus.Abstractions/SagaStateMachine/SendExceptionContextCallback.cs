using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// Represents the method that handles send exception context callback.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TException">The t exception type.</typeparam>
/// <typeparam name="T">The t type.</typeparam>
/// <param name="context">The operation context.</param>
/// <param name="sendContext">The send context value.</param>
/// <returns>The result of the operation.</returns>
public delegate void SendExceptionContextCallback<TSaga, in TException, in T>(BehaviorExceptionContext<TSaga, TException> context,
    SendContext<T> sendContext)
    where TSaga : class, SagaStateMachineInstance
    where TException : Exception
    where T : class;


/// <summary>
/// Represents the method that handles send exception context callback.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
/// <typeparam name="TException">The t exception type.</typeparam>
/// <typeparam name="T">The t type.</typeparam>
/// <param name="context">The operation context.</param>
/// <param name="sendContext">The send context value.</param>
/// <returns>The result of the operation.</returns>
public delegate void SendExceptionContextCallback<TSaga, in TMessage, in TException, in T>(BehaviorExceptionContext<TSaga, TMessage, TException> context,
    SendContext<T> sendContext)
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
    where TException : Exception
    where T : class;
