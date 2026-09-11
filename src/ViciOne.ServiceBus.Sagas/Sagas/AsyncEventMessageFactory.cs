using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Creates a message asynchronously from a message-specific behavior context.</summary>
/// <typeparam name="TSaga">The saga state-machine instance type.</typeparam>
/// <typeparam name="TMessage">The message contract available to the factory.</typeparam>
/// <typeparam name="T">The produced message type.</typeparam>
/// <param name="context">The behavior context used to create the message.</param>
/// <returns>A task whose result is the message created from the behavior context.</returns>
public delegate Task<T> AsyncEventMessageFactory<TSaga, in TMessage, T>(BehaviorContext<TSaga, TMessage> context)
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
    where T : class;


/// <summary>Creates a message asynchronously from a behavior context.</summary>
/// <typeparam name="TSaga">The saga state-machine instance type.</typeparam>
/// <typeparam name="T">The produced message type.</typeparam>
/// <param name="context">The behavior context used to create the message.</param>
/// <returns>A task whose result is the message created from the behavior context.</returns>
public delegate Task<T> AsyncEventMessageFactory<TSaga, T>(BehaviorContext<TSaga> context)
    where TSaga : class, SagaStateMachineInstance
    where T : class;
