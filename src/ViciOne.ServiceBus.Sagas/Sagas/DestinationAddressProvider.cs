using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Selects the destination address from a message-specific behavior context.</summary>
/// <typeparam name="TSaga">The saga instance.</typeparam>
/// <typeparam name="TMessage">The message data.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <returns>The selected destination address.</returns>
public delegate Uri DestinationAddressProvider<TSaga, in TMessage>(IBehaviorContext<TSaga, TMessage> context)
    where TSaga : class, ISagaStateMachineInstance
    where TMessage : class;


/// <summary>Selects the destination address from a behavior context.</summary>
/// <typeparam name="TSaga">The saga instance.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <returns>The selected destination address.</returns>
public delegate Uri DestinationAddressProvider<TSaga>(IBehaviorContext<TSaga> context)
    where TSaga : class, ISagaStateMachineInstance;
