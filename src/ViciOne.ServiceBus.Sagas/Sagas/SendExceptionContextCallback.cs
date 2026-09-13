using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Represents the method that handles send exception context callback.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TException">The exception handled by the member.</typeparam>
/// <typeparam name="T">The value type.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <param name="sendContext">The send context.</param>
public delegate void SendExceptionContextCallback<TSaga, in TException, in T>(IBehaviorExceptionContext<TSaga, TException> context,
    SendContext<T> sendContext)
    where TSaga : class, ISagaStateMachineInstance
    where TException : Exception
    where T : class;


/// <summary>Represents the method that handles send exception context callback.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="TException">The exception handled by the member.</typeparam>
/// <typeparam name="T">The value type.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <param name="sendContext">The send context.</param>
public delegate void SendExceptionContextCallback<TSaga, in TMessage, in TException, in T>(IBehaviorExceptionContext<TSaga, TMessage, TException> context,
    SendContext<T> sendContext)
    where TSaga : class, ISagaStateMachineInstance
    where TMessage : class
    where TException : Exception
    where T : class;
