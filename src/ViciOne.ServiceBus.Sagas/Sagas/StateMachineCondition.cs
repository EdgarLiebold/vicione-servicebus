namespace ViciOne.ServiceBus.Sagas;

/// <summary>Evaluates a behavior condition synchronously.</summary>
/// <typeparam name="TSaga">The saga state-machine instance type.</typeparam>
/// <param name="context">The behavior context evaluated by the condition.</param>
/// <returns><see langword="true" /> when the behavior condition is satisfied; otherwise, <see langword="false" />.</returns>
public delegate bool StateMachineCondition<TSaga>(IBehaviorContext<TSaga> context)
    where TSaga : class, ISagaStateMachineInstance;


/// <summary>Evaluates a message-specific behavior condition synchronously.</summary>
/// <typeparam name="TSaga">The saga state-machine instance type.</typeparam>
/// <typeparam name="TMessage">The message contract available to the condition.</typeparam>
/// <param name="context">The behavior context evaluated by the condition.</param>
/// <returns><see langword="true" /> when the behavior condition is satisfied; otherwise, <see langword="false" />.</returns>
public delegate bool StateMachineCondition<TSaga, in TMessage>(IBehaviorContext<TSaga, TMessage> context)
    where TSaga : class, ISagaStateMachineInstance
    where TMessage : class;
