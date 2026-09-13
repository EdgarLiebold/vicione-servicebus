namespace ViciOne.ServiceBus.Sagas;

/// <summary>Filters activities based on the conditional statement.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
public delegate bool StateMachineCondition<TSaga>(IBehaviorContext<TSaga> context)
    where TSaga : class, ISagaStateMachineInstance;


/// <summary>Filters activities based on the conditional statement.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
public delegate bool StateMachineCondition<TSaga, in TMessage>(IBehaviorContext<TSaga, TMessage> context)
    where TSaga : class, ISagaStateMachineInstance
    where TMessage : class;
