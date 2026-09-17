using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Selects the request service address from a behavior context.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <returns>The selected service address.</returns>
public delegate Uri ServiceAddressProvider<TSaga>(IBehaviorContext<TSaga> context)
    where TSaga : class, ISagaStateMachineInstance;


/// <summary>Selects the request service address from a message-specific behavior context.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <returns>The selected service address.</returns>
public delegate Uri ServiceAddressProvider<TSaga, in TMessage>(IBehaviorContext<TSaga, TMessage> context)
    where TSaga : class, ISagaStateMachineInstance
    where TMessage : class;
