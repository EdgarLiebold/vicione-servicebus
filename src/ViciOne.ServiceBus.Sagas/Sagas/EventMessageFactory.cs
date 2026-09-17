namespace ViciOne.ServiceBus.Sagas;

/// <summary>Creates a message synchronously from a behavior context.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="T">The produced message type.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <returns>The message created from the behavior context.</returns>
public delegate T EventMessageFactory<TSaga, out T>(IBehaviorContext<TSaga> context)
    where TSaga : class, ISagaStateMachineInstance
    where T : class;


/// <summary>Creates a message synchronously from a message-specific behavior context.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="T">The produced message type.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <returns>The message created from the behavior context.</returns>
public delegate T EventMessageFactory<TInstance, in TMessage, out T>(IBehaviorContext<TInstance, TMessage> context)
    where TInstance : class, ISagaStateMachineInstance
    where TMessage : class
    where T : class;
