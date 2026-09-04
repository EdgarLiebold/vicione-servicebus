namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// Represents the method that handles send context callback.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="T">The t type.</typeparam>
/// <param name="context">The operation context.</param>
/// <param name="sendContext">The send context value.</param>
/// <returns>The result of the operation.</returns>
public delegate void SendContextCallback<TSaga, in T>(BehaviorContext<TSaga> context, SendContext<T> sendContext)
    where TSaga : class, SagaStateMachineInstance
    where T : class;


/// <summary>
/// Represents the method that handles send context callback.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
/// <typeparam name="T">The t type.</typeparam>
/// <param name="context">The operation context.</param>
/// <param name="sendContext">The send context value.</param>
/// <returns>The result of the operation.</returns>
public delegate void SendContextCallback<TSaga, in TMessage, in T>(BehaviorContext<TSaga, TMessage> context, SendContext<T> sendContext)
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
    where T : class;
