using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// Represents the method that handles async event message factory.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
/// <typeparam name="T">The t type.</typeparam>
/// <param name="context">The operation context.</param>
/// <returns>The result of the operation.</returns>
public delegate Task<T> AsyncEventMessageFactory<TSaga, in TMessage, T>(BehaviorContext<TSaga, TMessage> context)
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
    where T : class;


/// <summary>
/// Represents the method that handles async event message factory.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="T">The t type.</typeparam>
/// <param name="context">The operation context.</param>
/// <returns>The result of the operation.</returns>
public delegate Task<T> AsyncEventMessageFactory<TSaga, T>(BehaviorContext<TSaga> context)
    where TSaga : class, SagaStateMachineInstance
    where T : class;
