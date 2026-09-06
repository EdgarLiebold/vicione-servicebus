using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Represents the method that handles async event message factory.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="T">The value type.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <returns>The value produced by the operation.</returns>
public delegate Task<T> AsyncEventMessageFactory<TSaga, in TMessage, T>(BehaviorContext<TSaga, TMessage> context)
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
    where T : class;


/// <summary>Represents the method that handles async event message factory.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="T">The value type.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <returns>The value produced by the operation.</returns>
public delegate Task<T> AsyncEventMessageFactory<TSaga, T>(BehaviorContext<TSaga> context)
    where TSaga : class, SagaStateMachineInstance
    where T : class;
