namespace ViciOne.ServiceBus.Sagas;

/// <summary>Configures an outgoing send context from a behavior context.</summary>
/// <typeparam name="TSaga">The saga state-machine instance type.</typeparam>
/// <typeparam name="T">The outgoing message type.</typeparam>
/// <param name="context">The behavior context supplied to the callback.</param>
/// <param name="sendContext">The outgoing send context to configure.</param>
public delegate void SendContextCallback<TSaga, in T>(IBehaviorContext<TSaga> context, SendContext<T> sendContext)
    where TSaga : class, ISagaStateMachineInstance
    where T : class;


/// <summary>Configures an outgoing send context from a message-specific behavior context.</summary>
/// <typeparam name="TSaga">The saga state-machine instance type.</typeparam>
/// <typeparam name="TMessage">The message contract available to the callback.</typeparam>
/// <typeparam name="T">The outgoing message type.</typeparam>
/// <param name="context">The behavior context supplied to the callback.</param>
/// <param name="sendContext">The outgoing send context to configure.</param>
public delegate void SendContextCallback<TSaga, in TMessage, in T>(IBehaviorContext<TSaga, TMessage> context, SendContext<T> sendContext)
    where TSaga : class, ISagaStateMachineInstance
    where TMessage : class
    where T : class;
