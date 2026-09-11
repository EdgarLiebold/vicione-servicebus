using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Evaluates a message-specific behavior condition asynchronously.</summary>
/// <typeparam name="TSaga">The saga state-machine instance type.</typeparam>
/// <typeparam name="TMessage">The message contract available to the condition.</typeparam>
/// <param name="context">The behavior context evaluated by the condition.</param>
/// <returns>A task whose result indicates whether the behavior condition is satisfied.</returns>
public delegate Task<bool> StateMachineAsyncCondition<TSaga, in TMessage>(BehaviorContext<TSaga, TMessage> context)
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class;


/// <summary>Evaluates a behavior condition asynchronously.</summary>
/// <typeparam name="TSaga">The saga state-machine instance type.</typeparam>
/// <param name="context">The behavior context evaluated by the condition.</param>
/// <returns>A task whose result indicates whether the behavior condition is satisfied.</returns>
public delegate Task<bool> StateMachineAsyncCondition<TSaga>(BehaviorContext<TSaga> context)
    where TSaga : class, SagaStateMachineInstance;
