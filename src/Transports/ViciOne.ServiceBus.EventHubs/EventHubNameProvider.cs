namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Selects the destination Event Hub for a saga state-machine behavior.</summary>
/// <typeparam name="TInstance">The saga instance type.</typeparam>
/// <param name="context">The current behavior context.</param>
/// <returns>The destination Event Hub entity name.</returns>
public delegate string EventHubNameProvider<TInstance>(BehaviorContext<TInstance> context)
    where TInstance : class, SagaStateMachineInstance;


/// <summary>Selects the destination Event Hub for a message behavior in a saga state machine.</summary>
/// <typeparam name="TInstance">The saga instance type.</typeparam>
/// <typeparam name="TMessage">The behavior message type.</typeparam>
/// <param name="context">The current message behavior context.</param>
/// <returns>The destination Event Hub entity name.</returns>
public delegate string EventHubNameProvider<TInstance, in TMessage>(BehaviorContext<TInstance, TMessage> context)
    where TInstance : class, SagaStateMachineInstance
    where TMessage : class;
