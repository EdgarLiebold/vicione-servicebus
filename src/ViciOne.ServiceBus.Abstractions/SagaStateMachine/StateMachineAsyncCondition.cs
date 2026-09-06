using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Filters activities based on the async conditional statement.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <returns>The value produced by the operation.</returns>
public delegate Task<bool> StateMachineAsyncCondition<TSaga, in TMessage>(BehaviorContext<TSaga, TMessage> context)
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class;


/// <summary>Filters activities based on the async conditional statement.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <returns>The value produced by the operation.</returns>
public delegate Task<bool> StateMachineAsyncCondition<TSaga>(BehaviorContext<TSaga> context)
    where TSaga : class, SagaStateMachineInstance;
