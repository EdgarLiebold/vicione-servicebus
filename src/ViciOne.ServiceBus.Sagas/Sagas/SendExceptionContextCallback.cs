using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Configures an outgoing send context from an exception behavior context.</summary>
/// <typeparam name="TSaga">The saga state-machine instance type.</typeparam>
/// <typeparam name="TException">The exception type available to the callback.</typeparam>
/// <typeparam name="T">The outgoing message type.</typeparam>
/// <param name="context">The exception behavior context supplied to the callback.</param>
/// <param name="sendContext">The outgoing send context to configure.</param>
public delegate void SendExceptionContextCallback<TSaga, in TException, in T>(IBehaviorExceptionContext<TSaga, TException> context,
    SendContext<T> sendContext)
    where TSaga : class, ISagaStateMachineInstance
    where TException : Exception
    where T : class;


/// <summary>Configures an outgoing send context from a message-specific exception behavior context.</summary>
/// <typeparam name="TSaga">The saga state-machine instance type.</typeparam>
/// <typeparam name="TMessage">The message contract available to the callback.</typeparam>
/// <typeparam name="TException">The exception type available to the callback.</typeparam>
/// <typeparam name="T">The outgoing message type.</typeparam>
/// <param name="context">The exception behavior context supplied to the callback.</param>
/// <param name="sendContext">The outgoing send context to configure.</param>
public delegate void SendExceptionContextCallback<TSaga, in TMessage, in TException, in T>(IBehaviorExceptionContext<TSaga, TMessage, TException> context,
    SendContext<T> sendContext)
    where TSaga : class, ISagaStateMachineInstance
    where TMessage : class
    where TException : Exception
    where T : class;
