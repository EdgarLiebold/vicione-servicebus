namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// Represents the method that handles event message factory.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="T">The t type.</typeparam>
/// <param name="context">The operation context.</param>
/// <returns>The result of the operation.</returns>
public delegate T EventMessageFactory<TSaga, out T>(BehaviorContext<TSaga> context)
    where TSaga : class, SagaStateMachineInstance
    where T : class;


/// <summary>
/// Represents the method that handles event message factory.
/// </summary>
/// <typeparam name="TInstance">The t instance type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
/// <typeparam name="T">The t type.</typeparam>
/// <param name="context">The operation context.</param>
/// <returns>The result of the operation.</returns>
public delegate T EventMessageFactory<TInstance, in TMessage, out T>(BehaviorContext<TInstance, TMessage> context)
    where TInstance : class, SagaStateMachineInstance
    where TMessage : class
    where T : class;
