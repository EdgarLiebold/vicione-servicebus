namespace ViciOne.ServiceBus.Sagas;

/// <summary>Represents the method that handles send context callback.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="T">The value type.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <param name="sendContext">The send context.</param>
public delegate void SendContextCallback<TSaga, in T>(IBehaviorContext<TSaga> context, SendContext<T> sendContext)
    where TSaga : class, ISagaStateMachineInstance
    where T : class;


/// <summary>Represents the method that handles send context callback.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="T">The value type.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <param name="sendContext">The send context.</param>
public delegate void SendContextCallback<TSaga, in TMessage, in T>(IBehaviorContext<TSaga, TMessage> context, SendContext<T> sendContext)
    where TSaga : class, ISagaStateMachineInstance
    where TMessage : class
    where T : class;
